using Microsoft.Extensions.Logging;
using ServiceHostedMediaCallingBot.Engine;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace ServiceHostedMediaCallingBot.UnitTests;

/// <summary>
/// Does nothing; just for testing.
/// </summary>
internal class UnitTestBot : BaseGraphCallingBot<BaseActiveCallState>
{
    public UnitTestBot(RemoteMediaCallingBotConfiguration botConfig, ICallStateManager<BaseActiveCallState> callStateManager, ICallHistoryManager<BaseActiveCallState,
        CallNotification> callHistoryManager, ILogger logger, ILogger<BotCallRedirector> botCallRedirectorLogger) 
        : base(botConfig, callStateManager, callHistoryManager, logger, new BotCallRedirector(botCallRedirectorLogger))
    {
    }
}
