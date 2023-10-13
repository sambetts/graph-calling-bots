using CommonUtils;
using GroupCalls.Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ServiceHostedMediaCallingBot.Engine.Models;
using System.Net;
using System.Reflection;
using System.Text.Json;

namespace GroupCallingBotFunctionsApp.FunctionApp;

/// <summary>
/// Azure Functions implementation of PSTN bot.
/// </summary>
public class HttpFunctions
{
    private readonly ILogger _logger;
    private readonly GroupCallStartBot _callingBot;

    public HttpFunctions(ILoggerFactory loggerFactory, GroupCalls.Common.GroupCallStartBot callingBot)
    {
        _logger = loggerFactory.CreateLogger<HttpFunctions>();
        _callingBot = callingBot;
    }

    /// <summary>
    /// Handle Graph call notifications.
    /// </summary>
    [Function(nameof(CallNotification))]
    public async Task<HttpResponseData> CallNotification([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var notifications = await GetBody<CommsNotificationsPayload>(req);

        if (notifications != null)
        {
            _logger.LogDebug($"Processing {notifications.CommsNotifications.Count} Graph call notification(s)");
            try
            {
                await _callingBot.HandleNotificationsAndUpdateCallStateAsync(notifications);
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
    /// Send WAV file for call. Recommended: use CDN to deliver content.
    /// </summary>
    [Function(HttpRouteConstants.WavFileActionName)]
    public async Task<HttpResponseData> WavFile([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
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

        var newCallReq = await GetBody<StartGroupCallData>(req);
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

    HttpResponseData SendBadRequest(HttpRequestData req)
    {
        _logger.LogWarning("Unrecognized request body.");
        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        return response;
    }

    async Task<T?> GetBody<T>(HttpRequestData req)
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

        return notifications;
    }
}
