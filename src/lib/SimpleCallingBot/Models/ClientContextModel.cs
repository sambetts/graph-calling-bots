using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SimpleCallingBotEngine.Models;

public class ClientContextModel
{
    public ClientContextModel()
    {
        ClientContext = Guid.NewGuid().ToString();
    }

    [JsonPropertyName("clientContext")]
    public string ClientContext { get; set; }
}
