using CommonUtils;
using GroupCalls.Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
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
    private readonly GroupCallStartBot _callingBot;
    private readonly ICallStateManager<GroupCallActiveCallState> _callStateManager;

    public HttpFunctions(ILoggerFactory loggerFactory, GroupCallStartBot callingBot, ICallStateManager<GroupCallActiveCallState> callStateManager)
    {
        _logger = loggerFactory.CreateLogger<HttpFunctions>();
        _callingBot = callingBot;
        _callStateManager = callStateManager;
    }

    /// <summary>
    /// Handle Graph call notifications. Must be anonymous.
    /// </summary>
    [Function(nameof(CallNotification))]
    public async Task<HttpResponseData> CallNotification([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var (notifications, body) = await GetBody<CommsNotificationsPayload>(req);

        if (notifications != null)
        {
            _logger.LogDebug($"Processing {notifications.CommsNotifications.Count} Graph call notification(s)");
            try
            {
                await _callingBot.HandleNotificationsAndUpdateCallStateAsync(notifications, body);
            }
            catch (Exception ex)
            {
                var exResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                exResponse.WriteString(ex.ToString());
                return exResponse;
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
            var call = await _callingBot.StartGroupCall(newCallReq);

            if (call != null)
            {
                var response = req.CreateResponse(HttpStatusCode.Accepted);
                await response.WriteAsJsonAsync(call);
                return response;
            }
            else
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }
        }
        else
        {
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
