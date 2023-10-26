using ServiceHostedMediaCallingBot.Engine.Models;

namespace ServiceHostedMediaCallingBot.Engine.StateManagement;

public class ConcurrentInMemoryCallHistoryManager<T> : ICallHistoryManager<T> where T : BaseActiveCallState
{
    private readonly Dictionary<string, CallHistoryEntity<T>> _callHistory = new();

    public Task Initialise()
    {
        return Task.CompletedTask;
    }
    public bool Initialised => true;        // Nothing to initialise

    public Task AddToCallHistory(T callState, object graphNotificationPayload)
    {
        lock (this)
        {
            if (callState.HasValidCallId)
            {
                var newHistoryArray = new List<NotificationHistory> { new NotificationHistory { Payload = graphNotificationPayload, Timestamp = DateTime.Now } };
                var newCallStateList = new List<T> { callState };

                if (!_callHistory.ContainsKey(callState.CallId!))
                {
                    _callHistory.Add(callState.CallId!, new CallHistoryEntity<T>(callState) { NotificationsHistory = newHistoryArray, StateHistory = newCallStateList });
                }
                else
                {
                    _callHistory[callState.CallId!].NotificationsHistory = _callHistory[callState.CallId!].NotificationsHistory.Concat(newHistoryArray).ToList();
                    _callHistory[callState.CallId!].StateHistory = _callHistory[callState.CallId!].StateHistory.Concat(newCallStateList).ToList();
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
                    return Task.FromResult<CallHistoryEntity<T>?>( _callHistory[callState.CallId!]);
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
