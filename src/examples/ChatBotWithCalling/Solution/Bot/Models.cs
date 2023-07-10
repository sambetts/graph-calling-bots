using Microsoft.AspNetCore.Cors.Infrastructure;
using Newtonsoft.Json;
namespace Bot;

public class ActionResponse
{
    [JsonProperty(CardConstants.CardActionPropName)]
    public string Action { get; set; } = null!;
}
