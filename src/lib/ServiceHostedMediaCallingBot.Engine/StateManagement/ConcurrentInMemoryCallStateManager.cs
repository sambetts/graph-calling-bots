using ServiceHostedMediaCallingBot.Engine.Models;

namespace ServiceHostedMediaCallingBot.Engine.StateManagement;

public class ConcurrentInMemoryCallStateManager<T> : ICallStateManager<T> where T : BaseActiveCallState
{
    private readonly Dictionary<string, T> _callStates = new();

    public virtual Task AddCallStateOrUpdate(T callState)
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

    public virtual Task<T?> GetByNotificationResourceUrl(string resourceUrl)
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

    public virtual Task<bool> RemoveCurrentCall(string resourceUrl)
    {
        var callId = BaseActiveCallState.GetCallId(resourceUrl);
        if (callId == null) return Task.FromResult(false);

        lock (this)
        {
            var r = _callStates.Remove(callId);
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


    public virtual Task<List<T>> GetActiveCalls()
    {

        lock (this)
        {
            return Task.FromResult(_callStates.Select(s => s.Value).ToList());
        }
    }
}
