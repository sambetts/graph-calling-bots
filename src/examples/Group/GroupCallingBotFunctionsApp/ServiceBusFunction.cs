using Azure.Messaging.ServiceBus;
using GraphCallingBots;
using GraphCallingBots.CallingBots;
using GraphCallingBots.EventQueue;
using GraphCallingBots.Models;
using GroupCalls.Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace GroupCallingBot.FunctionApp;


public class ServiceBusFunction(QueueManager<CommsNotificationsPayload> queueManager, ILogger<ServiceBusFunction> logger,
    BotCallRedirector<GroupCallBot, BaseActiveCallState> botCallRedirectorGroupCall,
    BotCallRedirector<CallInviteBot, GroupCallInviteActiveCallState> botCallRedirectorCallInviteCall)
{

    public const string SB_CONNECTION_NAME = "GraphMessagesSericeBusQueueCallUpdates";

    /// <summary>
    /// Processes call notifications from the Service Bus queue, one by one in the order received.
    /// </summary>
    [Function(nameof(ProcessCallNotification))]
    public async Task ProcessCallNotification(
        [ServiceBusTrigger(GraphUpdatesAzureServiceBusJsonQueueAdapter.SB_QUEUE_NAME, Connection = SB_CONNECTION_NAME, IsBatched = false)]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        logger.LogInformation("Message ID: {id}", message.MessageId);

        var notificationsPayload = await queueManager.ProcessMessage(message.Body.ToString() ?? string.Empty);
        if (notificationsPayload == null)
        {
            logger.LogWarning("Failed to process message with ID: {id}", message.MessageId);
            await messageActions.AbandonMessageAsync(message);
            return; // Exit if processing failed
        }

        if (notificationsPayload != null)
        {
            foreach (var notification in notificationsPayload.CommsNotifications)
            {
                var callId = BaseActiveCallState.GetCallId(notification.ResourceUrl);
                if (callId != null)
                {
                    var botGroupCall = await GetBotAndHandleNotifications(botCallRedirectorGroupCall, callId, notificationsPayload);

                    if (botGroupCall != null)        // Logging for negative handled in GetBotByCallId
                    {
                        logger.LogInformation($"Processing {notificationsPayload.CommsNotifications.Count} Graph call notification(s) for GroupCall bot.");
                        try
                        {
                            var stats = await botGroupCall.HandleNotificationsAndUpdateCallStateAsync(notificationsPayload);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"Error handling notifications: {ex.Message}");
                            await messageActions.AbandonMessageAsync(message);
                            return;
                        }
                    }
                    else
                    {

                        var botInviteCall = await GetBotAndHandleNotifications(botCallRedirectorCallInviteCall, callId, notificationsPayload);
                        if (botInviteCall != null)
                        {
                            logger.LogInformation($"Processing {notificationsPayload.CommsNotifications.Count} Graph call notification(s) for CallInvite bot.");

                            try
                            {
                                var stats = await botInviteCall.HandleNotificationsAndUpdateCallStateAsync(notificationsPayload);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError($"Error handling notifications: {ex.Message}");
                                await messageActions.AbandonMessageAsync(message);
                                return;
                            }
                        }
                        else
                        {
                            logger.LogWarning($"No bot found for call ID {callId} in notification {notification.ResourceUrl}");
                        }
                    }
                }
                else
                {
                    logger.LogError($"Unrecognized call ID in notification {notification.ResourceUrl}");
                }
            }


            // Complete the message
            await messageActions.CompleteMessageAsync(message);
        }
        else
        {
            logger.LogError($"Unrecognized request body: {message.Body}");
            await messageActions.AbandonMessageAsync(message);
            return;
        }
    }

    private async Task<BOTTYPE?> GetBotAndHandleNotifications<BOTTYPE, CALLSTATETYPE>(
        BotCallRedirector<BOTTYPE, CALLSTATETYPE> botCallRedirector, string callId, CommsNotificationsPayload notificationsPayload)
        where BOTTYPE : BaseBot<CALLSTATETYPE>
        where CALLSTATETYPE : BaseActiveCallState, new()
    {
        var bot = await botCallRedirector.GetBotByCallId(callId);
        if (bot != null)
        {
            await bot.HandleNotificationsAndUpdateCallStateAsync(notificationsPayload);
        }

        return bot;
    }

}