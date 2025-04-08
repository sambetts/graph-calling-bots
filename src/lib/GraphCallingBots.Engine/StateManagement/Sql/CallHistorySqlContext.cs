using GraphCallingBots.Models;
using Microsoft.EntityFrameworkCore;

namespace GraphCallingBots.StateManagement.Sql;


public class CallHistorySqlContext<CALLSTATETYPE> : DbContext
    where CALLSTATETYPE : BaseActiveCallState
{
    public DbSet<CallStateAndNotificationsHistoryEntitySqlEntry<CALLSTATETYPE>> CallsMade { get; set; }

    public CallHistorySqlContext(DbContextOptions<CallHistorySqlContext<CALLSTATETYPE>> options) : base(options)
    {
    }
}
