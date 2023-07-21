using Microsoft.AspNetCore.Http;
using Microsoft.Graph;
using ServiceHostedMediaCallingBot.Engine.Models;

namespace ServiceHostedMediaCallingBot.Engine.CallingBots;

public interface IGraphCallingBot 
{
    Task HandleNotificationsAsync(CommsNotificationsPayload notifications);
    Task<bool> ValidateNotificationRequestAsync(HttpRequest request);
}

public interface IPstnCallingBot : IGraphCallingBot
{
    Task<Call> StartPTSNCall(string phoneNumber);
}