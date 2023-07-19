using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Bot.Builder;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Dialogs;
using GroupCallingChatBot.Web.Dialogues;

namespace GroupCallingChatBot.FunctionApp;

public class Chat
{
    private readonly ILogger _logger;
    private readonly CloudAdapter _adapter;
    private readonly IBot _bot;

    public Chat(ILoggerFactory loggerFactory, CloudAdapter adapter, IBot bot)
    {
        _logger = loggerFactory.CreateLogger<Chat>();
        _adapter = adapter;
        _bot = bot;
    }

    [Function("Function1")]
    public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        response.WriteString("Welcome to Azure Functions!");
        return response;
    }

    [Function("messages")]
    public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req, CancellationToken hostCancellationToken)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        var authHeader =
            String.Join(' ',
                req.Headers
                .Where(x => (x.Key.ToUpper() == "AUTHORIZATION"))
                .Select(x => x.Value)
                .SelectMany(x => x)
            );
        var activity = JsonConvert.DeserializeObject<Activity>(requestBody);
        _logger.LogInformation($"ChannelId: {activity?.ChannelId}");
        try
        {
            var response = await _adapter.ProcessActivityAsync(authHeader, activity, BotLogic, hostCancellationToken);
        }
        catch (Exception ex)
        {
            log.LogError(ex.ToString());
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }
    async Task BotLogic(ITurnContext turnContext, CancellationToken cancellationToken)
    {
        if (turnContext.Activity.Type == ActivityTypes.Message)
        {
            var state = turnContext.TurnState.Get<ConversationState>(typeof(ConversationState).FullName);
            await new MainDialog(_factory, _config).Run(
                turnContext,
                state.CreateProperty<DialogState>("DialogState"),
                cancellationToken);
        }
    }
}
