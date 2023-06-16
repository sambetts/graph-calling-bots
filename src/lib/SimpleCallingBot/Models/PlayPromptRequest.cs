using Microsoft.Graph;
using System.Text.Json.Serialization;

namespace SimpleCallingBotEngine.Models;


public class PlayPromptRequest : ModelWithClientContext
{
    [JsonPropertyName("prompts")]
    public IEnumerable<MediaPrompt>? Prompts { get; set; }
}
