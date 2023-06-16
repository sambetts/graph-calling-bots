using Microsoft.Graph;
using System.Text.Json.Serialization;

namespace SimpleCallingBotEngine.Models;


public class PlayPromptRequest : ClientContextModel
{
    [JsonPropertyName("prompts")]
    public List<MediaPrompt> Prompts { get; set; } = new(); 
}
