using GraphCallingBots;
using GraphCallingBots.CallingBots;
using GraphCallingBots.EventQueue;
using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using GroupCalls.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace GroupCallingChatBot.Web.Controllers;

/// <summary>
/// Entry point for handling call-related web hook requests from the stateful client.
/// </summary>
public class PlatformCallController(GroupCallOrchestrator callOrchestrator, MessageQueueManager<CommsNotificationsPayload> queueManager, ILogger<PlatformCallController> logger) : ControllerBase
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
            var error = false;
            await callOrchestrator.HandleNotificationsForOneBotOrAnotherAsync(notificationsPayload,
                queueManager, 
                () =>
                {
                    logger.LogError("Failed to process notifications.");
                    error = true;
                    return Task.CompletedTask;
                });
            if (error)
            {
                return StatusCode(500, "Failed to process notifications.");
            }
            else
            {
                return Accepted("Notifications processed successfully.");
            }
        }
        else
        {
            return BadRequest("Invalid notifications payload.");
        }
    }
}