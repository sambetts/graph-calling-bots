using GraphCallingBots;
using GraphCallingBots.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace GroupCallingChatBot.Web.Controllers;

/// <summary>
/// Entry point for handling call-related web hook requests from the stateful client.
/// </summary>
public class PlatformCallController(BotCallRedirector botCallRedirector, ILogger<PlatformCallController> logger) : ControllerBase
{

    /// <summary>
    /// Handle a callback for an existing call.
    /// </summary>
    [HttpPost]
    [Route(HttpRouteConstants.CallNotificationsRoute)]
    public async Task<IActionResult> OnIncomingRequestAsync([FromBody] JsonElement json)
    {
        var rawText = json.GetRawText();
        var notificationsPayload = JsonSerializer.Deserialize<CommsNotificationsPayload>(rawText);

        if (notificationsPayload != null)
        {
            foreach (var notification in notificationsPayload.CommsNotifications)
            {
                var callId = BaseActiveCallState.GetCallId(notification.ResourceUrl);
                if (callId != null)
                {

                    var bot = botCallRedirector.GetBotByCallId(callId);
                    if (bot != null)        // Logging for negative handled in GetBotByCallId
                    {
                        var validRequest = await bot.ValidateNotificationRequestAsync(Request);

                        if (validRequest)
                        {
                            try
                            {
                                await bot.HandleNotificationsAndUpdateCallStateAsync(notificationsPayload);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Error handling notifications");
                                throw;
                            }

                            return Accepted();
                        }
                        else
                        {
                            return BadRequest();
                        }
                    }
                }
                else
                {
                    logger.LogError($"Unrecognized call ID in notification {notification.ResourceUrl}");
                }
            }
        }
        return BadRequest();
    }
}