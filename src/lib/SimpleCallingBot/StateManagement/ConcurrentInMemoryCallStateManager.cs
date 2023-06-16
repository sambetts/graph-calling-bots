namespace SimpleCallingBotEngine;

public class ConcurrentInMemoryCallStateManager : ICallStateManager
{
    private readonly Dictionary<string, ActiveCallState> _callStates = new();

    public Task AddCallState(ActiveCallState callState)
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

    public Task<ActiveCallState?> GetByNotificationResourceUrl(string resourceUrl)
    {
        var callId = ActiveCallState.GetCallId(resourceUrl);
        if (callId == null) return Task.FromResult<ActiveCallState?>(null);
        lock (this)
        {
            ActiveCallState? callState = null;

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

    public Task Update(ActiveCallState callState)
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
