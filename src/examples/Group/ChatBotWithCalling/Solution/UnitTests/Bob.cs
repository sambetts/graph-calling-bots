using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using GroupCalls.Common;
using Microsoft.Extensions.Logging;

namespace CallingTestBot.UnitTests;

[TestClass]
public class Bob
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
        var bot = new GroupCallBot(new GraphCallingBots.Models.RemoteMediaCallingBotConfiguration(),
            new ConcurrentInMemoryCallStateManager<BaseActiveCallState>(),
            new ConcurrentInMemoryCallHistoryManager<BaseActiveCallState>(),
            LoggerFactory.Create(config => { config.AddTraceSource(new System.Diagnostics.SourceSwitch("SourceSwitch")); config.AddConsole(); }).CreateLogger<GroupCallBot>());

        // Serialize the bot to a JSON string
        var json = System.Text.Json.JsonSerializer.Serialize(bot, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            IncludeFields = true
        });
        // Deserialize the JSON string back to a GroupCallBot object
        var deserializedBot = System.Text.Json.JsonSerializer.Deserialize<GroupCallBot>(json, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            IncludeFields = true
        });

        // Check if the deserialized object is not null
        Assert.IsNotNull(deserializedBot, "Deserialized bot should not be null.");
    }
}
