using Microsoft.Extensions.Logging;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace ServiceHostedMediaCallingBot.Engine.CallingBots;

/// <summary>
/// A bot that plays service-hosted audio and responds to DTMF input. Can be used for Teams calls or PSTN calls.
/// </summary>
public abstract class AudioPlaybackAndDTMFCallingBot<T> : BaseStatelessGraphCallingBot<T> where T : BaseActiveCallState, new()
{
    protected AudioPlaybackAndDTMFCallingBot(RemoteMediaCallingBotConfiguration botOptions, ICallStateManager<T> callStateManager, ILogger logger) : base(botOptions, callStateManager, logger)
    {
    }

    protected override async Task CallConnectedWithP2PAudio(T callState)
    {
        if (callState.CallId != null)
        {
            // Play the prompt found in the call state
            await SubscribeToToneAsync(callState.CallId);
            await PlayPromptAsync(callState, callState.BotMediaPlaylist.Select(m => m.Value));
        }
        else
        {
            _logger.LogWarning("CallConnected: callState.CallId is null");
        }
        await base.CallConnectedWithP2PAudio(callState);
    }
}
