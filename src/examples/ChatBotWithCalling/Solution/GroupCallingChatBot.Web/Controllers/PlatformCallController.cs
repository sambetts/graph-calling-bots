using GroupCallingChatBot.Web.Bots;
using Microsoft.AspNetCore.Mvc;
using ServiceHostedMediaCallingBot.Engine.Models;
using System.Threading.Tasks;

namespace GroupCallingChatBot.Web.Controllers;

/// <summary>
/// Entry point for handling call-related web hook requests from the stateful client.
/// </summary>
public class PlatformCallController : ControllerBase
{
    private readonly IGroupCallingBot _callingBot;

    public PlatformCallController(IGroupCallingBot callingBot)
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
            await _callingBot.HandleNotificationsAndUpdateCallStateAsync(notifications);
            return Accepted();
        }
        else
        {
            return BadRequest();
        }
    }
}
