using Newtonsoft.Json;
using ServiceHostedMediaCallingBot.Engine.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceHostedMediaCallingBot.Engine.StateManagement.Sql;

/// <summary>
/// SQL Server Entity Framework Core entry for call history
/// </summary>
public class CallStateAndNotificationsHistoryEntitySqlEntry<CALLSTATETYPE, HISTORYPAYLOADTYPE> : CallStateAndNotificationsHistoryEntity<CALLSTATETYPE, HISTORYPAYLOADTYPE>
    where CALLSTATETYPE : BaseActiveCallState
    where HISTORYPAYLOADTYPE : class
{
    public CallStateAndNotificationsHistoryEntitySqlEntry() : this(null) { }
    public CallStateAndNotificationsHistoryEntitySqlEntry(CALLSTATETYPE? callState)
    {
        CallId = callState?.CallId ?? string.Empty;
        LastUpdated = DateTime.UtcNow;
    }

    [Key]
    public string CallId { get; set; }

    public string NotificationsHistoryJson { get; set; } = null!;

    [NotMapped]
    public override List<NotificationHistory<HISTORYPAYLOADTYPE>> NotificationsHistory
    {
        get => GetListFromJson<NotificationHistory<HISTORYPAYLOADTYPE>>(NotificationsHistoryJson);
        set => NotificationsHistoryJson = JsonConvert.SerializeObject(value);
    }

    public string StateHistoryJson { get; set; } = null!;

    [NotMapped]
    public override List<CALLSTATETYPE> StateHistory
    {
        get => GetListFromJson<CALLSTATETYPE>(StateHistoryJson);
        set => StateHistoryJson = JsonConvert.SerializeObject(value);
    }

    static List<T> GetListFromJson<T>(string json) where T : class
    {
        var empty = new List<T>();
        try
        {
            return JsonConvert.DeserializeObject<List<T>>(json) ?? empty;
        }
        catch (Exception)
        {
            return empty;
        }
    }

    public DateTime? LastUpdated { get; set; } = null;
}
