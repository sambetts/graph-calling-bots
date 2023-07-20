using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;

namespace PstnBot.FunctionApp;

public class HttpFunctions
{
    private readonly ILogger _logger;
    private readonly IGraphCallingBot _callingBot;

    public HttpFunctions(ILoggerFactory loggerFactory, IGraphCallingBot callingBot)
    {
        _logger = loggerFactory.CreateLogger<HttpFunctions>();
        _callingBot = callingBot;
    }

    [Function("CallNotification")]
    public async Task<HttpResponseData> CallNotification([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        var reqBodyContent = await req.ReadAsStringAsync();

        // Deserialise the JSON payload into a strongly typed Microsoft.Graph.Communications.Calls.Media.CommsNotificationsPayload object
        CommsNotificationsPayload? notifications = null;
        try
        {
            notifications = JsonSerializer.Deserialize<CommsNotificationsPayload>(reqBodyContent ?? string.Empty);
        }
        catch (JsonException)
        {
            // Ignore invalid JSON
        }
        if (notifications != null)
        {
            try
            {
                await _callingBot.HandleNotificationsAsync(notifications);
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
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            return response;
        }
    }
}
