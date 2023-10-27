using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using PstnBot.Shared;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using System.Net;
using System.Text.Json;

namespace PstnBot.FunctionApp;

/// <summary>
/// Azure Functions implementation of PSTN bot.
/// </summary>
public class HttpFunctions
{
    private readonly ILogger _logger;
    private readonly IPstnCallingBot _callingBot;
    private readonly SingleWavFileBotConfig _botConfig;

    public HttpFunctions(ILoggerFactory loggerFactory, IPstnCallingBot callingBot, SingleWavFileBotConfig botConfig)
    {
        _logger = loggerFactory.CreateLogger<HttpFunctions>();
        _callingBot = callingBot;
        _botConfig = botConfig;
    }

    /// <summary>
    /// Handle Graph call notifications.
    /// </summary>
    [Function(nameof(CallNotification))]
    public async Task<HttpResponseData> CallNotification([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var (notifications, rawBody) = await GetBody<CommsNotificationsPayload>(req);

        if (notifications != null)
        {
            _logger.LogDebug($"Processing Graph call notification");
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
    public async Task<HttpResponseData> WavFile([HttpTrigger(AuthorizationLevel.Anonymous, "get", "head")] HttpRequestData req)
    {
        // Use embedded WAV file to avoid external dependencies. Not recommended for production.
        using (var memoryStream = new MemoryStream())
        {
            Properties.Resources.RickrollWavFile.CopyTo(memoryStream);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteBytesAsync(memoryStream.ToArray());
            response.Headers.Add("Content-Type", "audio/wav");
            return response;
        }
    }

    /// <summary>
    /// Start call triggered by HTTP request.
    /// </summary>
    [Function(nameof(StartCall))]
    public async Task<HttpResponseData> StartCall([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var (startCall, rawBody) = await GetBody<StartCallData>(req);

        if (startCall != null)
        {
            _logger.LogInformation($"Starting new call to number {startCall.PhoneNumber}");
            try
            {
                await _callingBot.StartPTSNCall(startCall.PhoneNumber, _botConfig.WavCallbackUrl);
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
