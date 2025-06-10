using Azure.Messaging.ServiceBus;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GraphCallingBots.EventQueue;

public class AzureServiceBusJsonQueueAdapter<T> : IJsonQueueAdapter<T>
{
    private readonly ServiceBusSender _sender;
    private readonly ServiceBusReceiver _receiver;

    public AzureServiceBusJsonQueueAdapter(ServiceBusClient client, string queueName)
    {
        _sender = client.CreateSender(queueName);
        _receiver = client.CreateReceiver(queueName);
    }

    public async Task EnqueueAsync(T payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var message = new ServiceBusMessage(json);
        await _sender.SendMessageAsync(message);
    }

    public async Task<T?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        var message = await _receiver.ReceiveMessageAsync(cancellationToken: cancellationToken);
        if (message == null) return default;

        var json = message.Body.ToString();
        var payload = JsonSerializer.Deserialize<T>(json);
        await _receiver.CompleteMessageAsync(message, cancellationToken);
        return payload;
    }
}