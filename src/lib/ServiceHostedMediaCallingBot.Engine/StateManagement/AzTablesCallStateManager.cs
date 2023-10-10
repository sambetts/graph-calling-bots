using Azure;
using Azure.Data.Tables;
using ServiceHostedMediaCallingBot.Engine.Models;
using System.Runtime.Serialization;
using System.Text.Json;

namespace ServiceHostedMediaCallingBot.Engine.StateManagement;

/// <summary>
/// Azure tables implementation of ICallStateManager
/// </summary>
public class AzTablesCallStateManager<T> : AbstractAzTablesStorageManager, ICallStateManager<T> where T : BaseActiveCallState
{
    public override string TableName => "CallState";

    public AzTablesCallStateManager(string storageConnectionString) : base(storageConnectionString)
    {
    }

    public async Task AddCallState(T callState)
    {
        InitCheck(_tableClient);

        var entity = new TableCallState(callState);
        await _tableClient!.UpsertEntityAsync(entity);
    }

    public async Task<T?> GetByNotificationResourceUrl(string resourceUrl)
    {
        InitCheck(_tableClient);

        var callId = BaseActiveCallState.GetCallId(resourceUrl);
        if (callId != null)
        {
            var results = _tableClient!.QueryAsync<TableCallState>(f => f.RowKey == callId);
            await foreach (var result in results)
            {
                return result.State;
            }
        }

        return null;
    }

    public async Task<bool> Remove(string resourceUrl)
    {
        InitCheck(_tableClient);
        var callId = BaseActiveCallState.GetCallId(resourceUrl);
        var r = await _tableClient!.DeleteEntityAsync(TableCallState.PARTITION_KEY, callId);
        return !r.IsError;
    }

    public async Task RemoveAll()
    {
        InitCheck(_tableClient);

        var r = _tableClient!.QueryAsync<TableCallState>(f => f.PartitionKey == TableCallState.PARTITION_KEY);

        await foreach (var result in r)
        {
            await _tableClient!.DeleteEntityAsync(result.PartitionKey, result.RowKey);
        }
    }

    public async Task Update(T callState)
    {
        // Uses Upsert so will update if exists, or insert if not
        await AddCallState(callState);
    }


    public async Task<int> GetCount()
    {
        InitCheck(_tableClient);

        // There has to be a better way of doing this...
        var r = _tableClient!.QueryAsync<TableCallState>(f=> f.PartitionKey == TableCallState.PARTITION_KEY);

        int count = 0;
        await foreach (var result in r)
        {
            count++;
        }
        return count;
    }

    public class TableCallState : ITableEntity
    {
        public const string PARTITION_KEY = "CallState";

        public TableCallState()
        {
        }
        public TableCallState(T state)
        {
            State = state;
        }

        public string PartitionKey
        {
            get
            {
                return PARTITION_KEY;
            }
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
}
