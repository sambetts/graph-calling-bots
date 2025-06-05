using GraphCallingBots.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GraphCallingBots.StateManagement.Cosmos;



public class CosmosCallHistoryManager<CALLSTATETYPE> : CosmosService<CALLSTATETYPE>, ICallHistoryManager<CALLSTATETYPE>
    where CALLSTATETYPE : BaseActiveCallState
{
    private readonly Container _historyContainer;
    private readonly ILogger<CosmosCallHistoryManager<CALLSTATETYPE>> _logger;

    public override string PARTITION_KEY => "/" + nameof(CosmosCallDoc.CallId);

    public CosmosCallHistoryManager(CosmosClient cosmosClient, ICosmosConfig cosmosConfig, ILogger<CosmosCallHistoryManager<CALLSTATETYPE>> logger)
    : base(cosmosClient, cosmosConfig.ContainerNameCallHistory, cosmosConfig.CosmosDatabaseName)
    {
        _historyContainer = cosmosClient.GetContainer(cosmosConfig.CosmosDatabaseName, cosmosConfig.ContainerNameCallHistory);
        _logger = logger;
    }

    public async Task AddToCallHistory(CALLSTATETYPE callState, JsonElement graphNotificationPayload)
    {
        var newHistoryArray = new List<NotificationHistory> { new NotificationHistory
        {
            Payload = graphNotificationPayload.ToString(), Timestamp = DateTime.Now }
        };
        var newCallStateList = new List<CALLSTATETYPE> { callState };
        var callHistoryRecordFound = await GetCallHistory(callState);
        if (callHistoryRecordFound != null)
        {
            _logger.LogInformation($"Adding to existing call history for call {callState.CallId}");
            var callHistoryRecordExistingReplacement = new CallHistoryCosmosDoc<CALLSTATETYPE>(callState);

            if (callHistoryRecordFound.StateHistory.Count > 0 && !callHistoryRecordFound.StateHistory.Last().Equals(callState))
            {
                // Only update state if changed
                callHistoryRecordFound.StateHistory = callHistoryRecordFound.StateHistory.Concat(newCallStateList).ToList();
            }
            callHistoryRecordFound.NotificationsHistory.AddRange(newHistoryArray);
            callHistoryRecordExistingReplacement.CallHistory = callHistoryRecordFound;
            await _historyContainer.UpsertItemAsync(callHistoryRecordExistingReplacement);
        }
        else
        {
            _logger.LogInformation($"Creating new call history for call {callState.CallId}");
            var callHistoryRecordNew = new CallHistoryCosmosDoc<CALLSTATETYPE>(callState);
            callHistoryRecordNew.CallHistory = new CallStateAndNotificationsHistoryEntity<CALLSTATETYPE>
            {
                NotificationsHistory = newHistoryArray,
                StateHistory = newCallStateList,
                Timestamp = DateTime.UtcNow
            };
            await _historyContainer.UpsertItemAsync(callHistoryRecordNew);

        }
    }

    public async Task<CallStateAndNotificationsHistoryEntity<CALLSTATETYPE>?> GetCallHistory(CALLSTATETYPE callState)
    {
        CallStateAndNotificationsHistoryEntity<CALLSTATETYPE>? r = null;
        try
        {
            var result = await _historyContainer.ReadItemAsync<CallHistoryCosmosDoc<CALLSTATETYPE>>(callState.CallId, new PartitionKey(callState.CallId));
            if (result != null && result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                r = result.Resource.CallHistory;
            }
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Ignore
        }

        return r;
    }

    public async Task DeleteCallHistory(CALLSTATETYPE callState)
    {
        await _historyContainer.DeleteItemAsync<CallStateAndNotificationsHistoryEntity<CALLSTATETYPE>>(callState.CallId, new PartitionKey(callState.CallId));
    }

}
