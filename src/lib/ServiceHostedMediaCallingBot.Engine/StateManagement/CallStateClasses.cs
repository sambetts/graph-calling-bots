using Azure;
using Azure.Data.Tables;
using ServiceHostedMediaCallingBot.Engine.Models;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServiceHostedMediaCallingBot.Engine.StateManagement;

public class CallStateEntity<T> : ITableEntity where T : BaseActiveCallState
{
    public const string PARTITION_KEY = "CallState";

    public CallStateEntity()
    {
    }
    public CallStateEntity(T state)
    {
        State = state;
    }

    public string PartitionKey
    {
        get { return PARTITION_KEY; }
        set
        {
            // ignore
        }
    }

    public string RowKey
    {
        get
        {
            return State?.CallId ?? throw new ArgumentNullException(nameof(State.CallId));
        }
        set
        {
            // ignore
        }
    }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    [IgnoreDataMember]
    public T? State
    {
        get => StateJson != null ? JsonSerializer.Deserialize<T>(StateJson, new JsonSerializerOptions { Converters = { new JsonStringEnumConverter () } }) : default;
        set => StateJson = JsonSerializer.Serialize(value);
    }

    public string StateJson { get; set; } = null!;
}


public class CallStateAndNotificationsHistoryEntity<CALLSTATETYPE, HISTORYPAYLOADTYPE>
    where CALLSTATETYPE : BaseActiveCallState
    where HISTORYPAYLOADTYPE : class
{
    public CallStateAndNotificationsHistoryEntity()
    {
    }
    public CallStateAndNotificationsHistoryEntity(CALLSTATETYPE initialState) : this()
    {
        StateHistory = new List<CALLSTATETYPE> { initialState };
    }

    public DateTimeOffset? Timestamp { get; set; } = DateTime.UtcNow;

    public virtual List<CALLSTATETYPE> StateHistory { get; set; } = new();

    public virtual List<NotificationHistory<HISTORYPAYLOADTYPE>> NotificationsHistory { get; set; } = new();
}

public class NotificationHistory<HISTORYPAYLOADTYPE> where HISTORYPAYLOADTYPE : class
{
    public HISTORYPAYLOADTYPE? Payload { get; set; }
    public DateTime Timestamp { get; set; }
}
