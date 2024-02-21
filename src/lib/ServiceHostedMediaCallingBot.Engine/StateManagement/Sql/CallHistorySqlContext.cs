using Microsoft.EntityFrameworkCore;
using ServiceHostedMediaCallingBot.Engine.Models;

namespace ServiceHostedMediaCallingBot.Engine.StateManagement.Sql;


public class CallHistorySqlContext<CALLSTATETYPE, HISTORYPAYLOADTYPE> : DbContext
    where CALLSTATETYPE : BaseActiveCallState
    where HISTORYPAYLOADTYPE : class
{
    public DbSet<CallStateAndNotificationsHistoryEntitySqlEntry<CALLSTATETYPE, HISTORYPAYLOADTYPE>> CallsMade { get; set; }

    public CallHistorySqlContext(DbContextOptions<CallHistorySqlContext<CALLSTATETYPE, HISTORYPAYLOADTYPE>> options) : base(options)
    {
    }
}
