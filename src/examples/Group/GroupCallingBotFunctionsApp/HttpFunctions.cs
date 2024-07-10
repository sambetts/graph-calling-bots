using CommonUtils;
using GroupCalls.Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ServiceHostedMediaCallingBot.Engine;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;
using System.Net;
using System.Reflection;
using System.Text.Json;

namespace GroupCallingBot.FunctionApp;

/// <summary>
/// Azure Functions implementation of PSTN bot.
/// </summary>
public class HttpFunctions
{
    private readonly ILogger _logger;
    private readonly GroupCallBot _groupCallingBot;
    private readonly CallInviteBot _callInviteBot;
    private readonly ICallStateManager<GroupCallActiveCallState> _callStateManager;
    private readonly BotCallRedirector _botCallRedirector;

    public HttpFunctions(ILoggerFactory loggerFactory, GroupCallBot callingBot, CallInviteBot callInviteBot, ICallStateManager<GroupCallActiveCallState> callStateManager, BotCallRedirector botCallRedirector)
    {
        _logger = loggerFactory.CreateLogger<HttpFunctions>();
        _groupCallingBot = callingBot;
        _callInviteBot = callInviteBot;
        _callStateManager = callStateManager;
        _botCallRedirector = botCallRedirector;
    }

    /// <summary>
    /// Handle Graph call notifications. Must be anonymous.
    /// </summary>
    [Function(nameof(CallNotification))]
    public async Task<HttpResponseData> CallNotification([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var (notificationsPayload, body) = await GetBody<CommsNotificationsPayload>(req);

        if (notificationsPayload != null)
        {
            _logger.LogDebug($"Processing {notificationsPayload.CommsNotifications.Count} Graph call notification(s)");
            foreach (var notification in notificationsPayload.CommsNotifications)
            {
                var callId = BaseActiveCallState.GetCallId(notification.ResourceUrl);
                if (callId != null)
                {
                    var bot = _botCallRedirector.GetBotByCallId(callId);
                    if (bot != null)        // Logging for negative handled in GetBotByCallId
                        try
                        {
                            await bot.HandleNotificationsAndUpdateCallStateAsync(notificationsPayload);
                        }
                        catch (Exception ex)
                        {
                            var exResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                            exResponse.WriteString(ex.ToString());
                            return exResponse;
                        }
                }
                else
                {
                    _logger.LogError($"Unrecognized call ID in notification {notification.ResourceUrl}");
                }
            }

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            return response;
        }
        else
        {
            return SendBadRequest(req);
        }
    }

    /// <summary>
    /// Send WAV file for call. Recommended: use CDN to deliver content. Must be anonymous.
    /// </summary>
    [Function(HttpRouteConstants.WavFileActionName)]
    public async Task<HttpResponseData> WavFile([HttpTrigger(AuthorizationLevel.Anonymous, "get", "head")] HttpRequestData req)
    {
        _logger.LogInformation($"Sending WAV file HTTP response");

        // Use embedded WAV file to avoid external dependencies. Not recommended for production.
        using (var memoryStream = new MemoryStream())
        {
            using (var localWavStream = Resources.ReadResource("GroupCallingBot.FunctionApp.groupcall.wav", Assembly.GetExecutingAssembly()))
            {
                localWavStream.CopyTo(memoryStream);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteBytesAsync(memoryStream.ToArray());
                response.Headers.Add("Content-Type", "audio/wav");
                return response;
            }
        }
    }

    /// <summary>
    /// Start call triggered by HTTP request.
    /// </summary>
    [Function(nameof(StartCall))]
    public async Task<HttpResponseData> StartCall([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var (newCallReq, responseBodyRaw) = await GetBody<StartGroupCallData>(req);
        if (newCallReq != null)
        {
            var groupCall = await _groupCallingBot.StartGroupCall(newCallReq);
            if (groupCall != null)
            {
                LogBotLogic($"Started group call with ID {groupCall.Id}");

                if (groupCall != null)
                {
                    foreach (var attendee in newCallReq.Attendees)
                    {
                        var inviteCall = await _callInviteBot.CallCandidateForGroupCall(attendee, newCallReq, groupCall);
                        if (inviteCall == null)
                        {
                            _logger.LogError($"Failed to invite {attendee.DisplayName} ({attendee.Id})");
                        }
                        else
                        {
                            LogBotLogic($"Invited '{attendee.DisplayName}' ({attendee.Id}) on new P2P call {inviteCall.Id}");
                        }
                    }

                    var response = req.CreateResponse(HttpStatusCode.Accepted);
                    await response.WriteAsJsonAsync(groupCall);
                    return response;
                }
                else
                {
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }
            }
            else
            {
                _logger.LogError("Failed to start group call");
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }
        else
        {
            _logger.LogError($"Unrecognized request body: {responseBodyRaw}");
            return SendBadRequest(req);
        }
    }


    [Function(nameof(GetActiveCalls))]
    public async Task<HttpResponseData> GetActiveCalls([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        if (!_callStateManager.Initialised) await _callStateManager.Initialise();

        var calls = await _callStateManager.GetActiveCalls();
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(calls);
        return response;
    }

    void LogBotLogic(string msg)
    { 
        _logger.LogInformation("-" + msg);
    }

    HttpResponseData SendBadRequest(HttpRequestData req)
    {
        _logger.LogWarning("Unrecognized request body.");
        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        return response;
    }

    async Task<(T?, string)> GetBody<T>(HttpRequestData req)
    {
        var reqBodyContent = await req.ReadAsStringAsync();
        T? notifications = default;
        try
        {
            notifications = JsonSerializer.Deserialize<T>(reqBodyContent ?? string.Empty);
        }
        catch (JsonException)
        {
            // Ignore invalid JSON
        }

        return (notifications, reqBodyContent ?? string.Empty);
    }
}
