using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using ServiceHostedMediaCallingBot.Engine;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;
using ServiceHostedMediaCallingBot.UnitTests.TestServices;

namespace ServiceHostedMediaCallingBot.UnitTests;

[TestClass]
public class BotNotificationsHandlerTests : BaseTests
{
    private readonly UnitTestBot _unitTestBot;
    public BotNotificationsHandlerTests()
    {
        _logger = LoggerFactory.Create(config =>
        {
            config.AddTraceSource(new System.Diagnostics.SourceSwitch("SourceSwitch"));
            config.AddConsole();
        }).CreateLogger("Unit tests");

        _unitTestBot = new UnitTestBot(new RemoteMediaCallingBotConfiguration(), _callStateManager, _historyManager, _logger);
    }

    /// <summary>
    /// Tests that call connected only fires once for multiple call updates
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task MultipleCallUpdatesTests()
    {
        var callConnectedWithP2PAudioCount = 0;
        var callEstablishingCount = 0;
        var callEstablishedCount = 0;
        var callbackInfo = new NotificationCallbackInfo<BaseActiveCallState>
        {
            CallEstablishing = (callState) =>
            {
                callEstablishingCount++;
                return Task.CompletedTask;
            },
            CallEstablished = (callState) =>
            {
                callEstablishedCount++;
                return Task.CompletedTask;
            },
            CallConnectedWithP2PAudio = (callState) =>
            {
                callConnectedWithP2PAudioCount++;
                return Task.CompletedTask;
            }
        };
        var notificationsManager = new BotNotificationsHandler<BaseActiveCallState>(_callStateManager, _historyManager, callbackInfo, _logger);

        var callResourceUrl = NotificationsLibrary.P2PTest2Event1Establishing.CommsNotifications[0]!.ResourceUrl!;

        // Handle call establish for a call never seen before
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.P2PTest2Event1Establishing, _unitTestBot);

        // We should find the call in the call state manager
        Assert.AreEqual(_callStateManager.GetByNotificationResourceUrl(callResourceUrl).Result!.StateEnum, CallState.Establishing);

