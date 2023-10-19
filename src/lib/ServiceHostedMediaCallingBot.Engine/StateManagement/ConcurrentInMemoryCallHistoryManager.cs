using ServiceHostedMediaCallingBot.Engine.Models;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ServiceHostedMediaCallingBot.Engine.StateManagement;

public class ConcurrentInMemoryCallHistoryManager<T> : ICallHistoryManager<T> where T : BaseActiveCallState
{
    private readonly Dictionary<string, JsonElement[]> _callHistory = new();

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

                var newHistoryArray = new JsonElement[1] { graphNotificationPayload.RootElement };
                if (!_callHistory.ContainsKey(callState.CallId!))
                {
                    _callHistory[callState.CallId!] = newHistoryArray;
                }
                else
                {
                    _callHistory[callState.CallId!] = _callHistory[callState.CallId!].Concat(newHistoryArray).ToArray();
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

    Task ICallHistoryManager<T>.DeleteCallHistory(T callState)
    {
        lock (this)
        {
            if (callState.HasValidCallId)
            {
                if (_callHistory.ContainsKey(callState.CallId!))
                {
                    _callHistory.Remove(callState.CallId!);
                }
            }
        }
        return Task.CompletedTask;
    }
}
