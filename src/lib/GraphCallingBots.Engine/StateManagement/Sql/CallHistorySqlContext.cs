using GraphCallingBots.Models;
using Microsoft.EntityFrameworkCore;

namespace GraphCallingBots.StateManagement.Sql;


public class CallHistorySqlContext<CALLSTATETYPE, HISTORYPAYLOADTYPE> : DbContext
    where CALLSTATETYPE : BaseActiveCallState
    where HISTORYPAYLOADTYPE : class
{
    public DbSet<CallStateAndNotificationsHistoryEntitySqlEntry<CALLSTATETYPE, HISTORYPAYLOADTYPE>> CallsMade { get; set; }

    public CallHistorySqlContext(DbContextOptions<CallHistorySqlContext<CALLSTATETYPE, HISTORYPAYLOADTYPE>> options) : base(options)
    {
    }
}
