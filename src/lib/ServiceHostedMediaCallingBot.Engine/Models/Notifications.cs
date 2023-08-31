using Microsoft.Graph;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ServiceHostedMediaCallingBot.Engine.Models;

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
    public JsonElement? NotificationResource { get; set; }

    public Call? AssociatedCall => GetTypedResourceObject<Call>("#microsoft.graph.call");
    public PlayPromptOperation? AssociatedPlayPromptOperation => GetTypedResourceObject<PlayPromptOperation>("#microsoft.graph.playPromptOperation");

    public List<Participant>? JoinedParticipants => GetTypedResourceArray<Participant>();

    public ResultInfo? ResultInfo => GetTypedResourceObject<ResultInfo>("#microsoft.graph.resultInfo");


    T? GetTypedResourceObject<T>(string odataType) where T : class
    {
        if (NotificationResource != null)
        {
            var s = NotificationResource.ToString();

            // Single object
            if (s != null && NotificationResource.Value.ValueKind == JsonValueKind.Object)
            {
                var obj = JsonNode.Parse(s);
                if (obj != null && obj["@odata.type"]?.GetValue<string>() == odataType)
                {
                    return JsonSerializer.Deserialize<T>(s);
                }
            }
        }
        return null;
    }

    List<T>? GetTypedResourceArray<T>() where T : class
    {
        if (NotificationResource != null)
        {
            var s = NotificationResource.ToString();

            // Single object
            if (s != null && NotificationResource.Value.ValueKind == JsonValueKind.Array)
            {
                var array = JsonNode.Parse(s);
                if (array != null)
                {
                    return JsonSerializer.Deserialize<List<T>>(s);
                }
            }
        }
        return null;
    }
}
