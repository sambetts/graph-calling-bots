using ServiceHostedMediaCallingBot.Engine.Models;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ServiceHostedMediaCallingBot.Engine.StateManagement;

public class ConcurrentInMemoryCallStateManager<T> : ICallStateManager<T> where T : BaseActiveCallState
{
    private readonly Dictionary<string, T> _callStates = new();
    private readonly Dictionary<string, JsonArray> _callHistory = new();

    public Task AddCallStateOrUpdate(T callState)
    {
        if (callState is null || callState.CallId == null)
        {
            throw new ArgumentNullException(nameof(callState));
        }
        lock (this)
        {
            if (_callStates.ContainsKey(callState.CallId))
            {
                _callStates[callState.CallId] = callState;
            }
            else
            {
                _callStates.Add(callState.CallId, callState);
            }
        }
        return Task.CompletedTask;
    }

    public Task<T?> GetByNotificationResourceUrl(string resourceUrl)
    {
        var callId = BaseActiveCallState.GetCallId(resourceUrl);
        if (callId == null) return Task.FromResult<T?>(null);
        lock (this)
        {
            T? callState = null;

            _callStates.TryGetValue(callId, out callState);
            return Task.FromResult(callState);
        }
    }

    public Task<bool> RemoveCurrentCall(string resourceUrl)
    {
        var callId = BaseActiveCallState.GetCallId(resourceUrl);
        if (callId == null) return Task.FromResult(false);

        lock (this)
        {
            var r = _callStates.Remove(callId);
            return Task.FromResult(r);
        }
    }

    public Task UpdateCurrentCallState(T callState)
    {
        if (callState is null || callState.CallId == null)
        {
            throw new ArgumentNullException(nameof(callState));
        }

        lock (this)
        {
            if (_callStates.ContainsKey(callState.CallId))
            {
                _callStates[callState.CallId] = callState;
            }
        }
        return Task.CompletedTask;
    }

    public Task Initialise()
    {
        return Task.CompletedTask;
    }
    public bool Initialised => true;        // Nothing to initialise

    public Task<int> GetCurrentCallCount()
    {
        lock (this)
        {
            return Task.FromResult(_callStates.Count);
        }
    }

    public Task AddToCallHistory(T callState, string graphNotificationPayload)
    {
        throw new NotImplementedException();
    }

    public Task AddToCallHistory(T callState, JsonDocument graphNotificationPayload)
    {
        lock (this)
        {
            if (callState.HasValidCallId)
            {
                if (_callHistory.ContainsKey(callState.CallId!))
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
