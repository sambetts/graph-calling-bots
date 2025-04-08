using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GraphCallingBots.UnitTests;

[TestClass]
public class BotNotificationsHistoryTests : BaseTests
{
    public BotNotificationsHistoryTests()
    {
        _logger = LoggerFactory.Create(config =>
        {
            config.AddTraceSource(new System.Diagnostics.SourceSwitch("SourceSwitch"));
            config.AddConsole();
        }).CreateLogger("Unit tests");
    }

    [TestMethod]
    public void CallNotificationSerialisation()
    {
        // Sanity test
        var json1 = @"{ ""Age"": 25, ""Name"": ""Alice"" }";
        var json2 = @"{ ""Name"": ""Alice"", ""Age"": 25 }";

        using var docTest1 = JsonDocument.Parse(json1);
        using var docTest2 = JsonDocument.Parse(json2);

        // Compare json objects
        var notificationJsonOriginal = Properties.Resources.P2PTest1Event5PlayPromptFinish;
        var notificationJsonFromObject = JsonSerializer.Serialize(NotificationsLibrary.P2PTest1PlayPromptFinish);

        using var docNotificationJsonOriginal = JsonDocument.Parse(notificationJsonOriginal);
        using var docNotificationJsonFromObject = JsonDocument.Parse(notificationJsonFromObject);

        Assert.IsTrue(JsonElementEquals(docTest1.RootElement, docTest2.RootElement), "Test serialisation failed");

        Assert.IsTrue(JsonElementEquals(docNotificationJsonOriginal.RootElement, docNotificationJsonFromObject.RootElement), "Serialisation failed");
    }

    static bool JsonElementEquals(JsonElement elem1, JsonElement elem2)
    {
        if (elem1.ValueKind != elem2.ValueKind) return false;

        switch (elem1.ValueKind)
        {
            case JsonValueKind.Object:
                {
                    var properties1 = elem1.EnumerateObject();
                    var properties2 = elem2.EnumerateObject();
                    if (properties1.Count() != properties2.Count()) return false;

                    foreach (var property in properties1)
                    {
                        if (!elem2.TryGetProperty(property.Name, out JsonElement elem2Value))
                            return false;

                        if (!JsonElementEquals(property.Value, elem2Value))
                            return false;
                    }
                    return true;
                }
            case JsonValueKind.Array:
                {
                    var array1 = elem1.EnumerateArray();
                    var array2 = elem2.EnumerateArray();
                    if (array1.Count() != array2.Count()) return false;

                    var enumerator1 = array1.GetEnumerator();
                    var enumerator2 = array2.GetEnumerator();

                    while (enumerator1.MoveNext() && enumerator2.MoveNext())
                    {
                        if (!JsonElementEquals(enumerator1.Current, enumerator2.Current))
                            return false;
                    }
                    return true;
                }
            default:
                return elem1.ToString() == elem2.ToString();
        }
    }
}