        // Establish the call
        Assert.IsTrue(callEstablishingCount == 1);
        Assert.IsTrue(callEstablishedCount == 0);
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.P2PTest2Event2Established, _unitTestBot);
        Assert.IsTrue(callEstablishedCount == 1);

        Assert.AreEqual(_callStateManager.GetByNotificationResourceUrl(callResourceUrl).Result!.StateEnum,
            CallState.Established);
        Assert.IsTrue(callConnectedWithP2PAudioCount == 0);

        // Connect audio. Should trigger the callback
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.P2PTest2Event3UpdatedWithMediaState, _unitTestBot);
        Assert.IsTrue(callConnectedWithP2PAudioCount == 1);

        // More updates. Should not fire callConnectedWithP2PAudio
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.P2PTest2Event4UpdatedWithChatInfo, _unitTestBot);
        Assert.IsTrue(callConnectedWithP2PAudioCount == 1);

        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.P2PTest2Event5UpdatedWithRandomShit, _unitTestBot);
        Assert.IsTrue(callConnectedWithP2PAudioCount == 1);

        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.P2PTest2Event6UserJoin, _unitTestBot);
        Assert.IsTrue(callConnectedWithP2PAudioCount == 1);

    }


    /// <summary>
    /// A call is established, but failed to connect. The call is then deleted.
    /// </summary>
    [TestMethod]
    public async Task FailedP2PCallFlowTests()
    {
        await FailedCallTest(_logger, _callStateManager, _historyManager, _unitTestBot);
    }

    internal static async Task FailedCallTest(ILogger logger, ICallStateManager<BaseActiveCallState> _callStateManager, 
        ICallHistoryManager<BaseActiveCallState, CallNotification> historyManager, UnitTestBot unitTestBot)
    {
        var callEstablishingCount = 0;
        var callConnectedCount = 0;
        var callTerminatedCount = 0;
        var callbackInfo = new NotificationCallbackInfo<BaseActiveCallState>
        {
            CallEstablishing = (callState) =>
            {
                callEstablishingCount++;
                return Task.CompletedTask;
            },
            CallEstablished = (callState) =>
            {
                callConnectedCount++;
                return Task.CompletedTask;
            },
            CallTerminated = (callState, result) =>
            {
                callTerminatedCount++;
                return Task.CompletedTask;
            }
        };
        var notificationsManager = new BotNotificationsHandler<BaseActiveCallState>(_callStateManager, historyManager, callbackInfo, logger);

        var callResourceUrl = NotificationsLibrary.FailedCallEstablishingP2P.CommsNotifications[0]!.ResourceUrl!;

        // Handle call establish for a call never seen before
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.FailedCallEstablishingP2P, unitTestBot);
        Assert.IsTrue(callEstablishingCount == 1);

        var postEstablishingCallState = await _callStateManager.GetByNotificationResourceUrl(callResourceUrl);
        Assert.IsNotNull(postEstablishingCallState);
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.FailedCallDeleted, unitTestBot);

        var postCallDeletedState = await _callStateManager.GetByNotificationResourceUrl(callResourceUrl);
        Assert.IsNull(postCallDeletedState);

        Assert.AreEqual(0, callConnectedCount);
        Assert.AreEqual(1, callTerminatedCount);
    }

    [TestMethod]
    public async Task P2PFlowTestsNoPreviousCallState()
    {
        var callConnectedWithP2PAudioCount = 0;
        var callEstablishingCount = 0;
        var callEstablishedCount = 0;
        var callPlayPromptFinished = 0;
        var callTerminatedCount = 0;
        var toneList = new List<Tone>();
        var callbackInfo = new NotificationCallbackInfo<BaseActiveCallState>
        {
            CallEstablishing = (callState) =>
            {
                callEstablishingCount++;
                return Task.CompletedTask;
            },
            CallEstablished = (callState) =>
            {
                callEstablishedCount++;
                return Task.CompletedTask;
            },
            CallConnectedWithP2PAudio = (callState) =>
            {
                callConnectedWithP2PAudioCount++;
                return Task.CompletedTask;
            },
            PlayPromptFinished = (callState) =>
            {
                callPlayPromptFinished++;
                return Task.CompletedTask;
            },
            NewTonePressed = (callState, tone) =>
            {
                toneList.Add(tone);
                return Task.CompletedTask;
            },
            CallTerminated = (callState, result) =>
            {
                callTerminatedCount++;
                return Task.CompletedTask;
            }
        };
        var notificationsManager = new BotNotificationsHandler<BaseActiveCallState>(_callStateManager, _historyManager, callbackInfo, _logger);

        var callResourceUrl = NotificationsLibrary.P2PTest1CallEstablishingP2P.CommsNotifications[0]!.ResourceUrl!;

        // Handle call establish for a call never seen before
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.P2PTest1CallEstablishingP2P, _unitTestBot);

        // We should find the call in the call state manager
        Assert.AreEqual(_callStateManager.GetByNotificationResourceUrl(callResourceUrl).Result!.StateEnum, CallState.Establishing);

        // Establish the call
        Assert.IsTrue(callEstablishingCount == 1);
        Assert.IsTrue(callEstablishedCount == 0);
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.P2PTest1CallEstablishedP2P, _unitTestBot);
        Assert.IsTrue(callEstablishedCount == 1);

        Assert.AreEqual(_callStateManager.GetByNotificationResourceUrl(callResourceUrl).Result!.StateEnum,
            CallState.Established);
        Assert.IsTrue(callConnectedWithP2PAudioCount == 0);

        // Connect audio. Should trigger the callback
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.P2PTest1CallEstablishedWithAudioP2P, _unitTestBot);
        Assert.IsTrue(callConnectedWithP2PAudioCount == 1);

        // Pretend we've finished playing a prompt
        var callState = await _callStateManager.GetByNotificationResourceUrl(callResourceUrl);

        // Add a media prompt to the call state
        callState!.MediaPromptsPlaying.Add(new EquatableMediaPrompt { MediaInfo = new MediaInfo { ResourceId = NotificationsLibrary.P2PTest1PlayPromptFinish!.CommsNotifications[0]!.AssociatedPlayPromptOperation!.Id } });
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.P2PTest1PlayPromptFinish, _unitTestBot);
        Assert.IsTrue(callPlayPromptFinished == 1);

        // Make sure the media prompt was removed
        Assert.IsTrue(_callStateManager.GetByNotificationResourceUrl(callResourceUrl).Result!.MediaPromptsPlaying.Count == 0);

        // Press buttons. Should trigger the callback
        Assert.IsTrue(toneList.Count == 0);
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.P2PTest1TonePress, _unitTestBot);
        Assert.IsTrue(toneList.Count == 1);

        // Terminate the call
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.P2PTest1HangUp, _unitTestBot);
        Assert.IsNull(await _callStateManager.GetByNotificationResourceUrl(callResourceUrl));
        Assert.IsTrue(callConnectedWithP2PAudioCount == 1);
        Assert.IsTrue(callTerminatedCount == 1);

        Assert.IsTrue((await _callStateManager.GetActiveCalls()).Count == 0);
    }

    /// <summary>
    /// Tests we can add call-state for a call before the notifications are processed
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task CallEstablishedWithPreviousCallState()
    {
        var callConnectedWithP2PAudioCount = 0;
        var callEstablishingCount = 0;
        var callEstablishedCount = 0;
        var toneList = new List<Tone>();
        var callbackInfo = new NotificationCallbackInfo<BaseActiveCallState>
        {
            CallEstablishing = (callState) =>
            {
                callEstablishingCount++;
                return Task.CompletedTask;
            },
            CallEstablished = (callState) =>
            {
                callEstablishedCount++;
                return Task.CompletedTask;
            }
        };
        var notificationsManager = new BotNotificationsHandler<BaseActiveCallState>(_callStateManager, _historyManager, callbackInfo, _logger);

        // Make sure no call state exists
        var callResourceUrl = NotificationsLibrary.P2PTest1CallEstablishingP2P.CommsNotifications[0]!.ResourceUrl!;
        await _callStateManager.RemoveCurrentCall(callResourceUrl);
        Assert.IsNull(await _callStateManager.GetByNotificationResourceUrl(callResourceUrl));

        // Insert initial state
        var callState = new BaseActiveCallState
        {
            StateEnum = CallState.UnknownFutureValue,
            ResourceUrl = callResourceUrl
        };
        await _callStateManager.AddCallStateOrUpdate(callState);

        Assert.IsNotNull(await _callStateManager.GetByNotificationResourceUrl(callResourceUrl));


        // Handle call establish for a call never seen before
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.P2PTest1CallEstablishingP2P, _unitTestBot);

        // Establish the call
        Assert.IsTrue(callEstablishingCount == 1, "CallEstablishing not fired");
        Assert.IsTrue(callEstablishedCount == 0);
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.P2PTest1CallEstablishedP2P, _unitTestBot);
        Assert.IsTrue(callEstablishedCount == 1);

        Assert.AreEqual(_callStateManager.GetByNotificationResourceUrl(callResourceUrl).Result!.StateEnum,
            CallState.Established);
        Assert.IsTrue(callConnectedWithP2PAudioCount == 0);
    }

    [TestMethod]
    public async Task AudoConnectMultiThreaded()
    {
        var callConnectedWithP2PAudioCount = 0;
        var callEstablishingCount = 0;
        var callEstablishedCount = 0;
        var callPlayPromptFinished = 0;
        var callTerminatedCount = 0;
        var toneList = new List<Tone>();
        var callStateManager = new SlowInMemoryCallStateManager<BaseActiveCallState>();
        var callbackInfo = new NotificationCallbackInfo<BaseActiveCallState>
        {
            CallEstablishing = (callState) =>
            {
                callEstablishingCount++;
                return Task.CompletedTask;
            },
            CallEstablished = (callState) =>
            {
                callEstablishedCount++;
                return Task.CompletedTask;
            },
            CallConnectedWithP2PAudio = (callState) =>
            {
                callConnectedWithP2PAudioCount++;
                return Task.CompletedTask;
            },
            PlayPromptFinished = (callState) =>
            {
                callPlayPromptFinished++;
                return Task.CompletedTask;
            },
            NewTonePressed = (callState, tone) =>
            {
                toneList.Add(tone);
                return Task.CompletedTask;
            },
            CallTerminated = (callState, result) =>
            {
                callTerminatedCount++;
                return Task.CompletedTask;
            }
        };
        var notificationsManager = new BotNotificationsHandler<BaseActiveCallState>(_callStateManager, _historyManager, callbackInfo, _logger);

        var callResourceUrl = NotificationsLibrary.P2PTest1CallEstablishingP2P.CommsNotifications[0]!.ResourceUrl!;

        // Handle call establish for a call never seen before
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.P2PTest1CallEstablishingP2P, _unitTestBot);

        // Parallel establish the call
        var tasks = new List<Task>
        {
            notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.P2PTest1CallEstablishedP2P, _unitTestBot),
            notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.P2PTest1CallEstablishedWithAudioP2P, _unitTestBot)
        };
        await Task.WhenAll(tasks);

        Assert.IsTrue(callConnectedWithP2PAudioCount == 1);

        // Pretend we've finished playing a prompt
        var callState = await _callStateManager.GetByNotificationResourceUrl(callResourceUrl);

        // Add a media prompt to the call state
        callState!.MediaPromptsPlaying.Add(new EquatableMediaPrompt { MediaInfo = new MediaInfo { ResourceId = NotificationsLibrary.P2PTest1PlayPromptFinish!.CommsNotifications[0]!.AssociatedPlayPromptOperation!.Id } });
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.P2PTest1PlayPromptFinish, _unitTestBot);
        Assert.IsTrue(callPlayPromptFinished == 1);

    }


    [TestMethod]
    public async Task BotNotificationsHandlerGroupFlowTests()
    {
        var callEstablishingCount = 0;
        var userJoinedCount = 0;
        var userLeftCount = 0;
        var callPlayPromptFinished = 0;
        var callTerminatedCount = 0;
        var toneList = new List<Tone>();
        var callStateManager = new SlowInMemoryCallStateManager<BaseActiveCallState>();
        var callbackInfo = new NotificationCallbackInfo<BaseActiveCallState>
        {
            CallEstablishing = (callState) =>
            {
                callEstablishingCount++;
                return Task.CompletedTask;
            },
            UsersJoinedGroupCall = (callState, usersJoined) =>
            {
                userJoinedCount += usersJoined.Count;
                return Task.CompletedTask;
            },
            UsersLeftGroupCall = (callState, usersLeft) =>
            {
                userLeftCount += usersLeft.Count;
                return Task.CompletedTask;
            },
            PlayPromptFinished = (callState) =>
            {
                callPlayPromptFinished++;
                return Task.CompletedTask;
            },
            NewTonePressed = (callState, tone) =>
            {
                toneList.Add(tone);
                return Task.CompletedTask;
            },
            CallTerminated = (callState, result) =>
            {
                callTerminatedCount++;
                return Task.CompletedTask;
            }
        };
        var notificationsManager = new BotNotificationsHandler<BaseActiveCallState>(_callStateManager, _historyManager, callbackInfo, _logger);

        var callResourceUrl = NotificationsLibrary.GroupCallEstablished.CommsNotifications[0]!.ResourceUrl!;

        // Handle call establish for a call never seen before
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.GroupCallEstablishing, _unitTestBot);
        Assert.IsTrue(callEstablishingCount == 1);

        // We should find the call in the call state manager
        Assert.AreEqual(_callStateManager.GetByNotificationResourceUrl(callResourceUrl).Result!.StateEnum,
            CallState.Establishing);

        // Establish the call
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.GroupCallEstablished, _unitTestBot);

        Assert.AreEqual(_callStateManager.GetByNotificationResourceUrl(callResourceUrl).Result!.StateEnum,
            CallState.Established);
        Assert.IsTrue(userJoinedCount == 0);

        // Add user. Should trigger the callback
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.GroupCallUserJoin, _unitTestBot);
        Assert.IsTrue(userJoinedCount == 2, "Was expecting a call with 2 users joining (bot and user)");


        // Terminate the call
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.GroupCallEnd, _unitTestBot);
        Assert.IsNull(await _callStateManager.GetByNotificationResourceUrl(callResourceUrl));
        Assert.IsTrue(callTerminatedCount == 1);

        Assert.IsTrue((await _callStateManager.GetActiveCalls()).Count == 0);
    }
}
