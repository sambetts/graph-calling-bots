using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using ServiceHostedMediaCallingBot.Engine.Models;
using System.Text.Json;

namespace ServiceHostedMediaCallingBot.Engine.StateManagement;

public class AzTablesCallHistoryManager<T> : AbstractAzTablesStorageManager, ICallHistoryManager<T> where T : BaseActiveCallState
{
    public override string TableName => "CallHistory";

    public AzTablesCallHistoryManager(TableServiceClient tableServiceClient, ILogger<AzTablesCallHistoryManager<T>> logger) : base(tableServiceClient, logger)
    {
    }

    public async Task AddToCallHistory(T callState, JsonDocument graphNotificationPayload)
    {
        InitCheck();

        var newHistoryArray = new JsonElement[1] { graphNotificationPayload.RootElement };
        var newCallStateList = new List<T> { callState };
        var callHistoryRecord = await GetCallHistory(callState);
        if (callHistoryRecord != null)
        {
            _logger.LogInformation($"Adding to existing call history for call {callState.CallId}");

            if (callHistoryRecord.StateHistory.Count > 0 && !callHistoryRecord.StateHistory.Last().Equals(callState))
            {
                // Only update state if changed
                callHistoryRecord.StateHistory = callHistoryRecord.StateHistory.Concat(newCallStateList).ToList();
            }
            
            callHistoryRecord.NotificationsHistory = callHistoryRecord.NotificationsHistory.Concat(newHistoryArray).ToArray();
        }
        else
        {
            _logger.LogInformation($"Creating new call history for call {callState.CallId}");
            callHistoryRecord = new CallHistoryEntity<T>(callState);
            callHistoryRecord.NotificationsHistory = newHistoryArray;
            callHistoryRecord.StateHistory = newCallStateList;
        }

        await _tableClient!.UpsertEntityAsync(callHistoryRecord);
    }

    public async Task<CallHistoryEntity<T>?> GetCallHistory(T callState)
    {
        InitCheck();

        var r = await _tableClient!.GetEntityIfExistsAsync<CallHistoryEntity<T>>(CallHistoryEntity<T>.PARTITION_KEY, callState.CallId);
        if (r.HasValue)
        {
            _logger.LogDebug($"Found call history for call {callState.CallId}");
            return r.Value;
        }

        _logger.LogDebug($"No call history found for call {callState.CallId}");
        return null;
    }

    public async Task DeleteCallHistory(T callState)
    {
        InitCheck();

        _logger.LogInformation($"Deleting call history for call {callState.CallId}");
        await _tableClient!.DeleteEntityAsync(CallHistoryEntity<T>.PARTITION_KEY, callState.CallId);
    }
}
