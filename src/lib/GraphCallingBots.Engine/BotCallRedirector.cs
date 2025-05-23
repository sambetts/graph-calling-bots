using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphCallingBots;

/// <summary>
/// Makes sure the right bot instance get the right call notifications.
/// </summary>
public class BotCallRedirector(ILogger<BotCallRedirector> logger)
{
    private readonly Dictionary<string, ICommsNotificationsPayloadHandler> _botInstances = new();

    public ICommsNotificationsPayloadHandler? GetBotByCallId(string callId)
    {
        if (_botInstances.ContainsKey(callId))
        {
            return _botInstances[callId];
        }
        logger.LogWarning($"{nameof(BotCallRedirector)} - No bot found for call {callId} - was this call created before?");

        return null;
    }

    internal void AddCall(string callId, ICommsNotificationsPayloadHandler baseStatelessGraphCallingBot)
    {
        if (!_botInstances.ContainsKey(callId))
        {
            _botInstances.Add(callId, baseStatelessGraphCallingBot);
            logger.LogInformation($"{nameof(BotCallRedirector)} - Added call {callId} to bot redirector");
        }
        else
        {
            logger.LogWarning($"{nameof(BotCallRedirector)} - Call {callId} already exists in bot redirector");
        }
    }
}
