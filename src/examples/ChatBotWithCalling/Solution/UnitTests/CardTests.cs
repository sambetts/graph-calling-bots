using AdaptiveCards;
using Azure.Identity;
using Bot.AdaptiveCards;
using Engine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using SimpleCallingBotEngine;
using SimpleCallingBotEngine.Models;
using UnitTests.FakeService;

namespace UnitTests;

[TestClass]
public class CardTests
{
    protected Config _config = null!;
    protected ILogger _tracer = null!;

    [TestInitialize]
    public void Init()
    {
        var builder = new ConfigurationBuilder()
            .AddUserSecrets<TeamsChatbotManagerTests>();

        var config = builder.Build();


        _tracer = LoggerFactory.Create(config =>
        {
            config.AddConsole();
            config.SetMinimumLevel(LogLevel.Debug);
        }).CreateLogger("Unit tests");

        config = builder.Build();
        _config = new Config(config);
    }

    [TestMethod]
    public void LoadMenuCard()
    {
        var c = new MenuCard(new MeetingRequest());
        var json = c.GetCardContent();
        Assert.IsNotNull(json);

        // Validate json
        AdaptiveCard.FromJson(json);
    }
}
