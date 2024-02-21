using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceHostedMediaCallingBot.Engine.Models;

namespace ServiceHostedMediaCallingBot.Engine.StateManagement;

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
        var newHistoryArray = new List<NotificationHistory<HISTORYPAYLOADTYPE>> { new NotificationHistory<HISTORYPAYLOADTYPE> 
        { 
            Payload = graphNotificationPayload, Timestamp = DateTime.Now } 
        };
        var newCallStateList = new List<CALLSTATETYPE> { callState };
        var callHistoryRecordFound = await GetCallHistory(callState);
        if (callHistoryRecordFound != null)
        {
            _logger.LogInformation($"Adding to existing call history for call {callState.CallId}");
            var callHistoryRecordExistingReplacement = new CallHistoryCosmosDoc<CALLSTATETYPE, HISTORYPAYLOADTYPE>(callState);

            if (callHistoryRecordFound.StateHistory.Count > 0 && !callHistoryRecordFound.StateHistory.Last().Equals(callState))
            {
                // Only update state if changed
                callHistoryRecordFound.StateHistory = callHistoryRecordFound.StateHistory.Concat(newCallStateList).ToList();
            }
            callHistoryRecordFound.NotificationsHistory.AddRange(newHistoryArray);
            callHistoryRecordExistingReplacement.CallHistory = callHistoryRecordFound;
            await _context.SaveChangesAsync();
        }
        else
        {
            _logger.LogInformation($"Creating new call history for call {callState.CallId}");
            var callHistoryRecordNew = new CallHistoryCosmosDoc<CALLSTATETYPE, HISTORYPAYLOADTYPE>(callState);
            callHistoryRecordNew.CallHistory = new CallHistoryEntity<CALLSTATETYPE, HISTORYPAYLOADTYPE> 
            { 
                NotificationsHistory = newHistoryArray, 
                StateHistory = newCallStateList, 
                Timestamp = DateTime.UtcNow 
            };
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

public class CallHistorySqlEntry<CALLSTATETYPE, HISTORYPAYLOADTYPE> : StatsCosmosDoc
    where CALLSTATETYPE : BaseActiveCallState
    where HISTORYPAYLOADTYPE : class
{
    public CallHistorySqlEntry() : this(null) { }
    public CallHistorySqlEntry(CALLSTATETYPE? callState)
    {
        this.CallId = callState?.CallId ?? string.Empty;
        this.LastUpdated = DateTime.UtcNow;
    }

    public string CallId { get; set; }
    public CallHistoryEntity<CALLSTATETYPE, HISTORYPAYLOADTYPE> CallHistory { get; set; } = null!;

    public override string id { get => CallId; set => CallId = value; }

    public DateTime? LastUpdated { get; set; } = null;
}

public class CallHistoryContext<CALLSTATETYPE, HISTORYPAYLOADTYPE> : DbContext
    where CALLSTATETYPE : BaseActiveCallState
    where HISTORYPAYLOADTYPE : class
{
    public DbSet<CallHistorySqlEntry<CALLSTATETYPE, HISTORYPAYLOADTYPE>> CallsMade { get; set; }

    public CallHistoryContext(DbContextOptions<CallHistoryContext<CALLSTATETYPE, HISTORYPAYLOADTYPE>> options) : base(options)
    {
    }
}
