using Microsoft.Graph.Models;
using Microsoft.Kiota.Serialization.Json;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace GraphCallingBots.Models;

public class CommsNotificationsPayload
{
    [JsonPropertyName("value")]
    public List<CallNotification> CommsNotifications { get; set; } = new();

    [JsonExtensionData]
    [NotMapped]
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class CallNotification
{
    [JsonPropertyName("changeType")]
    public string? ChangeType { get; set; }

    [JsonPropertyName("resourceUrl")]
    public string ResourceUrl { get; set; } = string.Empty;

    [JsonPropertyName("resourceData")]
    public JsonElement? NotificationResource { get; set; }

    [JsonExtensionData]
    [NotMapped]
    public Dictionary<string, object> AdditionalData { get; set; } = new();

    [JsonIgnore]
    public Call? AssociatedCall => GetTypedResourceObject<Call>("#microsoft.graph.call");

    [JsonIgnore]
    public PlayPromptOperation? AssociatedPlayPromptOperation => GetTypedResourceObject<PlayPromptOperation>("#microsoft.graph.playPromptOperation");

    [JsonIgnore]
    public List<CallParticipant>? JoinedParticipants => GetTypedResourceArray<CallParticipant>();

    T? GetTypedResourceObject<T>(string odataType) where T : Entity
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
                    // Deduced from https://github.com/microsoftgraph/msgraph-sdk-dotnet/blob/f7f01eeb125d8ed0cb023852b587eb30a0bb39f8/tests/Microsoft.Graph.DotnetCore.Test/Models/ModelSerializationTests.cs#L36
                    var jsonParseNode = new JsonParseNode(JsonDocument.Parse(s).RootElement);
                    var r = jsonParseNode.GetObjectValue(Entity.CreateFromDiscriminatorValue);

                    return r as T;
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
