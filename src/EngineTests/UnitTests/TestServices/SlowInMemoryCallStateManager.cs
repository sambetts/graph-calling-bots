using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace ServiceHostedMediaCallingBot.UnitTests.TestServices;

public class SlowInMemoryCallStateManager<T> : ICallStateManager<T> where T : BaseActiveCallState
{
    private readonly Dictionary<string, T> _callStates = new();

    public async Task AddCallState(T callState)
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

    async Task Delay()
    {
        await Task.Delay(100);
    }

    public async Task<bool> Remove(string resourceUrl)
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

    public Task Initialise()
    {
        return Task.CompletedTask;
    }
    public bool Initialised => true;        // Nothing to initialise

    public async Task<int> GetCount()
    {
        await Delay();

        lock (this)
        {
            return _callStates.Count;
        }
    }
}
