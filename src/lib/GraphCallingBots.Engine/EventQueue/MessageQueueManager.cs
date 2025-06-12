using System.Collections.Concurrent;

namespace GraphCallingBots.EventQueue;

public class MessageQueueManager<T> where T : class, IJsonClassWithOriginalContent, new()
{
    private readonly IJsonQueueAdapter<T> _adapter;

    public MessageQueueManager(IJsonQueueAdapter<T> adapter)
    {
        _adapter = adapter;
    }

    public Task EnqueueAsync(T payload) => _adapter.EnqueueAsync(payload);

    public Task<T?> ProcessMessage(string json) 
    { 
        var payload = System.Text.Json.JsonSerializer.Deserialize<T>(json);
        payload ??= default;
        if (payload != null)
        {
            payload.OriginalContent = json;
        }
        return Task.FromResult(payload);
    }
}

public class InMemoryJsonQueueAdapter<T> : IJsonQueueAdapter<T>
{
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);

    public Task EnqueueAsync(T payload)
    {
        _queue.Enqueue(payload);
        _signal.Release();
        return Task.CompletedTask;
    }

    public async Task<T?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        await _signal.WaitAsync(cancellationToken);
        if (_queue.TryDequeue(out var payload))
            return payload;
        return default;
    }
}
