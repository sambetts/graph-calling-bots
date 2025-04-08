using GraphCallingBots.Models;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace GraphCallingBots.StateManagement.Sql;

/// <summary>
/// SQL Server Entity Framework Core entry for call history
/// </summary>
public class CallStateAndNotificationsHistoryEntitySqlEntry<CALLSTATETYPE> : CallStateAndNotificationsHistoryEntity<CALLSTATETYPE>
    where CALLSTATETYPE : BaseActiveCallState
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
    public override List<NotificationHistory> NotificationsHistory
    {
        get => GetListFromJson<NotificationHistory>(NotificationsHistoryJson);
        set => NotificationsHistoryJson = JsonConvert.SerializeObject(value);
    }

    public string StateHistoryJson { get; set; } = null!;

    [NotMapped]
    public override List<CALLSTATETYPE> StateHistory
    {
        get => GetListFromJson<CALLSTATETYPE>(StateHistoryJson);
        set => StateHistoryJson = JsonConvert.SerializeObject(value);
    }

    static List<T> GetListFromJson<T>(string json) 
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
