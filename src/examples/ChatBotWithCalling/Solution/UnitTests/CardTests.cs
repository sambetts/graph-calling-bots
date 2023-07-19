using AdaptiveCards;
using Bot.AdaptiveCards;
using Bot.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UnitTests;

[TestClass]
public class CardTests
{
    protected ILogger _tracer = null!;

    [TestInitialize]
    public void Init()
    {
        _tracer = LoggerFactory.Create(config =>
        {
            config.AddConsole();
            config.SetMinimumLevel(LogLevel.Debug);
        }).CreateLogger("Unit tests");

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
