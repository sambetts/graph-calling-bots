using Azure.Data.Tables;
using GraphCallingBots.Models;
using Microsoft.Extensions.Logging;

namespace GraphCallingBots.StateManagement;

/// <summary>
/// Azure tables implementation of ICallStateManager
/// </summary>
public class AzTablesCallStateManager<T> : AbstractSingleTableAzStorageManager, ICallStateManager<T> where T : BaseActiveCallState
{
    public override string TableName => "CallState";

    public AzTablesCallStateManager(TableServiceClient tableServiceClient, ILogger<AzTablesCallStateManager<T>> logger) : base(tableServiceClient, logger)
    {
    }

    public async Task AddCallStateOrUpdate(T callState)
    {
        InitCheck();

        var entity = new CallStateEntity<T>(callState);
        var r = await _tableClient!.UpsertEntityAsync(entity);
    }

    public async Task<T?> GetStateByCallId(string callId)
    {
        InitCheck();

        if (callId != null)
        {
            var results = _tableClient!.QueryAsync<CallStateEntity<T>>(f => f.RowKey == callId);
            await foreach (var result in results)
            {
                return result.State;
            }
        }

        return null;
    }

    public async Task<bool> RemoveCurrentCall(string callId)
    {
        InitCheck();

        var r = await _tableClient!.DeleteEntityAsync(CallStateEntity<T>.PARTITION_KEY, callId);
        return !r.IsError;
    }

    public async Task RemoveAll()
    {
        InitCheck();

        var r = _tableClient!.QueryAsync<CallStateEntity<T>>(f => f.PartitionKey == CallStateEntity<T>.PARTITION_KEY);

        await foreach (var result in r)
        {
            await _tableClient!.DeleteEntityAsync(result.PartitionKey, result.RowKey);
        }
    }

    public async Task UpdateCurrentCallState(T callState)
    {
        // Uses Upsert so will update if exists, or insert if not
        await AddCallStateOrUpdate(callState);
    }

    public async Task<List<T>> GetActiveCalls()
    {
        InitCheck();

        var list = new List<T>();
        var r = _tableClient!.QueryAsync<CallStateEntity<T>>(f => f.PartitionKey == CallStateEntity<T>.PARTITION_KEY);

        await foreach (var result in r)
        {
            if (result.State != null)
            {
                list.Add(result.State);
            }
        }
        return list;
    }

    public async Task<string?> GetBotTypeNameByCallId(string callId)
    {
        InitCheck();

        var results = _tableClient!.QueryAsync<CallStateEntity<T>>(f => f.RowKey == callId);
        await foreach (var result in results)
        {
            return result.State?.BotClassNameFull;
        }
        return null;
    }
}
