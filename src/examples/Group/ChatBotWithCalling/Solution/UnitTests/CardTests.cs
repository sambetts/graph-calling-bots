using AdaptiveCards;
using GroupCallingChatBot.Web.AdaptiveCards;
using GroupCalls.Common;
using Microsoft.Extensions.Logging;

namespace GroupCallingChatBot.UnitTests;

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
        var c = new MenuCard(new StartGroupCallData());
        var json = c.GetCardContent();
        Assert.IsNotNull(json);

        // Validate json
        AdaptiveCard.FromJson(json);
    }
}
