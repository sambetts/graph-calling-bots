using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Communications.Common;
using SimpleCallingBotEngine;
using SimpleCallingBotEngineEngine;

namespace UnitTests;

[TestClass]
public class EngineTests
{
    private ILogger _logger;
    public EngineTests()
    {
        _logger = LoggerFactory.Create(config =>
        {
            config.AddTraceSource(new System.Diagnostics.SourceSwitch("SourceSwitch"));
            config.AddConsole();
        }).CreateLogger("Unit tests");
    }

    [TestMethod]
    public void ModelTests()
    {
        Assert.IsNull(new ActiveCallState().CallId);
        Assert.IsNull(new ActiveCallState { ResourceUrl = "/communications/calls/" }.CallId);
        Assert.AreEqual("6f1f5c00-8c1b-47f1-be9d-660c501041a9", new ActiveCallState { ResourceUrl = "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9" }.CallId);
        Assert.AreEqual("6f1f5c00-8c1b-47f1-be9d-660c501041a9", new ActiveCallState { ResourceUrl = "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9/" }.CallId);
        Assert.AreEqual("6f1f5c00-8c1b-47f1-be9d-660c501041a9", new ActiveCallState { ResourceUrl = "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9/operations/11cccef9-7eeb-4910-9189-977c0f0eae85" }.CallId);
    }


    [TestMethod]
    public async Task ConcurrentInMemoryCallStateManager()
    {
        var callStateManager = new ConcurrentInMemoryCallStateManager();
        var nonExistent = await callStateManager.GetByNotificationResourceUrl("whatever");
        Assert.IsNull(nonExistent);
        Assert.IsTrue(callStateManager.Count == 0);

        // Insert a call
        var callState = new ActiveCallState { ResourceUrl = "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9/operations/11cccef9-7eeb-4910-9189-977c0f0eae85" };
        await callStateManager.AddCallState(callState);

        // Get by notification resource url
        var callState2 = await callStateManager.GetByNotificationResourceUrl("/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9/operations/11cccef9-7eeb-4910-9189-977c0f0eae85");
        Assert.IsNotNull(callState2);
        Assert.AreEqual(callState2, callState);
        Assert.IsTrue(callStateManager.Count == 1);

        // Update
        callState2.StateEnum = CallState.Terminating;
        await callStateManager.Update(callState2);
        var callState3 = await callStateManager.GetByNotificationResourceUrl("/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9/operations/11cccef9-7eeb-4910-9189-977c0f0eae85");
        Assert.AreEqual(callState3!.StateEnum, CallState.Terminating);
        Assert.IsTrue(callStateManager.Count == 1);

        // Delete a call
        await callStateManager.Remove(callState.ResourceUrl);
        var nullCallState = await callStateManager.GetByNotificationResourceUrl("/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9/operations/11cccef9-7eeb-4910-9189-977c0f0eae85");
        Assert.IsNull(nullCallState);
        Assert.IsTrue(callStateManager.Count == 0);
    }

    [TestMethod]
    public async Task BotNotificationsHandlerP2PFlowTests()
    {
        var callConnectedCount = 0;
        var callPlayPromptFinished = 0;
        var callTerminatedCount = 0;
        var toneList = new List<Tone>();
        var callStateManager = new ConcurrentInMemoryCallStateManager();
        var callbackInfo = new NotificationCallbackInfo 
        {
            CallConnectedWithP2PAudio = (callState) => 
            {
                callConnectedCount++;
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
        var notificationsManager = new BotNotificationsHandler(callStateManager, callbackInfo, _logger);

        var callResourceUrl = NotificationsLibrary.CallEstablishingP2P.CommsNotifications[0]!.ResourceUrl!;    

        // Handle call establish for a call never seen before
        await notificationsManager.HandleNotificationsAsync(NotificationsLibrary.CallEstablishingP2P);

        // We should find the call in the call state manager
        Assert.AreEqual(callStateManager.GetByNotificationResourceUrl(callResourceUrl).Result!.StateEnum,
            CallState.Establishing);

        // Establish the call
        await notificationsManager.HandleNotificationsAsync(NotificationsLibrary.CallEstablishedP2P);

        Assert.AreEqual(callStateManager.GetByNotificationResourceUrl(callResourceUrl).Result!.StateEnum,
            CallState.Established);
        Assert.IsTrue(callConnectedCount == 0);

        // Connect audio. Should trigger the callback
        await notificationsManager.HandleNotificationsAsync(NotificationsLibrary.CallEstablishedWithAudioP2P);
        Assert.IsTrue(callConnectedCount == 1);

        // Pretend we've finished playing a prompt
        var callState = await callStateManager.GetByNotificationResourceUrl(callResourceUrl);

        // Add a media prompt to the call state
        callState!.MediaPromptsPlaying.Add( new MediaPrompt { MediaInfo = new MediaInfo { ResourceId = NotificationsLibrary.PlayPromptFinish!.CommsNotifications[0]!.AssociatedPlayPromptOperation!.Id } });
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
        Assert.IsTrue(callConnectedCount == 1);
        Assert.IsTrue(callTerminatedCount == 1);

        Assert.IsTrue(callStateManager.Count == 0);
    }


    [TestMethod]
    public async Task BotNotificationsHandlerGroupFlowTests()
    {
        var userJoinedCount = 0;
        var callPlayPromptFinished = 0;
        var callTerminatedCount = 0;
        var toneList = new List<Tone>();
        var callStateManager = new ConcurrentInMemoryCallStateManager();
        var callbackInfo = new NotificationCallbackInfo
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
        var notificationsManager = new BotNotificationsHandler(callStateManager, callbackInfo, _logger);

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

        Assert.IsTrue(callStateManager.Count == 0);
    }
}
