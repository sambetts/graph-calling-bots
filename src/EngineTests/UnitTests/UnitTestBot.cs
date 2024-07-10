using Microsoft.Extensions.Logging;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace ServiceHostedMediaCallingBot.UnitTests;

/// <summary>
/// Does nothing; just for testing.
/// </summary>
internal class UnitTestBot : BaseStatelessGraphCallingBot<BaseActiveCallState>
{
    public UnitTestBot(RemoteMediaCallingBotConfiguration botConfig, ICallStateManager<BaseActiveCallState> callStateManager, ICallHistoryManager<BaseActiveCallState, CallNotification> callHistoryManager, ILogger logger) : base(botConfig, callStateManager, callHistoryManager, logger)
    {
    }
}
