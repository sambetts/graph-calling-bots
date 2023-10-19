using Microsoft.Extensions.Logging;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace ServiceHostedMediaCallingBot.Engine.CallingBots;

/// <summary>
/// A bot that plays service-hosted audio and responds to DTMF input. Can be used for Teams calls or PSTN calls.
/// </summary>
public abstract class AudioPlaybackAndDTMFCallingBot<T> : BaseStatelessGraphCallingBot<T> where T : BaseActiveCallState, new()
{
    protected AudioPlaybackAndDTMFCallingBot(RemoteMediaCallingBotConfiguration botOptions, ICallStateManager<T> callStateManager, ICallHistoryManager<T> callHistoryManager, ILogger logger) : base(botOptions, callStateManager, callHistoryManager, logger)
    {
    }

    protected override async Task CallConnectedWithP2PAudio(T callState)
    {
        if (callState.CallId != null)
        {
            // Play the prompt found in the call state
            await SubscribeToToneAsync(callState.CallId);
            await PlayConfiguredMediaIfNotAlreadyPlaying(callState);
        }
        else
        {
            _logger.LogWarning("CallConnected: callState.CallId is null");
        }
        await base.CallConnectedWithP2PAudio(callState);
    }


    protected async Task PlayConfiguredMediaIfNotAlreadyPlaying(T callState)
    {
        // Don't play media if already playing
        var alreadyPlaying = false;
        foreach (var itemToPlay in callState.BotMediaPlaylist.Values)
        {
            if (callState.MediaPromptsPlaying.Select(p => p.MediaInfo.ResourceId).Contains(itemToPlay.MediaInfo.ResourceId))
            {
                alreadyPlaying = true;
                break;
            }
        }

        // But if not playing, play notification prompt again
        if (!alreadyPlaying)
        {
            try
            {
                await PlayPromptAsync(callState, callState.BotMediaPlaylist.Select(m => m.Value));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error playing prompt");
            }
        }
    }
}
