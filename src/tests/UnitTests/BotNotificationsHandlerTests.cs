using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using ServiceHostedMediaCallingBot.Engine;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

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

    [TestMethod]
    public async Task BotNotificationsHandlerP2PFlowTests()
    {
        var callConnectedWithP2PAudioCount = 0;
        var callConnectedCount = 0;
        var callPlayPromptFinished = 0;
        var callTerminatedCount = 0;
        var toneList = new List<Tone>();
        var callStateManager = new ConcurrentInMemoryCallStateManager<BaseActiveCallState>();
        var callbackInfo = new NotificationCallbackInfo<BaseActiveCallState>
        {
            CallEstablished = (callState) =>
            {
                callConnectedCount++;
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
            CallTerminated = (callState) =>
            {
                callTerminatedCount++;
                return Task.CompletedTask;
            }
        };
        var notificationsManager = new BotNotificationsHandler<BaseActiveCallState>(callStateManager, callbackInfo, _logger);

        var callResourceUrl = NotificationsLibrary.CallEstablishingP2P.CommsNotifications[0]!.ResourceUrl!;

        // Handle call establish for a call never seen before
        await notificationsManager.HandleNotificationsAsync(NotificationsLibrary.CallEstablishingP2P);

        // We should find the call in the call state manager
        Assert.AreEqual(callStateManager.GetByNotificationResourceUrl(callResourceUrl).Result!.StateEnum, CallState.Establishing);

        // Establish the call
        Assert.IsTrue(callConnectedCount == 0);
        await notificationsManager.HandleNotificationsAsync(NotificationsLibrary.CallEstablishedP2P);
        Assert.IsTrue(callConnectedCount == 1);

        Assert.AreEqual(callStateManager.GetByNotificationResourceUrl(callResourceUrl).Result!.StateEnum,
            CallState.Established);
        Assert.IsTrue(callConnectedWithP2PAudioCount == 0);

        // Connect audio. Should trigger the callback
        await notificationsManager.HandleNotificationsAsync(NotificationsLibrary.CallEstablishedWithAudioP2P);
        Assert.IsTrue(callConnectedWithP2PAudioCount == 1);

        // Pretend we've finished playing a prompt
        var callState = await callStateManager.GetByNotificationResourceUrl(callResourceUrl);

        // Add a media prompt to the call state
        callState!.MediaPromptsPlaying.Add(new MediaPrompt { MediaInfo = new MediaInfo { ResourceId = NotificationsLibrary.PlayPromptFinish!.CommsNotifications[0]!.AssociatedPlayPromptOperation!.Id } });
        await notificationsManager.HandleNotificationsAsync(NotificationsLibrary.PlayPromptFinish);
        Assert.IsTrue(callPlayPromptFinished == 1);

        // Make sure the media prompt was removed
        Assert.IsTrue(callStateManager.GetByNotificationResourceUrl(callResourceUrl).Result!.MediaPromptsPlaying.Count == 0);

        // Press buttons. Should trigger the callback
        Assert.IsTrue(toneList.Count == 0);
        await notificationsManager.HandleNotificationsAsync(NotificationsLibrary.TonePress);
        Assert.IsTrue(toneList.Count == 1);

        // Terminate the call
        await notificationsManager.HandleNotificationsAsync(NotificationsLibrary.HangUp);
        Assert.IsNull(await callStateManager.GetByNotificationResourceUrl(callResourceUrl));
        Assert.IsTrue(callConnectedWithP2PAudioCount == 1);
        Assert.IsTrue(callTerminatedCount == 1);

        Assert.IsTrue(await callStateManager.GetCount() == 0);
    }


    [TestMethod]
    public async Task BotNotificationsHandlerGroupFlowTests()
    {
        var userJoinedCount = 0;
        var callPlayPromptFinished = 0;
        var callTerminatedCount = 0;
        var toneList = new List<Tone>();
        var callStateManager = new ConcurrentInMemoryCallStateManager<BaseActiveCallState>();
        var callbackInfo = new NotificationCallbackInfo<BaseActiveCallState>
        {
            UserJoined = (callState) =>
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
            CallTerminated = (callState) =>
            {
                callTerminatedCount++;
                return Task.CompletedTask;
            }
        };
        var notificationsManager = new BotNotificationsHandler<BaseActiveCallState>(callStateManager, callbackInfo, _logger);

        var callResourceUrl = NotificationsLibrary.GroupCallEstablished.CommsNotifications[0]!.ResourceUrl!;

        // Handle call establish for a call never seen before
        await notificationsManager.HandleNotificationsAsync(NotificationsLibrary.GroupCallEstablishing);

        // We should find the call in the call state manager
        Assert.AreEqual(callStateManager.GetByNotificationResourceUrl(callResourceUrl).Result!.StateEnum,
            CallState.Establishing);

        // Establish the call
        await notificationsManager.HandleNotificationsAsync(NotificationsLibrary.GroupCallEstablished);

        Assert.AreEqual(callStateManager.GetByNotificationResourceUrl(callResourceUrl).Result!.StateEnum,
            CallState.Established);
        Assert.IsTrue(userJoinedCount == 0);

        // Add user. Should trigger the callback
        await notificationsManager.HandleNotificationsAsync(NotificationsLibrary.GroupCallUserJoin);
        Assert.IsTrue(userJoinedCount == 1, "Was expected a connected call");


        // Terminate the call
        await notificationsManager.HandleNotificationsAsync(NotificationsLibrary.GroupCallEnd);
        Assert.IsNull(await callStateManager.GetByNotificationResourceUrl(callResourceUrl));
        Assert.IsTrue(callTerminatedCount == 1);

        Assert.IsTrue(await callStateManager.GetCount() == 0);
    }
}
