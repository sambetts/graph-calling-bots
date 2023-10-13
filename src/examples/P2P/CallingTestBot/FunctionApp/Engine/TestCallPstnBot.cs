using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace CallingTestBot.FunctionApp.Engine;

/// <summary>
/// A bot that calls a PSTN number and plays a wav file, and hangs up. Results of call test are recorded in Azure table storage.
/// Call is started by a function app; this class just handles the responses once call starts.
/// </summary>
public class TestCallPstnBot : PstnCallingBot<BaseActiveCallState>
{
    public const string NotificationPromptName = "NotificationPrompt";
    private readonly CallingTestBotConfig _callingTestBotConfig;
    private readonly IBotTestsLogger _botTestsLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestCallPstnBot" /> class.
    /// </summary>
    public TestCallPstnBot(SingleWavFileBotConfig botOptions, CallingTestBotConfig callingTestBotConfig, ILogger<TestCallPstnBot> logger, ICallStateManager<BaseActiveCallState> callStateManager, IBotTestsLogger botTestsLogger)
        : base(botOptions, callStateManager, logger)
    {
        _callingTestBotConfig = callingTestBotConfig;
        _botTestsLogger = botTestsLogger;
    }

    #region Call Events

    protected override async Task CallEstablishing(BaseActiveCallState callState)
    {
        await base.CallEstablishing(callState);

        await InitLoggerIfNotAlready();
        if (callState.CallId != null)
        {
            // New call. Let's see if it works.
            await _botTestsLogger.LogNewCallEstablishing(callState.CallId, _callingTestBotConfig.TestNumber);
        }
    }

    protected override async Task CallEstablished(BaseActiveCallState callState)
    {
        await base.CallEstablished(callState);

        await InitLoggerIfNotAlready();
        if (callState.CallId != null)
        {
            // Call worked. Update logs.
            await _botTestsLogger.LogCallConnectedSuccesfully(callState.CallId);
        }
    }

    protected override async Task PlayPromptFinished(BaseActiveCallState callState)
    {
        await base.PlayPromptFinished(callState);

        if (callState.CallId != null)
        {
            await base.HangUp(callState.CallId);
        }
    }

    protected override async Task CallTerminated(string callId, ResultInfo resultInfo)
    {
        await base.CallTerminated(callId, resultInfo);

        var callState = await _botTestsLogger.GetTestCallState(callId);
        if (callState != null)
        {
            if (!callState.CallConnectedOk)
            {
                // Raise something we can trap in App Insights and alert on.
                _logger.LogError($"TEST CALL FAIL: Call {callId} terminated without connecting succesfully. Check configuration.");
            }
        }

        // Update logs.
        await _botTestsLogger.LogCallTerminated(callId, resultInfo);
    }

    #endregion

    private async Task InitLoggerIfNotAlready()
    {
        if (!_botTestsLogger.Initialised)
        {
            await _botTestsLogger.Initialise();
        }
    }
}
