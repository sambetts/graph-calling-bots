using Microsoft.AspNetCore.Http;
using Microsoft.Graph.Models;
using ServiceHostedMediaCallingBot.Engine.Models;

namespace ServiceHostedMediaCallingBot.Engine.CallingBots;

public interface IGraphCallingBot
{
    Task HandleNotificationsAndUpdateCallStateAsync(CommsNotificationsPayload notifications);
    Task<bool> ValidateNotificationRequestAsync(HttpRequest request);
}

public interface IPstnCallingBot : IGraphCallingBot
{
    Task<Call?> StartPTSNCall(string phoneNumber, string mediaUrl);
}
