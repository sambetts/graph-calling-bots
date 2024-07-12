using Microsoft.Graph.Models;
using System.Text.Json.Serialization;

namespace GraphCallingBots.Models;


public class PlayPromptRequest : EmptyModelWithClientContext
{
    [JsonPropertyName("prompts")]
    public IEnumerable<MediaPrompt>? Prompts { get; set; }
}
