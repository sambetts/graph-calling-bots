﻿using Microsoft.Graph;
using SimpleCallingBotEngine;
using SimpleCallingBotEngine.Bots;
using SimpleCallingBotEngine.Models;

namespace PstnBot;

/// <summary>
/// A bot that calls you over the phone.
/// </summary>
public class RickrollPstnBot : PstnCallingBot<ActiveCallState>
{
    public const string NotificationPromptName = "NotificationPrompt";

    /// <summary>
    /// Initializes a new instance of the <see cref="RickrollPstnBot" /> class.
    /// </summary>
    public RickrollPstnBot(RemoteMediaCallingBotConfiguration botOptions, ILogger logger, ICallStateManager<ActiveCallState> callStateManager) : base(botOptions, callStateManager, logger)
    {
        // Generate media prompts. Used later in call & need to have consistent IDs.
        this.MediaMap[NotificationPromptName] = new MediaPrompt
        {
            MediaInfo = new MediaInfo
            {
                Uri = new Uri(botOptions.BotBaseUrl + "/audio/rickroll.wav").ToString(),
                ResourceId = Guid.NewGuid().ToString(),
            },
        };
    }

}
