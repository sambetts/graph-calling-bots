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
    private readonly Dictionary<string, BOTTYPE> _botMemCache = new();

    public async Task<BOTTYPE?> GetBotByCallId(string callId)
    {
        if (_botMemCache.ContainsKey(callId))
        {
            return _botMemCache[callId];
        }

        await callStateManager.Initialise();

        var typeNameForCallId = await callStateManager.GetBotTypeNameByCallId(callId);
        if (typeNameForCallId == null) {
            logger.LogWarning($"{nameof(BotCallRedirector<BOTTYPE, CALLSTATETYPE>)} - No bot found for call {callId} - was this call created before?");
            return null;
        }

        var bot = BaseBot<CALLSTATETYPE>.HydrateBot<BOTTYPE, CALLSTATETYPE>(config, this, callStateManager, callHistoryManager, logger);


        // Compare type names to ensure the correct bot type is used
        if (typeNameForCallId != bot.BotTypeName)
        {
            logger.LogDebug($"{nameof(BotCallRedirector<BOTTYPE, CALLSTATETYPE>)} - call {callId} is not handled by '{typeof(BOTTYPE).FullName}', but by '{typeNameForCallId}'");
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
        // Associate the call with this bot type in the call state manager
        var initialState = new CALLSTATETYPE
        {
            ResourceUrl = BaseActiveCallState.GetResourceUrlFromCallId(callId),
            BotClassNameFull = bot.BotTypeName
        };
        await callStateManager.AddCallStateOrUpdate(initialState);
    }
}
