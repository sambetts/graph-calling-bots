using System.Text.Json.Serialization;

namespace SimpleCallingBotEngine.Models;

public class EmptyModelWithClientContext
{
    public EmptyModelWithClientContext()
    {
        ClientContext = Guid.NewGuid().ToString();
    }

    [JsonPropertyName("clientContext")]
    public string ClientContext { get; set; }
}
