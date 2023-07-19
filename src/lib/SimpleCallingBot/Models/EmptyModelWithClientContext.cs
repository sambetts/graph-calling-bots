using System.Text.Json.Serialization;

namespace ServiceHostedMediaCallingBot.Engine.Models;

public class EmptyModelWithClientContext
{
    public EmptyModelWithClientContext()
    {
        ClientContext = Guid.NewGuid().ToString();
    }

    [JsonPropertyName("clientContext")]
    public string ClientContext { get; set; }
}
