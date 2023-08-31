using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace CallingTestBot.FunctionApp.Engine;

public class TestCallPstnBot : PstnCallingBot<BaseActiveCallState>
{
    public const string NotificationPromptName = "NotificationPrompt";
    private readonly IBotTestsLogger _botTestsLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestCallPstnBot" /> class.
    /// </summary>
    public TestCallPstnBot(SingleWavFileBotConfig botOptions, ILogger<TestCallPstnBot> logger, ICallStateManager<BaseActiveCallState> callStateManager, IBotTestsLogger botTestsLogger)
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
        _botTestsLogger = botTestsLogger;
    }

    protected override async Task CallEstablishing(BaseActiveCallState callState)
    {
        await base.CallEstablishing(callState);

        if (callState.CallId != null)
        {
            await _botTestsLogger.LogNewCallEstablishing(callState.CallId);
        }
    }

    protected override async Task CallEstablished(BaseActiveCallState callState)
    {
        await base.CallEstablished(callState);

        if (callState.CallId != null)
        {
            await _botTestsLogger.LogCallConnectedSuccesfully(callState.CallId);
        }
    }

    protected override async Task CallTerminated(string callId, ResultInfo resultInfo)
    {
        await base.CallTerminated(callId, resultInfo);

        await _botTestsLogger.LogCallTerminated(callId, resultInfo);
    }
}
