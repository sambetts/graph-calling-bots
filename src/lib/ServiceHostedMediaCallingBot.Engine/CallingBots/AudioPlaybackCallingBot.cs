﻿using Microsoft.Extensions.Logging;
using Microsoft.Graph;
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

    // Supported media: https://learn.microsoft.com/en-us/graph/api/resources/mediainfo?view=graph-rest-1.0
    protected Dictionary<string, MediaPrompt> MediaMap { get; } = new();

    protected override async Task CallConnectedWithP2PAudio(T callState)
    {
        if (callState.CallId != null)
        {
            await SubscribeToToneAsync(callState.CallId);
            await PlayPromptAsync(callState, MediaMap.Select(m => m.Value));
        }
        else
        {
            _logger.LogWarning("CallConnected: callState.CallId is null");
        }
        await base.CallConnectedWithP2PAudio(callState);
    }
}
