using Microsoft.EntityFrameworkCore;
using ServiceHostedMediaCallingBot.Engine.Models;

namespace ServiceHostedMediaCallingBot.Engine.StateManagement.Sql;


public class CallHistoryContext<CALLSTATETYPE, HISTORYPAYLOADTYPE> : DbContext
    where CALLSTATETYPE : BaseActiveCallState
    where HISTORYPAYLOADTYPE : class
{
    public DbSet<CallHistorySqlEntry<CALLSTATETYPE, HISTORYPAYLOADTYPE>> CallsMade { get; set; }

    public CallHistoryContext(DbContextOptions<CallHistoryContext<CALLSTATETYPE, HISTORYPAYLOADTYPE>> options) : base(options)
    {
    }
}
