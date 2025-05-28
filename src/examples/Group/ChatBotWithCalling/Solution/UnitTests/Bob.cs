using GraphCallingBots.CallingBots;
using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using GroupCalls.Common;
using Microsoft.Extensions.Logging;


namespace GroupCallingChatBot.UnitTests;

[TestClass]
public class Bob : AbstractTest
{
    protected ILogger _logger;

    public Bob()
    {
        _logger = LoggerFactory.Create(config =>
        {
            config.AddTraceSource(new System.Diagnostics.SourceSwitch("SourceSwitch"));
            config.AddConsole();
        }).CreateLogger("Unit tests");
    }


    [TestMethod]
    public void SerialiseBot()
    {
        var config = _config.ToRemoteMediaCallingBotConfiguration(string.Empty);

        var callStateManager = new ConcurrentInMemoryCallStateManager<BaseActiveCallState>();
        var callHistoryManager = new ConcurrentInMemoryCallHistoryManager<BaseActiveCallState>();
        var callRedirector = new GraphCallingBots.BotCallRedirector<BaseGraphCallingBot<BaseActiveCallState>, BaseActiveCallState>(config, callStateManager, callHistoryManager, GetLogger<GroupCallBot>());   
        var bot = new GroupCallBot(config,
            callRedirector,
            callStateManager,
            callHistoryManager,
            LoggerFactory.Create(config => { config.AddTraceSource(new System.Diagnostics.SourceSwitch("SourceSwitch")); config.AddConsole(); }).CreateLogger<GroupCallBot>());

        // Deserialize the JSON string back to a GroupCallBot object
        var deserializedBot = BaseBot<BaseActiveCallState>.HydrateBot<GroupCallBot, BaseActiveCallState>(config, callStateManager, callHistoryManager, base.GetLogger<GroupCallBot>());

        // Check if the deserialized object is not null
        Assert.IsNotNull(deserializedBot, "Deserialized bot should not be null.");
    }
}
