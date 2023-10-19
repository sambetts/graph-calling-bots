using Azure.Data.Tables;
using Azure;
using System.Runtime.Serialization;
using ServiceHostedMediaCallingBot.Engine.Models;
using System.Text.Json;
using System.Text.Json.Nodes;

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


public class CallHistoryEntity<T> : ITableEntity where T : BaseActiveCallState
{
    public const string PARTITION_KEY = "CallHistory";

    public CallHistoryEntity()
    {
    }
    public CallHistoryEntity(T initialState)
    {
        StateHistory = new List<T> {initialState};
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
            if (StateHistory != null && StateHistory.Count > 0)
            {
                return StateHistory[0].CallId ?? throw new ArgumentNullException(nameof(StateHistory));
            }
            throw new ArgumentNullException(nameof(StateHistory));
        }
        set
        {
            // ignore
        }
    }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    [IgnoreDataMember]
    public List<T>? StateHistory { get => JsonSerializer.Deserialize<List<T>>(StateHistoryJson); set => StateHistoryJson = JsonSerializer.Serialize(value); }

    public string StateHistoryJson { get; set; } = null!;


    [IgnoreDataMember]
    public JsonElement[] NotificationsHistory 
    { 
        get => !string.IsNullOrEmpty(NotificationsHistoryJson) ? JsonSerializer.Deserialize<JsonElement[]>(NotificationsHistoryJson) ?? new JsonElement[0] : new JsonElement[0]; 
        set => NotificationsHistoryJson = JsonSerializer.Serialize(value); }

    public string NotificationsHistoryJson { get; set; } = null!;
}
