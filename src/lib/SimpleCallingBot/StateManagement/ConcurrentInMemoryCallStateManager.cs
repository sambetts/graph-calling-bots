namespace SimpleCallingBot;

public class ConcurrentInMemoryCallStateManager : ICallStateManager
{
    private readonly Dictionary<string, ActiveCallState> _callStates = new();

    public Task AddCallState(ActiveCallState callState)
    {
        lock (this)
        {
            _callStates.Add(callState.ResourceUrl, callState);
        }
        return Task.CompletedTask;
    }

    public Task<ActiveCallState?> GetByNotificationResourceId(string resourceId)
    {
        lock (this)
        {
            ActiveCallState? callState = null;

            _callStates.TryGetValue(resourceId, out callState);
            return Task.FromResult(callState);
        }
    }

    public Task Remove(ActiveCallState callState)
    {
        lock (this)
        {
            _callStates.Remove(callState.ResourceUrl);
        }
        return Task.CompletedTask;
    }

    public Task UpdateByResourceId(ActiveCallState callState)
    {
        lock (this)
        {
            if (_callStates.ContainsKey(callState.ResourceUrl))
            {
                _callStates[callState.ResourceUrl] = callState;
            }
        }
        return Task.CompletedTask;  
    }
}
