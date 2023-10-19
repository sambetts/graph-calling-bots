using Microsoft.Graph;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;
using System.Text.Json;

namespace ServiceHostedMediaCallingBot.UnitTests;

[TestClass]
public class EngineTests : BaseTests
{
    public EngineTests()
    {
    }

    [TestMethod]
    public async Task ConcurrentInMemoryCallHistoryManager()
    {
        await HistoryTest(_historyManager);
    }
    [TestMethod]
    public async Task AzTablesCallHistoryManager()
    {
        await HistoryTest(new AzTablesCallHistoryManager<BaseActiveCallState>(new Azure.Data.Tables.TableServiceClient("UseDevelopmentStorage=true"), 
            GetLogger<AzTablesCallHistoryManager<BaseActiveCallState>>()));
    }

    private async Task HistoryTest(ICallHistoryManager<BaseActiveCallState> historyManager)
    {
        if (!historyManager.Initialised)
        {
            await historyManager.Initialise();
        }

        var callStateRandomId = new BaseActiveCallState { ResourceUrl = $"/communications/calls/{Guid.NewGuid()}/", StateEnum = CallState.Establishing };

        // History should be null
        var historyNull = await historyManager.GetCallHistory(callStateRandomId);
        Assert.IsNull(historyNull);

        // Add a call to history
        await historyManager.AddToCallHistory(callStateRandomId, JsonDocument.Parse("{}"));

        // Get call history. Should not be null
        var history = await historyManager.GetCallHistory(callStateRandomId);
        Assert.IsNotNull(history);
        Assert.IsNotNull(history.NotificationsHistory);
        Assert.IsTrue(history.NotificationsHistory.Length == 1);
        Assert.IsTrue(history.StateHistory.Count == 1);
        Assert.IsTrue(history.StateHistory[0].StateEnum == CallState.Establishing);

        // Update state and history
        callStateRandomId.StateEnum = CallState.Established;
        await historyManager.AddToCallHistory(callStateRandomId, JsonDocument.Parse("{}"));

        history = await historyManager.GetCallHistory(callStateRandomId);
        Assert.IsTrue(history!.NotificationsHistory.Length == 2);
        Assert.IsTrue(history.StateHistory.Count == 2);
        Assert.IsTrue(history.StateHistory[1].StateEnum == CallState.Established);

        await historyManager.DeleteCallHistory(callStateRandomId);
        historyNull = await historyManager.GetCallHistory(callStateRandomId);
        Assert.IsNull(historyNull);
    }

    [TestMethod]
    public async Task ConcurrentInMemoryCallStateManager()
    {
        await TestCallStateManager(_callStateManager);
    }

    [TestMethod]
    public async Task AzTablesCallStateManagerTests()
    {
        var callStateManager = new AzTablesCallStateManager<BaseActiveCallState>(new Azure.Data.Tables.TableServiceClient("UseDevelopmentStorage=true"),
            GetLogger<AzTablesCallStateManager<BaseActiveCallState>>());

        await callStateManager.Initialise();
        await callStateManager.RemoveAll();
        await TestCallStateManager(callStateManager);

        // Test also a failed call
        await BotNotificationsHandlerTests.FailedCallTest(_logger, callStateManager, _historyManager);
    }
    async Task TestCallStateManager<T>(ICallStateManager<T> callStateManager) where T : BaseActiveCallState, new()
    {
        if (!callStateManager.Initialised)
        {
            await callStateManager.Initialise();
        }

        var randoId = $"/communications/calls/{Guid.NewGuid()}/";

        // Check that we have no calls
        var nonExistentState = await callStateManager.GetByNotificationResourceUrl("whatever");
        Assert.IsNull(nonExistentState);
        Assert.IsTrue(await callStateManager.GetCurrentCallCount() == 0);


        // Insert a call
        var callState = new T { ResourceUrl = randoId };
        await callStateManager.AddCallStateOrUpdate(callState);

        // Get by notification resource url
        var callState2 = await callStateManager.GetByNotificationResourceUrl(randoId);
        Assert.IsNotNull(callState2);
        Assert.AreEqual(callState2, callState);
        Assert.IsTrue(await callStateManager.GetCurrentCallCount() == 1);

        // Update state
        callState2.StateEnum = CallState.Terminating;
        await callStateManager.UpdateCurrentCallState(callState2);
        var callState3 = await callStateManager.GetByNotificationResourceUrl(randoId);
        Assert.AreEqual(callState3!.StateEnum, CallState.Terminating);
        Assert.IsTrue(await callStateManager.GetCurrentCallCount() == 1);

        // Delete a call
        await callStateManager.RemoveCurrentCall(callState.ResourceUrl);
        var nullCallState = await callStateManager.GetByNotificationResourceUrl(randoId);
        Assert.IsNull(nullCallState);
        Assert.IsTrue(await callStateManager.GetCurrentCallCount() == 0);
    }
}
