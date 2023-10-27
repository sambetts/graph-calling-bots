using Microsoft.AspNetCore.Mvc;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using System.Text.Json;

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
    [HttpPost]
    [Route(HttpRouteConstants.OnIncomingRequestRoute)]
    public async Task<IActionResult> OnIncomingRequestAsync([FromBody] JsonElement json)
    {
        var validRequest = await _callingBot.ValidateNotificationRequestAsync(Request);
        if (validRequest)
        {
            var rawText = json.GetRawText();
            var notifications = JsonSerializer.Deserialize<CommsNotificationsPayload>(rawText);
            if (notifications != null)
            {
                await _callingBot.HandleNotificationsAndUpdateCallStateAsync(notifications);

                return Accepted();
            }
        }
        return BadRequest();
    }
}
