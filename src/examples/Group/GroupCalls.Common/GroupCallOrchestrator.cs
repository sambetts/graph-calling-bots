using GraphCallingBots;
using GraphCallingBots.CallingBots;
using GraphCallingBots.EventQueue;
using GraphCallingBots.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;

namespace GroupCalls.Common;

public class GroupCallOrchestrator(GroupCallBot groupCallingBot, CallInviteBot callInviteBot, BotCallRedirector<GroupCallBot, BaseActiveCallState> botCallRedirectorGroupCall,
    BotCallRedirector<CallInviteBot, GroupCallInviteActiveCallState> botCallRedirectorCallInviteCall, ILogger<GroupCallOrchestrator> logger)
{
    /// <summary>
    /// Begin both bot calls to start a group call.
    /// </summary>
    public async Task<Call?> StartGroupCall(StartGroupCallData newCallReq)
    {
        var groupCall = await groupCallingBot.CreateGroupCall(newCallReq);
        if (groupCall != null)
        {
            logger.LogInformation($"Started group call with ID {groupCall.Id}");

            if (groupCall != null)
            {
                foreach (var attendee in newCallReq.Attendees)
                {
                    var inviteCall = await callInviteBot.CallCandidateForGroupCall(attendee, newCallReq, groupCall);
                    if (inviteCall == null)
                    {
                        logger.LogError($"Failed to invite {attendee.DisplayName} ({attendee.Id})");
                    }
                    else
                    {
                        logger.LogInformation($"Invited '{attendee.DisplayName ?? "Unknown display name"}' (id '{attendee.Id}') on new P2P call {inviteCall.Id}");
                    }
                }

                return groupCall;
            }
            else
            {
                return null;
            }
        }
        else
        {
            logger.LogError("Failed to start group call - check previous errors");
            return null;
        }
    }

    /// <summary>
    /// Figure out which bot to use for the call ID in the notification and handle the notifications for that bot.
    /// </summary>
    public async Task HandleNotificationsForOneBotOrAnotherAsync(CommsNotificationsPayload notificationsPayload,
        MessageQueueManager<CommsNotificationsPayload> queueManager,
        Func<Task> onNotificationExceptionCallback
    )
    {
        foreach (var notification in notificationsPayload.CommsNotifications)
        {
            var callId = BaseActiveCallState.GetCallId(notification.ResourceUrl);
            if (callId != null)
            {
                var botGroupCall = await GetBotAndHandleNotifications(botCallRedirectorGroupCall, callId, notificationsPayload, onNotificationExceptionCallback);

                if (botGroupCall != null)        // Logging for negative handled in GetBotByCallId
                {
                    logger.LogInformation($"Processing {notificationsPayload.CommsNotifications.Count} Graph call notification(s) for GroupCall bot.");
                }
                else
                {
                    var botInviteCall = await GetBotAndHandleNotifications(botCallRedirectorCallInviteCall, callId, notificationsPayload, onNotificationExceptionCallback);
                    if (botInviteCall != null)
                    {
                        logger.LogInformation($"Processing {notificationsPayload.CommsNotifications.Count} Graph call notification(s) for CallInvite bot.");
                    }
                    else
                    {
                        // No bot found for the call ID, log a warning
                        logger.LogWarning($"No bot found for call ID {callId} in notification {notification.ResourceUrl}");
                    }
                }
            }
            else
            {
                logger.LogError($"Unrecognized call ID in notification {notification.ResourceUrl}");
            }
        }
    }

    private async Task<BOTTYPE?> GetBotAndHandleNotifications<BOTTYPE, CALLSTATETYPE>(
        BotCallRedirector<BOTTYPE, CALLSTATETYPE> botCallRedirector,
        string callId,
        CommsNotificationsPayload notificationsPayload,
        Func<Task> onNotificationExceptionCallback)
        where BOTTYPE : BaseBot<CALLSTATETYPE>
        where CALLSTATETYPE : BaseActiveCallState, new()
    {
        var bot = await botCallRedirector.GetBotByCallId(callId);
        if (bot != null)
        {
            try
            {
                await bot.HandleNotificationsAndUpdateCallStateAsync(notificationsPayload);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error in GetBotAndHandleNotifications: {ex.Message}");
                await onNotificationExceptionCallback();
            }
        }

        return bot;
    }
}
