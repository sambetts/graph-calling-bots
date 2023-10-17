using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ServiceHostedMediaCallingBot.UnitTests.TestServices;

public class SlowInMemoryCallStateManager<T> : ICallStateManager<T> where T : BaseActiveCallState
{
    private readonly Dictionary<string, T> _callStates = new();
    private readonly Dictionary<string, JsonArray> _callHistory = new();

    public async Task AddCallStateOrUpdate(T callState)
    {
        if (callState is null || callState.CallId == null)
        {
            throw new ArgumentNullException(nameof(callState));
        }
        await Delay();

        lock (this)
        {
            _callStates.Add(callState.CallId, callState);
        }
    }


    public async Task<T?> GetByNotificationResourceUrl(string resourceUrl)
    {
        var callId = BaseActiveCallState.GetCallId(resourceUrl);
        if (callId == null) return null;

        T? callState = null;

        await Delay();
        _callStates.TryGetValue(callId, out callState);
        return callState;

    }
    public async Task<bool> RemoveCurrentCall(string resourceUrl)
    {
        var callId = BaseActiveCallState.GetCallId(resourceUrl);
        if (callId == null) return false;
        await Delay();

        lock (this)
        {
            var r = _callStates.Remove(callId);
            return r;
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

    public async Task<int> GetCurrentCallCount()
    {
        await Delay();

        lock (this)
        {
            return _callStates.Count;
        }
    }

    public async Task AddToCallHistory(T callState, JsonDocument graphNotificationPayload)
    {
        await Delay();
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
    }

    async Task Delay()
    {
        await Task.Delay(100);
    }

    public async Task<CallHistoryEntity<T>?> GetCallHistory(T callState)
    {
        await Delay();
        lock (this)
        {
            if (callState.HasValidCallId)
            {
                if (!_callStates.ContainsKey(callState.CallId!))
                {
                    return null;
                }
                if (_callHistory.ContainsKey(callState.CallId!))
                {
                    return new CallHistoryEntity<T>(callState) { NotificationsHistory = _callHistory[callState.CallId!] };
                }
                else
                {
                    return new CallHistoryEntity<T>(callState);
                }
            }
        }
        return null;
    }
}
