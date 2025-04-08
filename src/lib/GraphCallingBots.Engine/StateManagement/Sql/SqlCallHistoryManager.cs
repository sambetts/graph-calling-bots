using GraphCallingBots.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GraphCallingBots.StateManagement.Sql;

public class SqlCallHistoryManager<CALLSTATETYPE> : ICallHistoryManager<CALLSTATETYPE>
    where CALLSTATETYPE : BaseActiveCallState
{
    private bool _initialised = false;
    private readonly CallHistorySqlContext<CALLSTATETYPE> _context;
    private readonly ILogger<SqlCallHistoryManager<CALLSTATETYPE>> _logger;

    public bool Initialised => _initialised;

    public SqlCallHistoryManager(CallHistorySqlContext<CALLSTATETYPE> context, ILogger<SqlCallHistoryManager<CALLSTATETYPE>> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddToCallHistory(CALLSTATETYPE callState, JsonElement graphNotificationPayload)
    {
        if (callState?.CallId == null)
        {
            throw new ArgumentNullException(nameof(callState.CallId));
        }

        var newHistoryArray = new List<NotificationHistory> { new NotificationHistory 
        {
            Payload = graphNotificationPayload.ToString(), Timestamp = DateTime.Now }
        };
        var newCallStateList = new List<CALLSTATETYPE> { callState };
        var callRecordFound = await GetCall(callState.CallId);
        if (callRecordFound != null)
        {
            _logger.LogInformation($"Adding to existing call history for call {callState.CallId}");

            if (callRecordFound.StateHistory.Count > 0 && !callRecordFound.StateHistory.Last().Equals(callState))
            {
                // Only update state if changed
                var newHistoryList = callRecordFound.StateHistory.Concat(newCallStateList).ToList();

                callRecordFound.StateHistory = newHistoryList;
            }
            callRecordFound.NotificationsHistory = callRecordFound.NotificationsHistory.Concat(newHistoryArray).ToList();
            await _context.SaveChangesAsync();
        }
        else
        {
            _logger.LogInformation($"Creating new call history for call {callState.CallId}");
            var callHistoryRecordNew = new CallStateAndNotificationsHistoryEntitySqlEntry<CALLSTATETYPE>(callState)
            {
                NotificationsHistory = newHistoryArray,
                StateHistory = newCallStateList,
                Timestamp = DateTime.UtcNow
            };

            _context.CallsMade.Add(callHistoryRecordNew);
            await _context.SaveChangesAsync();
        }
    }

    async Task<CallStateAndNotificationsHistoryEntitySqlEntry<CALLSTATETYPE>?> GetCall(string callId)
    {
        return await _context.CallsMade.Where(h => h.CallId == callId).SingleOrDefaultAsync();
    }

    public async Task<CallStateAndNotificationsHistoryEntity<CALLSTATETYPE>?> GetCallHistory(CALLSTATETYPE callState)
    {
        if (callState?.CallId == null)
        {
            throw new ArgumentNullException(nameof(callState.CallId));
        }

        var result = await GetCall(callState.CallId);
        return result;
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
