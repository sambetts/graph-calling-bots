using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using ServiceHostedMediaCallingBot.Engine;
using ServiceHostedMediaCallingBot.Engine.StateManagement;
using ServiceHostedMediaCallingBot.UnitTests.TestServices;

namespace ServiceHostedMediaCallingBot.UnitTests;

[TestClass]
public class BotNotificationsHandlerTests
{
    private ILogger _logger;
    public BotNotificationsHandlerTests()
    {
        _logger = LoggerFactory.Create(config =>
        {
            config.AddTraceSource(new System.Diagnostics.SourceSwitch("SourceSwitch"));
            config.AddConsole();
        }).CreateLogger("Unit tests");
    }

    /// <summary>
    /// A call is established, but failed to connect. The call is then deleted.
    /// </summary>
    [TestMethod]
    public async Task FailedP2PCallFlowTests()
    {
        var callStateManager = new SlowInMemoryCallStateManager<BaseActiveCallState>();
        await FailedCallTest(_logger, callStateManager);
    }

    public static async Task FailedCallTest(ILogger logger, ICallStateManager<BaseActiveCallState> callStateManager)
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
        var notificationsManager = new BotNotificationsHandler<BaseActiveCallState>(callStateManager, callbackInfo, logger);

        var callResourceUrl = NotificationsLibrary.FailedCallEstablishingP2P.CommsNotifications[0]!.ResourceUrl!;

        // Handle call establish for a call never seen before
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.FailedCallEstablishingP2P);
        Assert.IsTrue(callEstablishingCount == 1);

        var postEstablishingCallState = await callStateManager.GetByNotificationResourceUrl(callResourceUrl);
        Assert.IsNotNull(postEstablishingCallState);
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.FailedCallDeleted);

        var postCallDeletedState = await callStateManager.GetByNotificationResourceUrl(callResourceUrl);
        Assert.IsNull(postCallDeletedState);

        Assert.AreEqual(0, callConnectedCount);
        Assert.AreEqual(1, callTerminatedCount);
    }

    [TestMethod]
    public async Task BotNotificationsHandlerP2PFlowTests()
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
        var notificationsManager = new BotNotificationsHandler<BaseActiveCallState>(callStateManager, callbackInfo, _logger);

        var callResourceUrl = NotificationsLibrary.CallEstablishingP2P.CommsNotifications[0]!.ResourceUrl!;

        // Handle call establish for a call never seen before
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.CallEstablishingP2P);

        // We should find the call in the call state manager
        Assert.AreEqual(callStateManager.GetByNotificationResourceUrl(callResourceUrl).Result!.StateEnum, CallState.Establishing);

        // Establish the call
        Assert.IsTrue(callEstablishingCount == 1);
        Assert.IsTrue(callEstablishedCount == 0);
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.CallEstablishedP2P);
        Assert.IsTrue(callEstablishedCount == 1);

        Assert.AreEqual(callStateManager.GetByNotificationResourceUrl(callResourceUrl).Result!.StateEnum,
            CallState.Established);
        Assert.IsTrue(callConnectedWithP2PAudioCount == 0);

        // Connect audio. Should trigger the callback
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.CallEstablishedWithAudioP2P);
        Assert.IsTrue(callConnectedWithP2PAudioCount == 1);

        // Pretend we've finished playing a prompt
        var callState = await callStateManager.GetByNotificationResourceUrl(callResourceUrl);

        // Add a media prompt to the call state
        callState!.MediaPromptsPlaying.Add(new MediaPrompt { MediaInfo = new MediaInfo { ResourceId = NotificationsLibrary.PlayPromptFinish!.CommsNotifications[0]!.AssociatedPlayPromptOperation!.Id } });
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.PlayPromptFinish);
        Assert.IsTrue(callPlayPromptFinished == 1);

        // Make sure the media prompt was removed
        Assert.IsTrue(callStateManager.GetByNotificationResourceUrl(callResourceUrl).Result!.MediaPromptsPlaying.Count == 0);

        // Press buttons. Should trigger the callback
        Assert.IsTrue(toneList.Count == 0);
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.TonePress);
        Assert.IsTrue(toneList.Count == 1);

        // Terminate the call
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.HangUp);
        Assert.IsNull(await callStateManager.GetByNotificationResourceUrl(callResourceUrl));
        Assert.IsTrue(callConnectedWithP2PAudioCount == 1);
        Assert.IsTrue(callTerminatedCount == 1);

        Assert.IsTrue(await callStateManager.GetCount() == 0);
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
        var notificationsManager = new BotNotificationsHandler<BaseActiveCallState>(callStateManager, callbackInfo, _logger);

        var callResourceUrl = NotificationsLibrary.CallEstablishingP2P.CommsNotifications[0]!.ResourceUrl!;

        // Handle call establish for a call never seen before
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.CallEstablishingP2P);

        // Parallel establish the call
        var tasks = new List<Task>
        {
            notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.CallEstablishedP2P),
            notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.CallEstablishedWithAudioP2P)
        };
        await Task.WhenAll(tasks);

        Assert.IsTrue(callConnectedWithP2PAudioCount == 1);

        // Pretend we've finished playing a prompt
        var callState = await callStateManager.GetByNotificationResourceUrl(callResourceUrl);

        // Add a media prompt to the call state
        callState!.MediaPromptsPlaying.Add(new MediaPrompt { MediaInfo = new MediaInfo { ResourceId = NotificationsLibrary.PlayPromptFinish!.CommsNotifications[0]!.AssociatedPlayPromptOperation!.Id } });
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.PlayPromptFinish);
        Assert.IsTrue(callPlayPromptFinished == 1);

    }


    [TestMethod]
    public async Task BotNotificationsHandlerGroupFlowTests()
    {
        var callEstablishingCount = 0;
        var userJoinedCount = 0;
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
            UserJoinedGroupCall = (callState) =>
            {
                userJoinedCount++;
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
        var notificationsManager = new BotNotificationsHandler<BaseActiveCallState>(callStateManager, callbackInfo, _logger);

        var callResourceUrl = NotificationsLibrary.GroupCallEstablished.CommsNotifications[0]!.ResourceUrl!;

        // Handle call establish for a call never seen before
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.GroupCallEstablishing);
        Assert.IsTrue(callEstablishingCount == 1);

        // We should find the call in the call state manager
        Assert.AreEqual(callStateManager.GetByNotificationResourceUrl(callResourceUrl).Result!.StateEnum,
            CallState.Establishing);

        // Establish the call
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.GroupCallEstablished);

        Assert.AreEqual(callStateManager.GetByNotificationResourceUrl(callResourceUrl).Result!.StateEnum,
            CallState.Established);
        Assert.IsTrue(userJoinedCount == 0);

        // Add user. Should trigger the callback
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.GroupCallUserJoin);
        Assert.IsTrue(userJoinedCount == 1, "Was expected a connected call");


        // Terminate the call
        await notificationsManager.HandleNotificationsAndUpdateCallStateAsync(NotificationsLibrary.GroupCallEnd);
        Assert.IsNull(await callStateManager.GetByNotificationResourceUrl(callResourceUrl));
        Assert.IsTrue(callTerminatedCount == 1);

        Assert.IsTrue(await callStateManager.GetCount() == 0);
    }
}
