using GraphCallingBots.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Graph.Models;

namespace GraphCallingBots.CallingBots;

public interface IGraphCallingBot
{
    Task HandleNotificationsAndUpdateCallStateAsync(CommsNotificationsPayload notifications);
    Task<bool> ValidateNotificationRequestAsync(HttpRequest request);
}

public interface IPstnCallingBot : IGraphCallingBot
{
    Task<Call?> StartPTSNCall(string phoneNumber, string mediaUrl);
}
