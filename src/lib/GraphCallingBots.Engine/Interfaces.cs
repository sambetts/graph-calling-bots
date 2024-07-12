using GraphCallingBots.Models;
using Microsoft.AspNetCore.Http;

namespace GraphCallingBots;

/// <summary>
/// Non generic class to handle notifications and call state management.
/// </summary>
public interface ICommsNotificationsPayloadHandler
{
    Task HandleNotificationsAndUpdateCallStateAsync(CommsNotificationsPayload notifications);
    Task<bool> ValidateNotificationRequestAsync(HttpRequest request);
}
