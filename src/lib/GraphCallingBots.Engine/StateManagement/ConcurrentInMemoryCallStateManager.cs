using GraphCallingBots.Models;

namespace GraphCallingBots.StateManagement;

public class ConcurrentInMemoryCallStateManager<T> : ICallStateManager<T> where T : BaseActiveCallState
{
    private readonly Dictionary<string, T> _callStatesByCallId = new();

    public virtual Task AddCallStateOrUpdate(T callState)
    {
        if (callState is null || callState.CallId == null)
        {
            throw new ArgumentNullException(nameof(callState));
        }
        lock (this)
        {
            if (_callStatesByCallId.ContainsKey(callState.CallId))
            {
                _callStatesByCallId[callState.CallId] = callState;
            }
            else
            {
                _callStatesByCallId.Add(callState.CallId, callState);
            }
        }
        return Task.CompletedTask;
    }

    public virtual Task<T?> GetStateByCallId(string callId)
    {
        if (callId == null) return Task.FromResult<T?>(null);
        return GetByCallId(callId);
    }

    public Task<T?> GetByCallId(string callId)
    {
        lock (this)
        {
            T? callState = null;

            _callStatesByCallId.TryGetValue(callId, out callState);
            return Task.FromResult(callState);
        }
    }

    public virtual Task<bool> RemoveCurrentCall(string callId)
    {
        if (callId == null) return Task.FromResult(false);

        lock (this)
        {
            var r = _callStatesByCallId.Remove(callId);
            return Task.FromResult(r);
        }
    }

    public virtual Task UpdateCurrentCallState(T callState)
    {
        if (callState is null || callState.CallId == null)
        {
            throw new ArgumentNullException(nameof(callState));
        }

        lock (this)
        {
            if (_callStatesByCallId.ContainsKey(callState.CallId))
            {
                _callStatesByCallId[callState.CallId] = callState;
            }
        }
        return Task.CompletedTask;
    }

    public Task Initialise()
    {
        return Task.CompletedTask;
    }
    public bool Initialised => true;        // Nothing to initialise


    public virtual Task<List<T>> GetActiveCalls()
    {

        lock (this)
        {
            return Task.FromResult(_callStatesByCallId.Select(s => s.Value).ToList());
        }
    }
}
