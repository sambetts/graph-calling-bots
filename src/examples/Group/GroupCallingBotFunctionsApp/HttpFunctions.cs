using CommonUtils;
using GraphCallingBots;
using GraphCallingBots.CallingBots;
using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using GroupCalls.Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using System.Net;
using System.Reflection;
using System.Text.Json;

namespace GroupCallingBot.FunctionApp;

/// <summary>
/// Azure Functions implementation of PSTN bot.
/// </summary>
public class HttpFunctions(ILogger<HttpFunctions> logger, GroupCallOrchestrator callOrchestrator,
    ICallStateManager<BaseActiveCallState> callStateManager,
    BotCallRedirector<GroupCallBot, BaseActiveCallState> botCallRedirectorGroupCall,
    BotCallRedirector<CallInviteBot, GroupCallInviteActiveCallState> botCallRedirectorCallInviteCall)
{


    /// <summary>
    /// Start call triggered by HTTP request.
    /// </summary>
    [Function(nameof(StartCall))]
    public async Task<HttpResponseData> StartCall([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var (newCallReq, responseBodyRaw) = await GetBody<StartGroupCallData>(req);
        if (newCallReq != null)
        {
            Call? groupCall;
            try
            {
                groupCall = await callOrchestrator.StartGroupCall(newCallReq);
            }
            catch (ArgumentException ex)
            {
                logger.LogError($"Argument exception: {ex.Message}");
                return SendBadRequest(req);
            }
            catch (Microsoft.Graph.Models.ODataErrors.ODataError ex)
            {
                // Graph API returned an error
                logger.LogError($"Graph API error {ex.ResponseStatusCode} creating call");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteAsJsonAsync(ex.ToString());
                return response;
            }
            catch (Exception ex)
            {
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteAsJsonAsync(ex.ToString());
                return response;
            }

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

    [Function(nameof(GetActiveCalls))]
    public async Task<HttpResponseData> GetActiveCalls([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        if (!callStateManager.Initialised) await callStateManager.Initialise();

        var calls = await callStateManager.GetActiveCalls();
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(calls);
        return response;
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
            foreach (var notification in notificationsPayload.CommsNotifications)
            {
                var callId = BaseActiveCallState.GetCallId(notification.ResourceUrl);
                if (callId != null)
                {
                    var botGroupCall = await GetBotAndHandleNotifications(botCallRedirectorGroupCall, callId, notificationsPayload);

                    if (botGroupCall != null)        // Logging for negative handled in GetBotByCallId
                    {
                        logger.LogInformation($"Processing {notificationsPayload.CommsNotifications.Count} Graph call notification(s) for GroupCall bot.");
                        try
                        {
                            await botGroupCall.HandleNotificationsAndUpdateCallStateAsync(notificationsPayload);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"Error handling notifications: {ex.Message}");
                            var exResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                            exResponse.WriteString(ex.ToString());
                            return exResponse;
                        }
                    }
                    else
                    {

                        var botInviteCall = await GetBotAndHandleNotifications(botCallRedirectorCallInviteCall, callId, notificationsPayload);
                        if (botInviteCall != null)
                        {
                            logger.LogInformation($"Processing {notificationsPayload.CommsNotifications.Count} Graph call notification(s) for CallInvite bot.");

                            try
                            {
                                await botInviteCall.HandleNotificationsAndUpdateCallStateAsync(notificationsPayload);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError($"Error handling notifications: {ex.Message}");
                                var exResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                                exResponse.WriteString(ex.ToString());
                                return exResponse;
                            }
                        }
                        else
                        {
                            logger.LogWarning($"No bot found for call ID {callId} in notification {notification.ResourceUrl}");
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

    private async Task<BOTTYPE?> GetBotAndHandleNotifications<BOTTYPE, CALLSTATETYPE>(
        BotCallRedirector<BOTTYPE, CALLSTATETYPE> botCallRedirector, string callId, CommsNotificationsPayload notificationsPayload)
        where BOTTYPE : BaseBot<CALLSTATETYPE>
        where CALLSTATETYPE : BaseActiveCallState, new()
    {
        var bot = await botCallRedirector.GetBotByCallId(callId);
        if (bot != null)
        {
            await bot.HandleNotificationsAndUpdateCallStateAsync(notificationsPayload);
        }

        return bot;
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
