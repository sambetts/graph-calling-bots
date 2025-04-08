using GraphCallingBots.Models;
using System.Text.Json;

namespace GraphCallingBots.StateManagement;

public class ConcurrentInMemoryCallHistoryManager<CALLSTATETYPE> : ICallHistoryManager<CALLSTATETYPE>
    where CALLSTATETYPE : BaseActiveCallState
{
    private readonly Dictionary<string, CallStateAndNotificationsHistoryEntity<CALLSTATETYPE>> _callHistory = new();

    public Task Initialise()
    {
        return Task.CompletedTask;
    }
    public bool Initialised => true;        // Nothing to initialise

    public Task AddToCallHistory(CALLSTATETYPE callState, JsonElement graphNotificationPayload)
    {
        lock (this)
        {
            if (callState.HasValidCallId)
            {
                var newHistoryArray = new List<NotificationHistory>
                {
                    new NotificationHistory { Payload = graphNotificationPayload.ToString(), Timestamp = DateTime.Now }
                };
                var newCallStateList = new List<CALLSTATETYPE> { callState };

                if (!_callHistory.ContainsKey(callState.CallId!))
                {
                    _callHistory.Add(callState.CallId!, new CallStateAndNotificationsHistoryEntity<CALLSTATETYPE>(callState) { NotificationsHistory = newHistoryArray, StateHistory = newCallStateList });
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

    public Task<CallStateAndNotificationsHistoryEntity<CALLSTATETYPE>?> GetCallHistory(CALLSTATETYPE callState)
    {
        lock (this)
        {
            if (callState.HasValidCallId)
            {
                if (_callHistory.ContainsKey(callState.CallId!))
                {
                    return Task.FromResult<CallStateAndNotificationsHistoryEntity<CALLSTATETYPE>?>(_callHistory[callState.CallId!]);
                }
                else
                {
                    Task.FromResult<CallStateAndNotificationsHistoryEntity<CALLSTATETYPE>?>(new CallStateAndNotificationsHistoryEntity<CALLSTATETYPE>(callState));
                }
            }
        }
        return Task.FromResult<CallStateAndNotificationsHistoryEntity<CALLSTATETYPE>?>(null);
    }

    Task ICallHistoryManager<CALLSTATETYPE>.DeleteCallHistory(CALLSTATETYPE callState)
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
