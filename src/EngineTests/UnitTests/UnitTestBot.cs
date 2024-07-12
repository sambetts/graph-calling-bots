using GraphCallingBots;
using GraphCallingBots.CallingBots;
using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using Microsoft.Extensions.Logging;

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
