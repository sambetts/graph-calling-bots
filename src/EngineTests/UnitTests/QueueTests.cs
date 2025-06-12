using GraphCallingBots.EventQueue;
using System.Text.Json.Nodes;

namespace GraphCallingBots.UnitTests;

[TestClass]
public class QueueTests
{
    [TestMethod]
    public async Task InMemoryJsonQueueAdapter_EnqueueDequeue_WorksInOrder()
    {
        var adapter = new InMemoryJsonQueueAdapter<JsonObject>();

        var payload1 = new JsonObject { ["id"] = 1, ["msg"] = "first" };
        var payload2 = new JsonObject { ["id"] = 2, ["msg"] = "second" };

        await adapter.EnqueueAsync(payload1);
        await adapter.EnqueueAsync(payload2);

        var result1 = await adapter.DequeueAsync();
        var result2 = await adapter.DequeueAsync();

        Assert.IsNotNull(result1);
        Assert.IsNotNull(result2);
        Assert.AreEqual(1, (int)result1!["id"]!);
        Assert.AreEqual("first", (string)result1!["msg"]!);
        Assert.AreEqual(2, (int)result2!["id"]!);
        Assert.AreEqual("second", (string)result2!["msg"]!);
    }

    [TestMethod]
    public async Task InMemoryJsonQueueAdapter_DequeueAsync_CanBeCancelled()
    {
        var adapter = new InMemoryJsonQueueAdapter<JsonObject>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
        {
            await adapter.DequeueAsync(cts.Token);
        });
    }

    [TestMethod]
    public async Task InMemoryJsonQueueAdapter_DequeueAsync_ReturnsNullIfQueueEmptyAfterSignal()
    {
        var adapter = new InMemoryJsonQueueAdapter<JsonObject>();

        // Use reflection to access the private _signal and release it
        var signalField = typeof(InMemoryJsonQueueAdapter<JsonObject>).GetField("_signal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var signal = (SemaphoreSlim)signalField!.GetValue(adapter)!;
        signal.Release();

        var result = await adapter.DequeueAsync();
        Assert.IsNull(result);
    }
}