using GraphCallingBots.CallingBots;
using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using Microsoft.Extensions.Logging;

namespace GraphCallingBots;

/// <summary>
/// Makes sure the right bot instance get the right call notifications.
/// </summary>
public class BotCallRedirector<BOTTYPE, CALLSTATETYPE>(
        RemoteMediaCallingBotConfiguration config,
        ICallStateManager<CALLSTATETYPE> callStateManager,
        ICallHistoryManager<CALLSTATETYPE> callHistoryManager,
        ILogger<BOTTYPE> logger)
        where BOTTYPE : BaseBot<CALLSTATETYPE>
        where CALLSTATETYPE : BaseActiveCallState, new()
{
    private readonly Dictionary<string, BaseBot<CALLSTATETYPE>> _botMemCache = new();
    private string botRedirectorTypeString = $"{nameof(BotCallRedirector<BOTTYPE, CALLSTATETYPE>)}<{typeof(BOTTYPE).Name},{typeof(CALLSTATETYPE).Name}>";

    /// <summary>
    /// Get a bot instance by call ID. Won't error if the bot is not found or of the expected type, just returns null.
    /// </summary>
    public async Task<BOTTYPE?> GetBotByCallId(string callId)
    {
        if (_botMemCache.ContainsKey(callId))
        {
            return (BOTTYPE?)_botMemCache[callId];
        }

        await callStateManager.Initialise();

        var state = await callStateManager.GetStateByCallId(callId);
        if (state == null)
        {
            logger.LogWarning($"{botRedirectorTypeString} - No call state found for call {callId} in call state manager");
            return null;
        }
        var typeNameForCallId = state.BotClassNameFull;
        if (string.IsNullOrWhiteSpace(typeNameForCallId))
        {
            return null;
        }

        var bot = BaseBot<CALLSTATETYPE>.HydrateBot(config, this, callStateManager, callHistoryManager, logger);

        // Compare type names to ensure the correct bot type is used
        if (typeNameForCallId != bot.BotTypeName)
        {
            logger.LogDebug($"{botRedirectorTypeString} - call {callId} is not handled by '{typeof(BOTTYPE).FullName}', but by '{typeNameForCallId}'");
            return null;
        }

        if (bot != null)
        {
            _botMemCache.Add(callId, bot);
        }

        return bot;
    }

    public async Task RegisterBotForCall(string callId, BaseBot<CALLSTATETYPE> bot)
    {
        _botMemCache.Add(callId, bot);

        // Associate the call with this bot type in the call state manager
        var initialState = new CALLSTATETYPE
        {
            ResourceUrl = BaseActiveCallState.GetResourceUrlFromCallId(callId),
            BotClassNameFull = bot.BotTypeName
        };
        await callStateManager.AddCallStateOrUpdate(initialState);
        logger.LogDebug($"{botRedirectorTypeString} - Bot registered for call {callId} with type '{bot.BotTypeName}'");
    }
}
