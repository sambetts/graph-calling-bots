using System.Text.Json.Serialization;

namespace SimpleCallingBotEngine.Models;

public class ModelWithClientContext
{
    public ModelWithClientContext()
    {
        ClientContext = Guid.NewGuid().ToString();
    }

    [JsonPropertyName("clientContext")]
    public string ClientContext { get; set; }
}
