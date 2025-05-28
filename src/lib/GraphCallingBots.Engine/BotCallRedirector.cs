using GraphCallingBots.CallingBots;
using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace GraphCallingBots;

/// <summary>
/// Makes sure the right bot instance get the right call notifications.
/// </summary>
public class BotCallRedirector<BOTTYPE, CALLSTATETYPE>(RemoteMediaCallingBotConfiguration config,
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

        var typeName = await callStateManager.GetBotTypeNameByCallId(callId);
        if (typeName == null) {
            logger.LogWarning($"{nameof(BotCallRedirector<BOTTYPE, CALLSTATETYPE>)} - No bot found for call {callId} - was this call created before?");
            return null;
        }

        // Compare type names to ensure the correct bot type is used
        if (typeName != typeof(BOTTYPE).FullName)
        {
            logger.LogWarning($"{nameof(BotCallRedirector<BOTTYPE, CALLSTATETYPE>)} - Call {callId} is not handled by {typeof(BOTTYPE).FullName}, but by {typeName}");
            return null;
        }

        var bot = BaseBot<CALLSTATETYPE>.HydrateBot<BOTTYPE, CALLSTATETYPE>(config, callStateManager, callHistoryManager, logger);
        if (bot != null)
        {
            _botMemCache.Add(callId, bot);
        }

        return bot;
    }

    internal async Task AddCall(string callId, BOTTYPE baseStatelessGraphCallingBot)
    {
        if (!_botMemCache.ContainsKey(callId))
        {
            _botMemCache.Add(callId, baseStatelessGraphCallingBot);
            await callStateManager.AddCall(callId, baseStatelessGraphCallingBot.GetType().FullName ?? throw new Exception("No type name for bot"));
            logger.LogInformation($"{nameof(BotCallRedirector<BOTTYPE, CALLSTATETYPE>)} - Added call {callId} to bot redirector");
        }
        else
        {
            logger.LogWarning($"{nameof(BotCallRedirector<BOTTYPE, CALLSTATETYPE>)} - Call {callId} already exists in bot redirector");
        }
    }
}
