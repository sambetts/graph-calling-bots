using CallingTestBot.FunctionApp;
using CallingTestBot.FunctionApp.Engine;
using Microsoft.Graph;

namespace CallingTestBot.UnitTests;

[TestClass]
public class BotTestsManagerTests
{
    [TestMethod]
    public async Task SuccesfullCallFlowTest()
    {
        var m = GetBotTestsLogger();      // Todo: make config bound

        await m.Initialise();

        // Test that we can create a new call state
        var randomId = Guid.NewGuid().ToString();
        await m.LogNewCallEstablishing(randomId);

        // Call should not've connected yet
        var testCallStatePreConnect = await m.GetTestCallState(randomId);
        Assert.IsNotNull(testCallStatePreConnect);
        Assert.AreEqual(randomId, testCallStatePreConnect!.CallId);
        Assert.IsFalse(testCallStatePreConnect.CallConnected);

        // Call connected
        await m.LogCallConnectedSuccesfully(randomId);
        var testCallStatePostConnect = await m.GetTestCallState(randomId);
        Assert.IsNotNull(testCallStatePostConnect);
        Assert.AreEqual(randomId, testCallStatePostConnect!.CallId);
        Assert.IsTrue(testCallStatePostConnect.CallConnected);

        // Call finished
        await m.LogCallTerminated(randomId, new ResultInfo { Code = 200, Message = "OK" });
        var testCallStatePostTerminate = await m.GetTestCallState(randomId);

        Assert.IsNotNull(testCallStatePostTerminate);
        Assert.AreEqual(randomId, testCallStatePostTerminate!.CallId);
        Assert.IsTrue(testCallStatePostTerminate.CallConnected);
        Assert.AreEqual(200, testCallStatePostTerminate.CallTerminateCode);
        Assert.AreEqual("OK", testCallStatePostTerminate.CallTerminateMessage);
    }

    AzTablesBotTestsLogger GetBotTestsLogger()
    {
        return new AzTablesBotTestsLogger(new CallingTestBotConfig { Storage = "UseDevelopmentStorage=true" });      // Todo: make config bound
    }

    [TestMethod]
    public async Task FailedCallFlowTest()
    {
        var m = GetBotTestsLogger();      // Todo: make config bound

        await m.Initialise();

        // Test that we can create a new call state
        var randomId = Guid.NewGuid().ToString();
        await m.LogNewCallEstablishing(randomId);

        // Call should not've connected yet
        var testCallStatePreConnect = await m.GetTestCallState(randomId);
        Assert.IsNotNull(testCallStatePreConnect);
        Assert.AreEqual(randomId, testCallStatePreConnect!.CallId);
        Assert.IsFalse(testCallStatePreConnect.CallConnected);

        // Call finished
        await m.LogCallTerminated(randomId, new ResultInfo { Code = 400, Message = "Massive error" });
        var testCallStatePostTerminate = await m.GetTestCallState(randomId);

        Assert.IsNotNull(testCallStatePostTerminate);
        Assert.AreEqual(randomId, testCallStatePostTerminate!.CallId);
        Assert.IsFalse(testCallStatePostTerminate.CallConnected);
        Assert.AreEqual(400, testCallStatePostTerminate.CallTerminateCode);
        Assert.AreEqual("Massive error", testCallStatePostTerminate.CallTerminateMessage);
    }
}
