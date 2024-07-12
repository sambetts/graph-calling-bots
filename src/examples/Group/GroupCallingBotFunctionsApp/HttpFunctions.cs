using CommonUtils;
using GraphCallingBots;
using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using GroupCalls.Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Reflection;
using System.Text.Json;

namespace GroupCallingBot.FunctionApp;

/// <summary>
/// Azure Functions implementation of PSTN bot.
/// </summary>
public class HttpFunctions(ILogger<HttpFunctions> logger, GroupCallOrchestrator callOrchestrator,
    ICallStateManager<BaseActiveCallState> callStateManager, BotCallRedirector botCallRedirector)
{

    /// <summary>
    /// Handle Graph call notifications. Must be anonymous.
    /// </summary>
    [Function(nameof(CallNotification))]
    public async Task<HttpResponseData> CallNotification([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var (notificationsPayload, body) = await GetBody<CommsNotificationsPayload>(req);

        if (notificationsPayload != null)
        {
            logger.LogDebug($"Processing {notificationsPayload.CommsNotifications.Count} Graph call notification(s)");
            foreach (var notification in notificationsPayload.CommsNotifications)
            {
                var callId = BaseActiveCallState.GetCallId(notification.ResourceUrl);
                if (callId != null)
                {
                    var bot = botCallRedirector.GetBotByCallId(callId);
                    if (bot != null)        // Logging for negative handled in GetBotByCallId
                    {
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

                }
                else
                {
                    logger.LogError($"Unrecognized call ID in notification {notification.ResourceUrl}");
                }
            }

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            return response;
        }
        else
        {
            logger.LogError($"Unrecognized request body: {body}");
            return SendBadRequest(req);
        }
    }

    /// <summary>
    /// Send WAV file for call. Recommended: use CDN to deliver content. Must be anonymous.
    /// </summary>
    [Function(HttpRouteConstants.WavFileInviteToCallActionName)]
    public async Task<HttpResponseData> WavFileInviteToCall([HttpTrigger(AuthorizationLevel.Anonymous, "get", "head")] HttpRequestData req)
    {
        logger.LogInformation($"Sending InviteToCall WAV file HTTP response");

        // Use embedded WAV file to avoid external dependencies. Not recommended for production.
        using (var memoryStream = new MemoryStream())
        {
            using (var localWavStream = Resources.ReadResource("GroupCallingBot.FunctionApp.WAVs.invite.wav", Assembly.GetExecutingAssembly()))
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
    /// Send WAV file for call. Recommended: use CDN to deliver content. Must be anonymous.
    /// </summary>
    [Function(HttpRouteConstants.WavFileTransferingActionName)]
    public async Task<HttpResponseData> WavFileTransfering([HttpTrigger(AuthorizationLevel.Anonymous, "get", "head")] HttpRequestData req)
    {
        logger.LogInformation($"Sending transfering WAV file HTTP response");

        // Use embedded WAV file to avoid external dependencies. Not recommended for production.
        using (var memoryStream = new MemoryStream())
        {
            using (var localWavStream = Resources.ReadResource("GroupCallingBot.FunctionApp.WAVs.transfering.wav", Assembly.GetExecutingAssembly()))
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
            var groupCall = await callOrchestrator.StartGroupCall(newCallReq);
            if (groupCall == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }
            else
            {
                var response = req.CreateResponse(HttpStatusCode.Accepted);
                await response.WriteAsJsonAsync(groupCall);
                return response;
            }
        }
        else
        {
            logger.LogError($"Unrecognized request body: {responseBodyRaw}");
            return SendBadRequest(req);
        }
    }


    [Function(nameof(GetActiveCalls))]
    public async Task<HttpResponseData> GetActiveCalls([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        if (!callStateManager.Initialised) await callStateManager.Initialise();

        var calls = await callStateManager.GetActiveCalls();
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(calls);
        return response;
    }

    HttpResponseData SendBadRequest(HttpRequestData req)
    {
        logger.LogWarning("Unrecognized request body.");
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
