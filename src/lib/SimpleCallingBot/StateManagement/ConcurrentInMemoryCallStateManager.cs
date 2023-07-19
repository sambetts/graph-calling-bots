namespace SimpleCallingBotEngine;

public class ConcurrentInMemoryCallStateManager<T> : ICallStateManager<T> where T : ActiveCallState
{
    private readonly Dictionary<string, T> _callStates = new();

    public Task AddCallState(T callState)
    {
        if (callState is null || callState.CallId == null)
        {
            throw new ArgumentNullException(nameof(callState));
        }
        lock (this)
        {
            _callStates.Add(callState.CallId, callState);
        }
        return Task.CompletedTask;
    }

    public Task<T?> GetByNotificationResourceUrl(string resourceUrl)
    {
        var callId = ActiveCallState.GetCallId(resourceUrl);
        if (callId == null) return Task.FromResult<T?>(null);
        lock (this)
        {
            T? callState = null;

            _callStates.TryGetValue(callId, out callState);
            return Task.FromResult(callState);
        }
    }

    public Task<bool> Remove(string resourceUrl)
    {
        var callId = ActiveCallState.GetCallId(resourceUrl);
        if (callId == null) return Task.FromResult(false);

        lock (this)
        {
            var r = _callStates.Remove(callId);
            return Task.FromResult(r);
        }
    }

    public Task Update(T callState)
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

    public int Count
    {
        get
        {
            lock (this)
            {
                return _callStates.Count;
            }
        }
    }
}
