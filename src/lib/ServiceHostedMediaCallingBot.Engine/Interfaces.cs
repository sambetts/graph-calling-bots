using Microsoft.AspNetCore.Http;
using ServiceHostedMediaCallingBot.Engine.Models;

namespace ServiceHostedMediaCallingBot.Engine;

/// <summary>
/// Non generic class to handle notifications and call state management.
/// </summary>
public interface ICommsNotificationsPayloadHandler
{
    Task HandleNotificationsAndUpdateCallStateAsync(CommsNotificationsPayload notifications);
    Task<bool> ValidateNotificationRequestAsync(HttpRequest request);
}
