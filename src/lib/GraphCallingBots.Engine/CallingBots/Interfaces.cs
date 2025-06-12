using GraphCallingBots.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Graph.Models;

namespace GraphCallingBots.CallingBots;


public interface IPstnCallingBot : ICommsNotificationsPayloadHandler
{
    Task<Call?> StartPTSNCall(string phoneNumber, string mediaUrl);
}
