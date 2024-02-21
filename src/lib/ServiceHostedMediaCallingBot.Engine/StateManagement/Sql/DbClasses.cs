using ServiceHostedMediaCallingBot.Engine.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ServiceHostedMediaCallingBot.Engine.StateManagement.Sql;


public abstract class BaseSqlClass
{
    [Key]
    public int Id { get; set; } = 0;
}

public class CallHistorySqlEntry<CALLSTATETYPE, HISTORYPAYLOADTYPE> : BaseSqlClass
    where CALLSTATETYPE : BaseActiveCallState
    where HISTORYPAYLOADTYPE : class
{
    public CallHistorySqlEntry() : this(null) { }
    public CallHistorySqlEntry(CALLSTATETYPE? callState)
    {
        CallId = callState?.CallId ?? string.Empty;
        LastUpdated = DateTime.UtcNow;
    }

    public string CallId { get; set; }

    public string CallHistoryJson { get; set; } = null!;

    [NotMapped]
    public CallHistoryEntity<CALLSTATETYPE, HISTORYPAYLOADTYPE>? CallHistory
    {
        get
        {
            return JsonSerializer.Deserialize<CallHistoryEntity<CALLSTATETYPE, HISTORYPAYLOADTYPE>>(CallHistoryJson);
        }
        set
        {
            CallHistoryJson = JsonSerializer.Serialize(value);
        }
    }

    public DateTime? LastUpdated { get; set; } = null;
}

public class History : BaseSqlClass
{

}
