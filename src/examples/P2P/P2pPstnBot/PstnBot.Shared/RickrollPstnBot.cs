using GraphCallingBots;
using GraphCallingBots.CallingBots;
using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using Microsoft.Extensions.Logging;

namespace PstnBot.Shared;

/// <summary>
/// A bot that calls you over the phone.
/// </summary>
public class RickrollPstnBot : PstnCallingBot<BaseActiveCallState, RickrollPstnBot>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RickrollPstnBot" /> class.
    /// </summary>
    public RickrollPstnBot(SingleWavFileBotConfig botOptions, BotCallRedirector<RickrollPstnBot, BaseActiveCallState> botCallRedirector, ILogger<RickrollPstnBot> logger, ICallStateManager<BaseActiveCallState> callStateManager,
        ICallHistoryManager<BaseActiveCallState> historyManager)
        : base(botOptions, botCallRedirector, callStateManager, historyManager, logger)
    {
    }
}
