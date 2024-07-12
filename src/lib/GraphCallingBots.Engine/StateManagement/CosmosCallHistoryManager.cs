using GraphCallingBots.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace GraphCallingBots.StateManagement;

/// <summary>
/// Something that has to go in Cosmos DB
/// </summary>
public abstract class StatsCosmosDoc
{
    public abstract string id { get; set; }
}

/// <summary>
/// Class to encapsulate CallHistoryEntity<T> in a Cosmos DB way. 
/// </summary>
public class CallHistoryCosmosDoc<CALLSTATETYPE, HISTORYPAYLOADTYPE> : StatsCosmosDoc
    where CALLSTATETYPE : BaseActiveCallState
    where HISTORYPAYLOADTYPE : class
{
    public CallHistoryCosmosDoc() : this(null) { }
    public CallHistoryCosmosDoc(CALLSTATETYPE? callState)
    {
        CallId = callState?.CallId ?? string.Empty;
        LastUpdated = DateTime.UtcNow;
    }

    public string CallId { get; set; }
    public CallStateAndNotificationsHistoryEntity<CALLSTATETYPE, HISTORYPAYLOADTYPE> CallHistory { get; set; } = null!;

    public override string id { get => CallId; set => CallId = value; }

    public DateTime? LastUpdated { get; set; } = null;
}


public class CosmosCallHistoryManager<CALLSTATETYPE, HISTORYPAYLOADTYPE> : ICallHistoryManager<CALLSTATETYPE, HISTORYPAYLOADTYPE>
    where CALLSTATETYPE : BaseActiveCallState
    where HISTORYPAYLOADTYPE : class
{
    private bool _initialised = false;
    private static string PARTITION_KEY = "/" + nameof(CallHistoryCosmosDoc<CALLSTATETYPE, HISTORYPAYLOADTYPE>.CallId);
    private readonly Container _historyContainer;
    private readonly CosmosClient _cosmosClient;
    private readonly ICosmosConfig _cosmosConfig;
    private readonly ILogger<CosmosCallHistoryManager<CALLSTATETYPE, HISTORYPAYLOADTYPE>> _logger;

    public bool Initialised => _initialised;

    public CosmosCallHistoryManager(CosmosClient cosmosClient, ICosmosConfig cosmosConfig, ILogger<CosmosCallHistoryManager<CALLSTATETYPE, HISTORYPAYLOADTYPE>> logger)
    {
        _historyContainer = cosmosClient.GetContainer(cosmosConfig.DatabaseName, cosmosConfig.ContainerName);
        _cosmosClient = cosmosClient;
        _cosmosConfig = cosmosConfig;
        _logger = logger;
    }

    public async Task AddToCallHistory(CALLSTATETYPE callState, HISTORYPAYLOADTYPE graphNotificationPayload)
    {
        var newHistoryArray = new List<NotificationHistory<HISTORYPAYLOADTYPE>> { new NotificationHistory<HISTORYPAYLOADTYPE>
        {
            Payload = graphNotificationPayload, Timestamp = DateTime.Now }
        };
        var newCallStateList = new List<CALLSTATETYPE> { callState };
        var callHistoryRecordFound = await GetCallHistory(callState);
        if (callHistoryRecordFound != null)
        {
            _logger.LogInformation($"Adding to existing call history for call {callState.CallId}");
            var callHistoryRecordExistingReplacement = new CallHistoryCosmosDoc<CALLSTATETYPE, HISTORYPAYLOADTYPE>(callState);

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
            var callHistoryRecordNew = new CallHistoryCosmosDoc<CALLSTATETYPE, HISTORYPAYLOADTYPE>(callState);
            callHistoryRecordNew.CallHistory = new CallStateAndNotificationsHistoryEntity<CALLSTATETYPE, HISTORYPAYLOADTYPE>
            {
                NotificationsHistory = newHistoryArray,
                StateHistory = newCallStateList,
                Timestamp = DateTime.UtcNow
            };
            await _historyContainer.UpsertItemAsync(callHistoryRecordNew);

        }
    }

    public async Task<CallStateAndNotificationsHistoryEntity<CALLSTATETYPE, HISTORYPAYLOADTYPE>?> GetCallHistory(CALLSTATETYPE callState)
    {
        CallStateAndNotificationsHistoryEntity<CALLSTATETYPE, HISTORYPAYLOADTYPE>? r = null;
        try
        {
            var result = await _historyContainer.ReadItemAsync<CallHistoryCosmosDoc<CALLSTATETYPE, HISTORYPAYLOADTYPE>>(callState.CallId, new PartitionKey(callState.CallId));
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
        await _historyContainer.DeleteItemAsync<CallStateAndNotificationsHistoryEntity<CALLSTATETYPE, HISTORYPAYLOADTYPE>>(callState.CallId, new PartitionKey(callState.CallId));
    }

    public async Task Initialise()
    {
        await _cosmosClient.CreateDatabaseIfNotExistsAsync(_cosmosConfig.DatabaseName);
        var db = _cosmosClient.GetDatabase(_cosmosConfig.DatabaseName);
        await db.CreateContainerIfNotExistsAsync(id: _cosmosConfig.ContainerName, partitionKeyPath: PARTITION_KEY);
        _initialised = true;
    }
}
