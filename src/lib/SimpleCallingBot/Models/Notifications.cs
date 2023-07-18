using Microsoft.Graph;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SimpleCallingBotEngine.Models;


public class CommsNotificationsPayload
{

    [JsonPropertyName("value")]
    public List<CallNotification> CommsNotifications { get; set; } = new();
}

public class CallNotification
{
    [JsonPropertyName("changeType")]
    public string? ChangeType { get; set; }

    [JsonPropertyName("resourceUrl")]
    public string ResourceUrl { get; set; } = string.Empty;

    [JsonPropertyName("resourceData")]
    public JsonObject? NotificationResource { get; set; }

    public Call? AssociatedCall => GetTypedResource<Call>("#microsoft.graph.call");
    public PlayPromptOperation? AssociatedPlayPromptOperation => GetTypedResource<PlayPromptOperation>("#microsoft.graph.playPromptOperation");

    T? GetTypedResource<T>(string odataType) where T : class
    {
        if (NotificationResource != null && NotificationResource["@odata.type"]?.GetValue<string>() == odataType)
        {
            return JsonSerializer.Deserialize<T>(NotificationResource.ToString());
        }
        return null;
    }
}

public class GenericResource
{

    [JsonPropertyName("@odata.type")]
    public string? Type { get; set; }

}
