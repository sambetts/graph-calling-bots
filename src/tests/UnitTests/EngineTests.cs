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
        callState2.State = CallState.Terminating;
        await callStateManager.Update(callState2);
        var callState3 = await callStateManager.GetByNotificationResourceUrl("/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9/operations/11cccef9-7eeb-4910-9189-977c0f0eae85");
        Assert.AreEqual(callState3!.State, CallState.Terminating);
        Assert.IsTrue(callStateManager.Count == 1);

        // Delete a call
        await callStateManager.Remove(callState.ResourceUrl);
        var nullCallState = await callStateManager.GetByNotificationResourceUrl("/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9/operations/11cccef9-7eeb-4910-9189-977c0f0eae85");
        Assert.IsNull(nullCallState);
        Assert.IsTrue(callStateManager.Count == 0);
    }

    [TestMethod]
    public async Task BotNotificationsHandlerNormalFlowTests()
    {
        var callConnectedCount = 0;
        var callTerminatedCount = 0;
        var toneList = new List<Tone>();
        var callStateManager = new ConcurrentInMemoryCallStateManager();
        var callbackInfo = new NotificationCallbackInfo 
        {
            CallConnectedWithAudio = (callState) => 
            {
                callConnectedCount++;
                return Task.CompletedTask;
            },
            NewTonePressed = (callState, tone) =>
            {
                toneList.Add(tone);
                Console.WriteLine($"DEBUG: Tone pressed: {tone}");   
                return Task.CompletedTask;
            },
            CallTerminated= (callState) =>
            {
                callTerminatedCount++;
                return Task.CompletedTask;
            }
        };
        var notificationsManager = new BotNotificationsHandler(callStateManager, callbackInfo, _logger);

        var callResourceUrl = NotificationsLibrary.CallEstablishing.CommsNotifications[0]!.ResourceUrl!;    

        // Handle call establish for a call never seen before
        await notificationsManager.HandleNotificationsAsync(NotificationsLibrary.CallEstablishing);

        // We should find the call in the call state manager
        Assert.AreEqual(callStateManager.GetByNotificationResourceUrl(callResourceUrl).Result!.State,
            CallState.Establishing);

        // Establish the call
        await notificationsManager.HandleNotificationsAsync(NotificationsLibrary.CallEstablished);

        Assert.AreEqual(callStateManager.GetByNotificationResourceUrl(callResourceUrl).Result!.State,
            CallState.Established);
        Assert.IsTrue(callConnectedCount == 0);

        // Connect audio. Should trigger the callback
        await notificationsManager.HandleNotificationsAsync(NotificationsLibrary.CallEstablishedWithAudio);
        Assert.IsTrue(callConnectedCount == 1);

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
}
