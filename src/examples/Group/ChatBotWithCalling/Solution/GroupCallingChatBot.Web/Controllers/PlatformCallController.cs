using GroupCalls.Common;
using Microsoft.AspNetCore.Mvc;
using ServiceHostedMediaCallingBot.Engine.Models;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GroupCallingChatBot.Web.Controllers;

/// <summary>
/// Entry point for handling call-related web hook requests from the stateful client.
/// </summary>
public class PlatformCallController : ControllerBase
{
    private readonly GroupCallStartBot _callingBot;

    public PlatformCallController(GroupCallStartBot callingBot)
    {
        _callingBot = callingBot;
    }

    /// <summary>
    /// Handle a callback for an existing call.
    /// </summary>
    [HttpPost]
    [Route(HttpRouteConstants.CallNotificationsRoute)]
    public async Task<IActionResult> OnIncomingRequestAsync([FromBody] JsonElement json)
    {
        var validRequest = await _callingBot.ValidateNotificationRequestAsync(Request);
        if (validRequest)
        {
            var rawText = json.GetRawText();
            var notifications = JsonSerializer.Deserialize<CommsNotificationsPayload>(rawText);
            if (notifications != null)
            {
                validRequest = await _callingBot.HandleNotificationsAndUpdateCallStateAsync(notifications, rawText);
                if (validRequest)
                {
                    return Accepted();
                }
            }
        }
        return BadRequest();
    }
}
