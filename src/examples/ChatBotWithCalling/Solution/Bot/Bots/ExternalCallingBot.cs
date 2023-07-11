using Microsoft.Extensions.Logging;
using SimpleCallingBotEngine;
using SimpleCallingBotEngine.Bots;
using SimpleCallingBotEngine.Models;

namespace Bot.Bots;

public class ExternalCallingBot : PstnCallingBot
{
    public ExternalCallingBot(RemoteMediaCallingBotConfiguration botOptions, ICallStateManager callStateManager, ILogger<ExternalCallingBot> logger) 
        : base(botOptions, callStateManager, logger, botOptions.BotBaseUrl + HttpRouteConstants.CallNotificationsRoute)
    {


    }
}
