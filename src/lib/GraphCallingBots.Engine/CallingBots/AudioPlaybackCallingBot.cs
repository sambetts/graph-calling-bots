using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using Microsoft.Extensions.Logging;

namespace GraphCallingBots.CallingBots;

/// <summary>
/// A bot that plays service-hosted audio and responds to DTMF input (dial-tones). Can be used for Teams calls or PSTN calls.
/// </summary>
public abstract class AudioPlaybackAndDTMFCallingBot<CALLSTATETYPE> : BaseGraphCallingBot<CALLSTATETYPE> where CALLSTATETYPE : BaseActiveCallState, new()
{
    public const string DEFAULT_PROMPT_ID = "defaultPrompt";
    protected AudioPlaybackAndDTMFCallingBot(
        RemoteMediaCallingBotConfiguration botOptions, 
        BotCallRedirector<BaseGraphCallingBot<CALLSTATETYPE>, CALLSTATETYPE> botCallRedirector, 
        ICallStateManager<CALLSTATETYPE> callStateManager, 
        ICallHistoryManager<CALLSTATETYPE> callHistoryManager,
        ILogger logger)
        : base(botOptions, botCallRedirector, callStateManager, callHistoryManager, logger)
    {
    }

    protected override async Task CallConnectedWithP2PAudio(CALLSTATETYPE callState)
    {
        if (callState.CallId != null)
        {
            // Play the default prompt found in the call state
            await SubscribeToToneAsync(callState.CallId);
            await PlayConfiguredMediaIfNotAlreadyPlaying(callState, DEFAULT_PROMPT_ID);
        }
        else
        {
            _logger.LogWarning("CallConnected: callState.CallId is null");
        }
        await base.CallConnectedWithP2PAudio(callState);
    }

    protected async Task PlayConfiguredMediaIfNotAlreadyPlaying(CALLSTATETYPE callState, string wantedPromptId)
    {
        // Don't play media if already playing
        var alreadyPlaying = false;

        if (callState.MediaPromptsPlaying.Select(p => p.MediaInfo!.ResourceId).Contains(wantedPromptId))
        {
            alreadyPlaying = true;
            _logger.LogInformation("Already playing prompt {PromptId}", wantedPromptId);
            return;
        }

        // But if not playing, play notification prompt again
        var prompt = callState.BotMediaPlaylist.FirstOrDefault(m => m.Value.MediaInfo?.ResourceId == wantedPromptId).Value;
        if (alreadyPlaying)
        {
            _logger.LogInformation("Prompt {PromptId} is already playing", wantedPromptId);
            return;
        }

        if (prompt != null)
        {
            try
            {
                await PlayPromptAsync(callState, prompt);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error playing prompt");
            }
        }
        else
        {
            _logger.LogWarning("Prompt {PromptId} not found in playlist", wantedPromptId);
        }
    }
}
