using Azure.Messaging.ServiceBus;
using GraphCallingBots.EventQueue;
using GraphCallingBots.Models;
using GroupCalls.Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace GroupCallingBot.FunctionApp;


public class ServiceBusFunction(MessageQueueManager<CommsNotificationsPayload> queueManager, GroupCallOrchestrator callOrchestrator, ILogger<ServiceBusFunction> logger)
{

    public const string SB_CONNECTION_NAME = "GraphMessagesServiceBusQueueCallUpdates";

    /// <summary>
    /// Processes call notifications from the Service Bus queue, one by one in the order received.
    /// </summary>
    [Function(nameof(ProcessCallNotification))]
    public async Task ProcessCallNotification(
        [ServiceBusTrigger(GraphUpdatesAzureServiceBusJsonQueueAdapter.SB_QUEUE_NAME, Connection = SB_CONNECTION_NAME, IsBatched = false)]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        logger.LogInformation("Processing service-bus message ID: {id}", message.MessageId);
        var body = message.Body.ToString();

        var notificationsPayload = await queueManager.ProcessMessage(body);
        if (notificationsPayload == null)
        {
            logger.LogError("Failed to process message with ID: {id}", message.MessageId);
            await messageActions.DeadLetterMessageAsync(message, null, "Invalid Json");
            return; // Exit if processing failed
        }

        if (notificationsPayload != null)
        {
            await callOrchestrator.HandleNotificationsForOneBotOrAnotherAsync(notificationsPayload, queueManager, 
                async () => await LogAndDeadLetter(messageActions, message));

            // Complete the message
            logger.LogInformation($"Successfully processed message with ID: {message.MessageId}");
            await messageActions.CompleteMessageAsync(message);
        }
        else
        {
            logger.LogError($"Unrecognized request body: {message.Body}");
            await messageActions.DeadLetterMessageAsync(message, null, deadLetterReason: "ProcessingError", $"Unrecognized request body: {message.Body}");
            return;
        }
    }

    private async Task LogAndDeadLetter(ServiceBusMessageActions messageActions, ServiceBusReceivedMessage message)
    {
        logger.LogError($"Failed to process message with ID: {message.MessageId}. Dead-lettering the message.");
        await messageActions.DeadLetterMessageAsync(message, null, deadLetterReason: "ProcessingError", "Failed to process the message due to an error.");
    }
}
