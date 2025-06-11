using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using GraphCallingBots.StateManagement.Cosmos;
using GraphCallingBots.StateManagement.Sql;
using GroupCalls.Common;
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
        var callStateManager = new CosmosCallStateManager<GroupCallInviteActiveCallState>(new CosmosClient(_config.CosmosConnectionString), _config,
            GetLogger<CosmosCallStateManager<GroupCallInviteActiveCallState>>());

        await callStateManager.Initialise();
        await callStateManager.RemoveAll();

        // Test partial updates
        var testCallId = "123" + DateTime.Now.Ticks;
        var testResourceUrl = BaseActiveCallState.GetResourceUrlFromCallId(testCallId);

        await callStateManager.AddCallStateOrUpdate(
            new GroupCallInviteActiveCallState
            { 
                BotClassNameFull = "Test", 
                GroupCallId = testCallId,
                ResourceUrl = testResourceUrl,
            }
        );

        var state = await callStateManager.GetStateByCallId(testCallId);    
        Assert.IsNotNull(state);
        Assert.AreEqual("Test", state!.BotClassNameFull);

        // Add to same state with different properties
        await callStateManager.AddCallStateOrUpdate(
            new GroupCallInviteActiveCallState
            { 
                GroupCallId = null,
                BotClassNameFull = "Test3", 
                ResourceUrl = testResourceUrl, 
                StateEnum = CallState.Establishing, // New state prop
                BotMediaPlaylist = new Dictionary<string, EquatableMediaPrompt>
                {
                    { "TestMedia", new EquatableMediaPrompt { MediaInfo = new MediaInfo { Uri = "https://example.com/media.mp3" } } }
                },
            }
        );
        state = await callStateManager.GetStateByCallId(testCallId);
        Assert.IsNotNull(state);
        Assert.AreEqual(testCallId, state!.GroupCallId);          // Null update should be ignored so should be previous update
        Assert.AreEqual("Test3", state!.BotClassNameFull);          // Updated bot class name
        Assert.AreEqual(CallState.Establishing, state.StateEnum);   // New state prop
        Assert.IsNotNull(state.BotMediaPlaylist); // New media playlist prop
        Assert.IsTrue(state.BotMediaPlaylist.Count > 0); // Ensure media playlist is not empty
        await callStateManager.RemoveAll();

        // Standard tests
        await TestCallStateManager(callStateManager);

        // Test also a failed call
        await BotNotificationsHandlerTests.FailedCallTest(_logger, callStateManager, new ConcurrentInMemoryCallHistoryManager<GroupCallInviteActiveCallState>());
    }

    [TestMethod]
    [Obsolete]
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
        await callStateManager.RemoveCurrentCall(randoId);
        var nullCallState = await callStateManager.GetStateByCallId(randoId);
        Assert.IsNull(nullCallState);
        Assert.IsTrue((await callStateManager.GetActiveCalls()).Count == 0);
    }
}
