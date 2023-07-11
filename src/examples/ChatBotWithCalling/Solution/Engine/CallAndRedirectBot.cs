using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using SimpleCallingBotEngine;
using SimpleCallingBotEngine.Bots;
using SimpleCallingBotEngine.Models;

namespace Engine;

public class CallAndRedirectBot : PstnCallingBot
{
    public const string NotificationPromptName = "NotificationPrompt";
    private readonly ITeamsChatbotManager _teamsChatbotManager;

    public CallAndRedirectBot(ITeamsChatbotManager teamsChatbotManager, RemoteMediaCallingBotConfiguration botOptions, ICallStateManager callStateManager, ILogger<CallAndRedirectBot> logger) 
        : base(botOptions, callStateManager, logger)
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
        _teamsChatbotManager = teamsChatbotManager;
    }

    protected override async Task NewTonePressed(ActiveCallState callState, Tone tone)
    {
        if (tone == Tone.Tone1)
        {
            await _teamsChatbotManager.Transfer(callState);
        }
        await base.NewTonePressed(callState, tone);
    }
}
