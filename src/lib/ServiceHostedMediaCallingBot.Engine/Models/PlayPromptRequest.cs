using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Text.Json.Serialization;

namespace ServiceHostedMediaCallingBot.Engine.Models;


public class PlayPromptRequest : EmptyModelWithClientContext
{
    [JsonPropertyName("prompts")]
    public IEnumerable<MediaPrompt>? Prompts { get; set; }
}
