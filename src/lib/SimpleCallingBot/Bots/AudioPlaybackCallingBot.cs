using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using SimpleCallingBotEngine.Models;

namespace SimpleCallingBotEngine.Bots;

/// <summary>
/// A bot that plays service-hosted audio and responds to DTMF input. Can be used for Teams calls or PSTN calls.
/// </summary>
public abstract class AudioPlaybackAndDTMFCallingBot : BaseStatelessGraphCallingBot
{
    protected AudioPlaybackAndDTMFCallingBot(RemoteMediaCallingBotConfiguration botOptions, ICallStateManager callStateManager, ILogger logger) : base(botOptions, callStateManager, logger)
    {
    }

    // Supported media: https://learn.microsoft.com/en-us/graph/api/resources/mediainfo?view=graph-rest-1.0
    protected Dictionary<string, MediaPrompt> MediaMap { get; } = new();

    protected override async Task CallConnectedWithAudio(ActiveCallState callState)
    {
        if (callState.CallId != null)
        {
            await base.SubscribeToToneAsync(callState.CallId);
            await base.PlayPromptAsync(callState, MediaMap.Select(m=> m.Value) );
        }
        else
        {
            _logger.LogWarning("CallConnected: callState.CallId is null");
        }
    }
}
