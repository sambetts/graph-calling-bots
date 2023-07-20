using Microsoft.AspNetCore.Mvc;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using System.Threading.Tasks;

namespace GroupCallingChatBot.Web.Controllers;

/// <summary>
/// Entry point for handling call-related web hook requests from the stateful client.
/// </summary>
public class PlatformCallController : ControllerBase
{
    private readonly IGraphCallingBot _callingBot;

    public PlatformCallController(IGraphCallingBot callingBot)
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
        var validRequest = await _callingBot.ValidateNotificationRequestAsync(Request);
        if (validRequest)
        {
            await _callingBot.HandleNotificationsAsync(notifications);
            return Accepted();
        }
        else
        {
            return BadRequest();
        }
    }
}
