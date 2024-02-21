using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceHostedMediaCallingBot.Engine.Models;

namespace ServiceHostedMediaCallingBot.Engine.StateManagement.Sql;

public class SqlCallHistoryManager<CALLSTATETYPE, HISTORYPAYLOADTYPE> : ICallHistoryManager<CALLSTATETYPE, HISTORYPAYLOADTYPE>
    where CALLSTATETYPE : BaseActiveCallState
    where HISTORYPAYLOADTYPE : class
{
    private bool _initialised = false;
    private readonly CallHistoryContext<CALLSTATETYPE, HISTORYPAYLOADTYPE> _context;
    private readonly ILogger<SqlCallHistoryManager<CALLSTATETYPE, HISTORYPAYLOADTYPE>> _logger;

    public bool Initialised => _initialised;

    public SqlCallHistoryManager(CallHistoryContext<CALLSTATETYPE, HISTORYPAYLOADTYPE> context, ILogger<SqlCallHistoryManager<CALLSTATETYPE, HISTORYPAYLOADTYPE>> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddToCallHistory(CALLSTATETYPE callState, HISTORYPAYLOADTYPE graphNotificationPayload)
    {
        if (callState?.CallId == null)
        {
            throw new ArgumentNullException(nameof(callState.CallId));
        }
        var newHistoryArray = new List<NotificationHistory<HISTORYPAYLOADTYPE>> { new NotificationHistory<HISTORYPAYLOADTYPE>
        {
            Payload = graphNotificationPayload, Timestamp = DateTime.Now }
        };
        var newCallStateList = new List<CALLSTATETYPE> { callState };
        var callRecordFound = await GetCall(callState.CallId);
        if (callRecordFound != null && callRecordFound.CallHistory != null)
        {
            _logger.LogInformation($"Adding to existing call history for call {callState.CallId}");

            if (callRecordFound.CallHistory.StateHistory.Count > 0 && !callRecordFound.CallHistory.StateHistory.Last().Equals(callState))
            {
                // Only update state if changed
                var newList = callRecordFound.CallHistory.StateHistory.Concat(newCallStateList).ToList();

                var callHistoryRecordExistingReplacement = new CallHistorySqlEntry<CALLSTATETYPE, HISTORYPAYLOADTYPE>(callState)
                {
                    CallHistory = callRecordFound.CallHistory
                };
                callHistoryRecordExistingReplacement.CallHistory.StateHistory = newList;

                // Update Json
                callRecordFound.CallHistory = callHistoryRecordExistingReplacement.CallHistory;
            }
            await _context.SaveChangesAsync();
        }
        else
        {
            _logger.LogInformation($"Creating new call history for call {callState.CallId}");
            var callHistoryRecordNew = new CallHistorySqlEntry<CALLSTATETYPE, HISTORYPAYLOADTYPE>(callState);
            callHistoryRecordNew.CallHistory = new CallHistoryEntity<CALLSTATETYPE, HISTORYPAYLOADTYPE>
            {
                NotificationsHistory = newHistoryArray,
                StateHistory = newCallStateList,
                Timestamp = DateTime.UtcNow
            };
            _context.CallsMade.Add(callHistoryRecordNew);
            await _context.SaveChangesAsync();
        }
    }

    async Task<CallHistorySqlEntry<CALLSTATETYPE, HISTORYPAYLOADTYPE>?> GetCall(string callId)
    {
        return await _context.CallsMade.Where(h => h.CallId == callId).SingleOrDefaultAsync();
    }

    public async Task<CallHistoryEntity<CALLSTATETYPE, HISTORYPAYLOADTYPE>?> GetCallHistory(CALLSTATETYPE callState)
    {
        if (callState?.CallId == null)
        {
            throw new ArgumentNullException(nameof(callState.CallId));
        }

        var result = await GetCall(callState.CallId);
        return result?.CallHistory;
    }

    public async Task DeleteCallHistory(CALLSTATETYPE callState)
    {
        if (callState?.CallId == null)
        {
            throw new ArgumentNullException(nameof(callState.CallId));
        }
        var call = await GetCall(callState.CallId);
        if (call != null)
        {
            _context.CallsMade.Remove(call);
            await _context.SaveChangesAsync();
        }
    }

    public async Task Initialise()
    {
        _initialised = true;
        await _context.Database.EnsureCreatedAsync();
    }
}
