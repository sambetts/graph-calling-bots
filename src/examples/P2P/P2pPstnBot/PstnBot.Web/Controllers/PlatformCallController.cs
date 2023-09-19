using Microsoft.AspNetCore.Mvc;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;

namespace PstnBot.Web.Controllers;

/// <summary>
/// Entry point for handling call-related web hook requests from the stateful client.
/// </summary>
public class PlatformCallController : ControllerBase
{
    private readonly IPstnCallingBot _callingBot;

    public PlatformCallController(IPstnCallingBot callingBot)
    {
        _callingBot = callingBot;
    }

    /// <summary>
    /// Handle a callback for an existing call.
    /// </summary>
    [HttpPost]
    [Route(HttpRouteConstants.OnIncomingRequestRoute)]
    public async Task<IActionResult> OnIncomingRequestAsync([FromBody] CommsNotificationsPayload notifications)
    {
        var validRequest = await _callingBot.ValidateNotificationRequestAsync(Request);
        if (validRequest)
        {
            await _callingBot.HandleNotificationsAndUpdateCallStateAsync(notifications);
            return Accepted();
        }
        else
        {
            return BadRequest();
        }
    }
}
