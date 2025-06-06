using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using GraphCallingBots.StateManagement.Cosmos;
using GraphCallingBots.StateManagement.Sql;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Text.Json;

namespace GraphCallingBots.UnitTests;

[TestClass]
public class CallStateTests : BaseTests
{
    public CallStateTests()
    {
    }

    [TestMethod]
    public async Task ConcurrentInMemoryCallStateManager()
    {
        await TestCallStateManager(_callStateManager);
    }

    [TestMethod]
    public async Task CosmosCallStateManagerTests()
    {
        var callStateManager = new CosmosCallStateManager<BaseActiveCallState>(new CosmosClient(_config.CosmosConnectionString), _config.ContainerNameCallState, _config.CosmosDatabaseName,
            GetLogger<CosmosCallStateManager<BaseActiveCallState>>());

        await callStateManager.Initialise();
        await callStateManager.RemoveAll();

        // Test partial updates

        var testCallId = "123" + DateTime.Now.Ticks;
        var testResourceUrl = BaseActiveCallState.GetResourceUrlFromCallId(testCallId);

        await callStateManager.AddCallStateOrUpdate(
            new BaseActiveCallState 
            { 
                BotClassNameFull = "Test", 
                ResourceUrl = testResourceUrl 
            }
            );
        var state = await callStateManager.GetStateByCallId(testCallId);    
        Assert.IsNotNull(state);
        Assert.AreEqual("Test", state!.BotClassNameFull);

        // Add to same state with different properties
        await callStateManager.AddCallStateOrUpdate(
            new BaseActiveCallState 
            { 
                BotClassNameFull = "Test2", 
                ResourceUrl = testResourceUrl, 
                StateEnum = CallState.Establishing 
            }
            );
        state = await callStateManager.GetStateByCallId(testCallId);
        Assert.IsNotNull(state);
        Assert.AreEqual("Test2", state!.BotClassNameFull);
        Assert.AreEqual(CallState.Establishing, state.StateEnum);

        await callStateManager.RemoveAll();
        await TestCallStateManager(callStateManager);

        // Test also a failed call
        await BotNotificationsHandlerTests.FailedCallTest(_logger, callStateManager, _historyManager);
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

        var randoId = Guid.NewGuid().ToString();

        // Check that we have no calls
        var nonExistentState = await callStateManager.GetStateByCallId("whatever");
        Assert.IsNull(nonExistentState);
        Assert.IsTrue((await callStateManager.GetActiveCalls()).Count == 0);


        // Insert a call
        var callState = new T { ResourceUrl = BaseActiveCallState.GetResourceUrlFromCallId(randoId) };
        await callStateManager.AddCallStateOrUpdate(callState);

        // Get by notification resource url
        var callState2 = await callStateManager.GetStateByCallId(randoId);
        Assert.IsNotNull(callState2);
        Assert.AreEqual(callState2, callState);
        Assert.IsTrue((await callStateManager.GetActiveCalls()).Count == 1);

        // Update state
        callState2.StateEnum = CallState.Terminating;
        await callStateManager.AddCallStateOrUpdate(callState2);
        var callState3 = await callStateManager.GetStateByCallId(randoId);
        Assert.AreEqual(callState3!.StateEnum, CallState.Terminating);
        Assert.IsTrue((await callStateManager.GetActiveCalls()).Count == 1);

        // Delete a call
        await callStateManager.RemoveCurrentCall(callState.ResourceUrl);
        var nullCallState = await callStateManager.GetStateByCallId(randoId);
        Assert.IsNull(nullCallState);
        Assert.IsTrue((await callStateManager.GetActiveCalls()).Count == 0);
    }
}
