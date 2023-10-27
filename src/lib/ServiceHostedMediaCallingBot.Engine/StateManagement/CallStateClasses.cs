using Azure.Data.Tables;
using Azure;
using System.Runtime.Serialization;
using ServiceHostedMediaCallingBot.Engine.Models;
using System.Text.Json;

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
    public T? State { get => JsonSerializer.Deserialize<T>(StateJson); set => StateJson = JsonSerializer.Serialize(value); }

    public string StateJson { get; set; } = null!;
}


public class CallHistoryEntity<CALLSTATETYPE, HISTORYPAYLOADTYPE> where CALLSTATETYPE : BaseActiveCallState
    where HISTORYPAYLOADTYPE : class
{
    public CallHistoryEntity()
    {
    }
    public CallHistoryEntity(CALLSTATETYPE initialState) : this()
    {
        StateHistory = new List<CALLSTATETYPE> {initialState};
    }

    public DateTimeOffset? Timestamp { get; set; } = DateTime.UtcNow;

    public List<CALLSTATETYPE> StateHistory { get; set; } = new();

    public List<NotificationHistory<HISTORYPAYLOADTYPE>> NotificationsHistory { get; set; } = new();
}

public class NotificationHistory<HISTORYPAYLOADTYPE> where HISTORYPAYLOADTYPE : class
{
    public HISTORYPAYLOADTYPE? Payload { get; set; }
    public DateTime Timestamp { get; set; }
}
