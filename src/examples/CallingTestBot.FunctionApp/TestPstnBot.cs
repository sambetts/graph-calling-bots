using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace CallingTestBot.FunctionApp;

public class TestPstnBot : PstnCallingBot<BaseActiveCallState>
{
    public const string NotificationPromptName = "NotificationPrompt";

    /// <summary>
    /// Initializes a new instance of the <see cref="TestPstnBot" /> class.
    /// </summary>
    public TestPstnBot(SingleWavFileBotConfig botOptions, ILogger<TestPstnBot> logger, ICallStateManager<BaseActiveCallState> callStateManager)
        : base(botOptions, callStateManager, logger)
    {
        // Play a notification prompt when the call is answered
        MediaMap[NotificationPromptName] = new MediaPrompt
        {
            MediaInfo = new MediaInfo
            {
                Uri = new Uri(botOptions.BotBaseUrl + botOptions.RelativeWavUrl).ToString(),
                ResourceId = Guid.NewGuid().ToString(),
            },
        };
    }

    protected override Task CallConnectedWithP2PAudio(BaseActiveCallState callState)
    {
        return base.CallConnectedWithP2PAudio(callState);
    }
}
