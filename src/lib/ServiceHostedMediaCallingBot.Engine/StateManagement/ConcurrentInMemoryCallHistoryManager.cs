using ServiceHostedMediaCallingBot.Engine.Models;

namespace ServiceHostedMediaCallingBot.Engine.StateManagement;

public class ConcurrentInMemoryCallHistoryManager<CALLSTATETYPE, HISTORYPAYLOADTYPE> : ICallHistoryManager<CALLSTATETYPE, HISTORYPAYLOADTYPE> 
    where CALLSTATETYPE : BaseActiveCallState 
    where HISTORYPAYLOADTYPE : class
{
    private readonly Dictionary<string, CallStateAndNotificationsHistoryEntity<CALLSTATETYPE, HISTORYPAYLOADTYPE>> _callHistory = new();

    public Task Initialise()
    {
        return Task.CompletedTask;
    }
    public bool Initialised => true;        // Nothing to initialise

    public Task AddToCallHistory(CALLSTATETYPE callState, HISTORYPAYLOADTYPE graphNotificationPayload)
    {
        lock (this)
        {
            if (callState.HasValidCallId)
            {
                var newHistoryArray = new List<NotificationHistory<HISTORYPAYLOADTYPE>> 
                { 
                    new NotificationHistory<HISTORYPAYLOADTYPE> { Payload = graphNotificationPayload, Timestamp = DateTime.Now } 
                };
                var newCallStateList = new List<CALLSTATETYPE> { callState };

                if (!_callHistory.ContainsKey(callState.CallId!))
                {
                    _callHistory.Add(callState.CallId!, new CallStateAndNotificationsHistoryEntity<CALLSTATETYPE, HISTORYPAYLOADTYPE>(callState) { NotificationsHistory = newHistoryArray, StateHistory = newCallStateList });
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

    public Task<CallStateAndNotificationsHistoryEntity<CALLSTATETYPE, HISTORYPAYLOADTYPE>?> GetCallHistory(CALLSTATETYPE callState)
    {
        lock (this)
        {
            if (callState.HasValidCallId)
            {
                if (_callHistory.ContainsKey(callState.CallId!))
                {
                    return Task.FromResult<CallStateAndNotificationsHistoryEntity<CALLSTATETYPE, HISTORYPAYLOADTYPE>?>( _callHistory[callState.CallId!]);
                }
                else
                {
                    Task.FromResult<CallStateAndNotificationsHistoryEntity<CALLSTATETYPE, HISTORYPAYLOADTYPE>?>(new CallStateAndNotificationsHistoryEntity<CALLSTATETYPE, HISTORYPAYLOADTYPE>(callState));
                }
            }
        }
        return Task.FromResult<CallStateAndNotificationsHistoryEntity<CALLSTATETYPE, HISTORYPAYLOADTYPE>?>(null);
    }

    Task ICallHistoryManager<CALLSTATETYPE, HISTORYPAYLOADTYPE>.DeleteCallHistory(CALLSTATETYPE callState)
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
