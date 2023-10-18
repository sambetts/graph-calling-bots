using ServiceHostedMediaCallingBot.Engine.Models;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ServiceHostedMediaCallingBot.Engine.StateManagement;

public class ConcurrentInMemoryCallHistoryManager<T> : ICallHistoryManager<T> where T : BaseActiveCallState
{
    private readonly Dictionary<string, JsonArray> _callHistory = new();

    public Task Initialise()
    {
        return Task.CompletedTask;
    }
    public bool Initialised => true;        // Nothing to initialise

    public Task AddToCallHistory(T callState, JsonDocument graphNotificationPayload)
    {
        lock (this)
        {
            if (callState.HasValidCallId)
            {
                if (!_callHistory.ContainsKey(callState.CallId!))
                {
                    _callHistory[callState.CallId!] = new JsonArray { graphNotificationPayload };
                }
                else
                {
                    _callHistory[callState.CallId!].Add(graphNotificationPayload);
                }
            }
        }
        return Task.CompletedTask;
    }

    public Task<CallHistoryEntity<T>?> GetCallHistory(T callState)
    {
        lock (this)
        {
            if (callState.HasValidCallId)
            {
                if (_callHistory.ContainsKey(callState.CallId!))
                {
                    return Task.FromResult<CallHistoryEntity<T>?>( new CallHistoryEntity<T>(callState) { NotificationsHistory = _callHistory[callState.CallId!] });
                }
                else
                {
                    Task.FromResult<CallHistoryEntity<T>?>(new CallHistoryEntity<T>(callState));
                }
            }
        }
        return Task.FromResult<CallHistoryEntity<T>?>(null);
    }
}
