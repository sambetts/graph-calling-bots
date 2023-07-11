using Bot.Bots;
using Microsoft.AspNetCore.Mvc;
using SimpleCallingBotEngine.Models;
using System.Threading.Tasks;

namespace Bot.Controllers;

/// <summary>
/// Entry point for handling call-related web hook requests from the stateful client.
/// </summary>
public class PlatformCallController : ControllerBase
{
    private readonly ExternalCallingBot _callingBot;

    public PlatformCallController(ExternalCallingBot callingBot)
    {
        _callingBot = callingBot;
    }

    /// <summary>
    /// Handle a callback for an existing call.
    /// </summary>
    [HttpPost]
    [Route(HttpRouteConstants.CallNotificationsRoute)]
    public async Task<IActionResult> OnIncomingRequestAsync([FromBody] CommsNotificationsPayload notifications)
    {
        var validRequest = await _callingBot.ValidateNotificationRequestAsync(this.Request);
        if (validRequest)
        {
            await _callingBot.BotNotificationsHandler.HandleNotificationsAsync(notifications);
            return Accepted();
        }
        else
        {
            return BadRequest();
        }
    }
}
