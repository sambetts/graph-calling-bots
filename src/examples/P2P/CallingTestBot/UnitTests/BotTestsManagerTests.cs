using CallingTestBot.FunctionApp.Engine;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace CallingTestBot.UnitTests;

[TestClass]
public class BotTestsManagerTests
{
    protected ILogger _logger;

    public BotTestsManagerTests()
    {
        _logger = LoggerFactory.Create(config =>
        {
            config.AddTraceSource(new System.Diagnostics.SourceSwitch("SourceSwitch"));
            config.AddConsole();
        }).CreateLogger("Unit tests");
    }

    [TestMethod]
    public async Task SuccesfullCallFlowTest()
    {
        var m = GetBotTestsLogger();      // Todo: make config bound

        await m.Initialise();

        // Test that we can create a new call state
        var randomCallId = Guid.NewGuid().ToString();
        var randomPhoneNumber = DateTime.Now.Ticks.ToString();
        await m.LogNewCallEstablishing(randomCallId, randomPhoneNumber);

        // Call should not've connected yet
        var testCallStatePreConnect = await m.GetTestCallState(randomCallId);
        Assert.IsNotNull(testCallStatePreConnect);
        Assert.AreEqual(randomCallId, testCallStatePreConnect!.CallId);
        Assert.IsFalse(testCallStatePreConnect.CallConnectedOk);
        Assert.AreEqual(randomPhoneNumber, testCallStatePreConnect!.NumberCalled);

        // Call connected
        await m.LogCallConnectedSuccesfully(randomCallId);
        var testCallStatePostConnect = await m.GetTestCallState(randomCallId);
        Assert.IsNotNull(testCallStatePostConnect);
        Assert.AreEqual(randomCallId, testCallStatePostConnect!.CallId);
        Assert.AreEqual(randomPhoneNumber, testCallStatePostConnect!.NumberCalled);
        Assert.IsTrue(testCallStatePostConnect.CallConnectedOk);

        // Call finished
        await m.LogCallTerminated(randomCallId, new ResultInfo { Code = 200, Message = "OK" });
        var testCallStatePostTerminate = await m.GetTestCallState(randomCallId);

        Assert.IsNotNull(testCallStatePostTerminate);
        Assert.AreEqual(randomCallId, testCallStatePostTerminate!.CallId);
        Assert.IsTrue(testCallStatePostTerminate.CallConnectedOk);
        Assert.AreEqual(200, testCallStatePostTerminate.CallTerminateCode);
        Assert.AreEqual("OK", testCallStatePostTerminate.CallTerminateMessage);
        Assert.AreEqual(randomPhoneNumber, testCallStatePostTerminate!.NumberCalled);

    }

    AzTablesBotTestsLogger GetBotTestsLogger()
    {
        return new AzTablesBotTestsLogger(new Azure.Data.Tables.TableServiceClient("UseDevelopmentStorage=true"), LoggerFactory.Create(config => config.AddConsole()).CreateLogger<AzTablesBotTestsLogger>());
    }

    [TestMethod]
    public async Task FailedCallFlowTest()
    {
        var m = GetBotTestsLogger();      // Todo: make config bound

        await m.Initialise();

        // Test that we can create a new call state
        var randomId = Guid.NewGuid().ToString();
        await m.LogNewCallEstablishing(randomId, "123");

        // Call should not've connected yet
        var testCallStatePreConnect = await m.GetTestCallState(randomId);
        Assert.IsNotNull(testCallStatePreConnect);
        Assert.AreEqual(randomId, testCallStatePreConnect!.CallId);
        Assert.IsFalse(testCallStatePreConnect.CallConnectedOk);

        // Call finished
        await m.LogCallTerminated(randomId, new ResultInfo { Code = 400, Message = "Massive error" });
        var testCallStatePostTerminate = await m.GetTestCallState(randomId);

        Assert.IsNotNull(testCallStatePostTerminate);
        Assert.AreEqual(randomId, testCallStatePostTerminate!.CallId);
        Assert.IsFalse(testCallStatePostTerminate.CallConnectedOk);
        Assert.AreEqual(400, testCallStatePostTerminate.CallTerminateCode);
        Assert.AreEqual("Massive error", testCallStatePostTerminate.CallTerminateMessage);
    }
}
