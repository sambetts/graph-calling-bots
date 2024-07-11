using Microsoft.Extensions.Logging;
using ServiceHostedMediaCallingBot.Engine;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace PstnBot.Shared;

/// <summary>
/// A bot that calls you over the phone.
/// </summary>
public class RickrollPstnBot : PstnCallingBot<BaseActiveCallState>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RickrollPstnBot" /> class.
    /// </summary>
    public RickrollPstnBot(SingleWavFileBotConfig botOptions, ILogger<RickrollPstnBot> logger, ICallStateManager<BaseActiveCallState> callStateManager,
        ICallHistoryManager<BaseActiveCallState, CallNotification> historyManager, ILogger<BotCallRedirector> botCallRedirectorLogger)
        : base(botOptions, callStateManager, historyManager, logger, new BotCallRedirector(botCallRedirectorLogger))
    {
    }
}
