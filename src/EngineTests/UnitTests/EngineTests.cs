using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using GraphCallingBots.StateManagement.Sql;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace GraphCallingBots.UnitTests;

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
    public async Task CosmosCallHistoryManager()
    {
        await HistoryTest(new CosmosCallHistoryManager<BaseActiveCallState, CallNotification>(new CosmosClient(_config.CosmosDb), _config,
            GetLogger<CosmosCallHistoryManager<BaseActiveCallState, CallNotification>>()));
    }

    [TestMethod]
    public async Task SqlCallHistoryManager()
    {
        var optionsBuilder = new DbContextOptionsBuilder<CallHistorySqlContext<BaseActiveCallState, CallNotification>>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ServiceHostedMediaCallingBotUnitTests;Trusted_Connection=True;MultipleActiveResultSets=true");

        var context = new CallHistorySqlContext<BaseActiveCallState, CallNotification>(optionsBuilder.Options);
        await HistoryTest(new SqlCallHistoryManager<BaseActiveCallState, CallNotification>(context,
            GetLogger<SqlCallHistoryManager<BaseActiveCallState, CallNotification>>()));
    }

    private async Task HistoryTest(ICallHistoryManager<BaseActiveCallState, CallNotification> historyManager)
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
        var notification1 = new CallNotification();
        notification1.SetInAdditionalData("rando", 1);
        await historyManager.AddToCallHistory(callStateRandomId, notification1);

        // Get call history. Should not be null
        var history = await historyManager.GetCallHistory(callStateRandomId);
        Assert.IsNotNull(history);
        Assert.IsNotNull(history.NotificationsHistory);
        Assert.IsTrue(history.NotificationsHistory.Count == 1);
        Assert.IsTrue(history.StateHistory.Count == 1);
        Assert.IsTrue(history.StateHistory[0].StateEnum == CallState.Establishing);

        // Update state and history
        callStateRandomId.StateEnum = CallState.Established;

        var notification2 = new CallNotification();
        notification2.SetInAdditionalData("rando", 2);
        await historyManager.AddToCallHistory(callStateRandomId, notification2);

        history = await historyManager.GetCallHistory(callStateRandomId);
        Assert.IsTrue(history!.NotificationsHistory.Count == 2);
        Assert.IsTrue(history.StateHistory.Count == 2);

        // Verify the history
        foreach (var item in history.NotificationsHistory)
        {
            var randoObj = item.Payload!;
            Assert.IsNotNull(randoObj?.AdditionalData["rando"]);
            Assert.IsTrue(Convert.ToInt32(randoObj?.AdditionalData["rando"] ?? "0") > 0);
            Assert.IsTrue(item.Timestamp > DateTime.MinValue);
        }
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
        await BotNotificationsHandlerTests.FailedCallTest(_logger, callStateManager, _historyManager,
            new UnitTestBot(new RemoteMediaCallingBotConfiguration(), callStateManager, _historyManager, _logger, GetLogger<BotCallRedirector>()));
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
        Assert.IsTrue((await callStateManager.GetActiveCalls()).Count == 0);


        // Insert a call
        var callState = new T { ResourceUrl = randoId };
        await callStateManager.AddCallStateOrUpdate(callState);

        // Get by notification resource url
        var callState2 = await callStateManager.GetByNotificationResourceUrl(randoId);
        Assert.IsNotNull(callState2);
        Assert.AreEqual(callState2, callState);
        Assert.IsTrue((await callStateManager.GetActiveCalls()).Count == 1);

        // Update state
        callState2.StateEnum = CallState.Terminating;
        await callStateManager.UpdateCurrentCallState(callState2);
        var callState3 = await callStateManager.GetByNotificationResourceUrl(randoId);
        Assert.AreEqual(callState3!.StateEnum, CallState.Terminating);
        Assert.IsTrue((await callStateManager.GetActiveCalls()).Count == 1);

        // Delete a call
        await callStateManager.RemoveCurrentCall(callState.ResourceUrl);
        var nullCallState = await callStateManager.GetByNotificationResourceUrl(randoId);
        Assert.IsNull(nullCallState);
        Assert.IsTrue((await callStateManager.GetActiveCalls()).Count == 0);
    }
}
