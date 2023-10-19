﻿using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using ServiceHostedMediaCallingBot.Engine.Models;
using System.Text.Json;

namespace ServiceHostedMediaCallingBot.Engine.StateManagement;

/// <summary>
/// Azure tables implementation of ICallStateManager
/// </summary>
public class AzTablesCallStateManager<T> : AbstractAzTablesStorageManager, ICallStateManager<T> where T : BaseActiveCallState
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

    public async Task<T?> GetByNotificationResourceUrl(string resourceUrl)
    {
        InitCheck();

        var callId = BaseActiveCallState.GetCallId(resourceUrl);
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

    public async Task<bool> RemoveCurrentCall(string resourceUrl)
    {
        InitCheck();
        var callId = BaseActiveCallState.GetCallId(resourceUrl);
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


    public async Task<int> GetCurrentCallCount()
    {
        InitCheck();

        // There has to be a better way of doing this...
        var r = _tableClient!.QueryAsync<CallStateEntity<T>>(f => f.PartitionKey == CallStateEntity<T>.PARTITION_KEY);

        int count = 0;
        await foreach (var result in r)
        {
            count++;
        }
        return count;
    }

    public async Task AddToCallHistory(T callState, JsonDocument graphNotificationPayload)
    {
        InitCheck();

        var r = await GetCallHistory(callState);
        if (r != null)
        {
            r.NotificationsHistory = r.NotificationsHistory.Concat(new JsonElement[1] { graphNotificationPayload.RootElement }).ToArray();
        }
        else
        {
            r = new CallHistoryEntity<T>(callState);
            r.NotificationsHistory = new JsonElement[0];
        }

        await _tableClient!.UpsertEntityAsync(r);
    }

    public async Task<CallHistoryEntity<T>?> GetCallHistory(T callState)
    {
        InitCheck();

        var r = await _tableClient!.GetEntityIfExistsAsync<CallHistoryEntity<T>>(callState.CallId, CallHistoryEntity<T>.PARTITION_KEY);
        if (r.HasValue)
        {
            return r.Value;
        }

        return null;
    }
}
