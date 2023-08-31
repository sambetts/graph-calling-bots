using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;
using System.Runtime.CompilerServices;

namespace CallingTestBot.FunctionApp.Engine;

public class TestCallPstnBot : PstnCallingBot<BaseActiveCallState>
{
    public const string NotificationPromptName = "NotificationPrompt";
    private readonly BotTestsManager _botTestsManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestCallPstnBot" /> class.
    /// </summary>
    public TestCallPstnBot(SingleWavFileBotConfig botOptions, ILogger<TestCallPstnBot> logger, ICallStateManager<BaseActiveCallState> callStateManager, BotTestsManager botTestsManager)
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
        _botTestsManager = botTestsManager;
    }

    protected override async Task CallEstablishing(BaseActiveCallState callState)
    {
        await base.CallEstablishing(callState);

        await _botTestsManager.CallConnectedSuccesfully(callState.CallId);
    }

    protected override async Task CallEstablished(BaseActiveCallState callState)
    {
        await base.CallEstablished(callState);

        await _botTestsManager.CallConnectedSuccesfully(callState.CallId);
    }

    protected override async Task CallTerminated(string callId, ServiceHostedMediaCallingBot.Engine.Models.ResultInfo resultInfo)
    {
        await base.CallTerminated(callId, resultInfo);


        await _botTestsManager.CallTerminated(callId);
    }
}
