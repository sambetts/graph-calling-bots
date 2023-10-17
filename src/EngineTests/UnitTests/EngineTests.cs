using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;
using ServiceHostedMediaCallingBot.UnitTests.TestServices;
using System.Text.Json;

namespace ServiceHostedMediaCallingBot.UnitTests;

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
    public async Task ConcurrentInMemoryCallStateManager()
    {
        var callStateManager = new SlowInMemoryCallStateManager<BaseActiveCallState>();
        await Test(callStateManager);
    }

    [TestMethod]
    public async Task AzTablesCallStateManagerTests()
    {
        var callStateManager = new AzTablesCallStateManager<BaseActiveCallState>("UseDevelopmentStorage=true");

        await callStateManager.Initialise();
        await callStateManager.RemoveAll();
        await Test(callStateManager);

        // Test also a failed call
        await BotNotificationsHandlerTests.FailedCallTest(_logger, callStateManager);
    }
    async Task Test<T>(ICallStateManager<T> callStateManager) where T : BaseActiveCallState, new()
    {
        if (!callStateManager.Initialised)
        {
            await callStateManager.Initialise();
        }

        // Check that we have no calls
        var nonExistentState = await callStateManager.GetByNotificationResourceUrl("whatever");
        Assert.IsNull(nonExistentState);
        Assert.IsTrue(await callStateManager.GetCurrentCallCount() == 0);

        // History should be null
        var historyNull = await callStateManager.GetCallHistory(new T { ResourceUrl = "/communications/calls/randomID/" });
        Assert.IsNull(historyNull);

        // Insert a call
        var callState = new T { ResourceUrl = "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9/operations/11cccef9-7eeb-4910-9189-977c0f0eae85" };
        await callStateManager.AddCallStateOrUpdate((T)callState);

        // History check
        await callStateManager.AddToCallHistory(callState, JsonDocument.Parse("{}"));
        var history = await callStateManager.GetCallHistory(callState);
        Assert.IsNotNull(history);
        Assert.IsNotNull(history.NotificationsHistory);
        Assert.IsTrue(history.NotificationsHistory.Count == 1);

        // Get by notification resource url
        var callState2 = await callStateManager.GetByNotificationResourceUrl("/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9/operations/11cccef9-7eeb-4910-9189-977c0f0eae85");
        Assert.IsNotNull(callState2);
        Assert.AreEqual(callState2, callState);
        Assert.IsTrue(await callStateManager.GetCurrentCallCount() == 1);

        // Update
        callState2.StateEnum = CallState.Terminating;
        await callStateManager.UpdateCurrentCallState(callState2);
        var callState3 = await callStateManager.GetByNotificationResourceUrl("/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9/operations/11cccef9-7eeb-4910-9189-977c0f0eae85");
        Assert.AreEqual(callState3!.StateEnum, CallState.Terminating);
        Assert.IsTrue(await callStateManager.GetCurrentCallCount() == 1);

        // Delete a call
        await callStateManager.RemoveCurrentCall(callState.ResourceUrl);
        var nullCallState = await callStateManager.GetByNotificationResourceUrl("/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9/operations/11cccef9-7eeb-4910-9189-977c0f0eae85");
        Assert.IsNull(nullCallState);
        Assert.IsTrue(await callStateManager.GetCurrentCallCount() == 0);
    }
}
