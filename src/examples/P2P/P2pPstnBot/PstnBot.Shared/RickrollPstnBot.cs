using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace PstnBot.Shared;

/// <summary>
/// A bot that calls you over the phone.
/// </summary>
public class RickrollPstnBot : PstnCallingBot<BaseActiveCallState>
{
    public const string NotificationPromptName = "NotificationPrompt";

    /// <summary>
    /// Initializes a new instance of the <see cref="RickrollPstnBot" /> class.
    /// </summary>
    public RickrollPstnBot(SingleWavFileBotConfig botOptions, ILogger<RickrollPstnBot> logger, ICallStateManager<BaseActiveCallState> callStateManager) 
        : base(botOptions, callStateManager, logger)
    {
        // Generate media prompts. Used later in call & need to have consistent IDs.
        MediaMap[NotificationPromptName] = new MediaPrompt
        {
            MediaInfo = new MediaInfo
            {
                Uri = new Uri(botOptions.BotBaseUrl + botOptions.RelativeWavCallbackUrl).ToString(),
                ResourceId = Guid.NewGuid().ToString(),
            },
        };
    }
}
