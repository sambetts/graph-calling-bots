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


public class CallHistoryEntity<T> where T : BaseActiveCallState
{
    public CallHistoryEntity()
    {
    }
    public CallHistoryEntity(T initialState) : this()
    {
        StateHistory = new List<T> {initialState};
    }

    public DateTimeOffset? Timestamp { get; set; } = DateTime.UtcNow;

    public List<T> StateHistory { get; set; } = new();

    public List<NotificationHistory> NotificationsHistory { get; set; } = new();
}

public class NotificationHistory
{
    public object? Payload { get; set; }
    public DateTime Timestamp { get; set; }
}
