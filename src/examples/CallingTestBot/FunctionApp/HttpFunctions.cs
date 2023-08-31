using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;

namespace CallingTestBot.FunctionApp;

/// <summary>
/// Azure Functions implementation of PSTN bot.
/// </summary>
public class HttpFunctions
{
    private readonly ILogger _logger;
    private readonly IPstnCallingBot _callingBot;
    private readonly CallingTestBotConfig _callingTestBotConfig;

    public HttpFunctions(ILoggerFactory loggerFactory, IPstnCallingBot callingBot, CallingTestBotConfig callingTestBotConfig)
    {
        _logger = loggerFactory.CreateLogger<HttpFunctions>();
        _callingBot = callingBot;
        _callingTestBotConfig = callingTestBotConfig;
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
            _logger.LogInformation($"Processing Graph call notification");
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
        // Use embedded WAV file to avoid external dependencies. Not recommended for production.
        using (var memoryStream = new MemoryStream())
        {
            using (var localWavStream = ReadResource("CallingTestBot.FunctionApp.testcall.wav"))
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
    /// Start call triggered by HTTP request. Not normally needed as done on a timer.
    /// </summary>
    [Function(nameof(StartCall))]
    public async Task<HttpResponseData> StartCall([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        _logger.LogInformation($"Starting new call to number {_callingTestBotConfig.TestNumber}");
        try
        {
            await _callingBot.StartPTSNCall(_callingTestBotConfig.TestNumber);
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

    Stream ReadResource(string resourcePath)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
        var stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream != null)
        {
            return stream;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(resourcePath), $"No resource found by name '{resourcePath}'");
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
