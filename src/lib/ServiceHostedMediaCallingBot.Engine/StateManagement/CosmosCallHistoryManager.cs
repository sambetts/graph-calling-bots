using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using ServiceHostedMediaCallingBot.Engine.Models;
using System.Text.Json;

namespace ServiceHostedMediaCallingBot.Engine.StateManagement;

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
public class CallHistoryCosmosDoc<T> : StatsCosmosDoc where T : BaseActiveCallState
{
    public CallHistoryCosmosDoc() : this(null) { }
    public CallHistoryCosmosDoc(T? callState) 
    { 
        this.CallId = callState?.CallId ?? string.Empty;
        this.LastUpdated = DateTime.UtcNow; 
    }

    public string CallId { get; set; }
    public CallHistoryEntity<T> CallHistory { get; set; } = null!;

    public override string id { get => CallId; set => CallId = value; }

    public DateTime? LastUpdated { get; set; } = null;
}


public class CosmosCallHistoryManager<T> : ICallHistoryManager<T> where T : BaseActiveCallState
{
    private bool _initialised = false;
    private static string PARTITION_KEY = "/" + nameof(CallHistoryCosmosDoc<T>.CallId);
    private readonly Container _historyContainer;
    private readonly CosmosClient _cosmosClient;
    private readonly ICosmosConfig _cosmosConfig;
    private readonly ILogger<CosmosCallHistoryManager<T>> _logger;

    public bool Initialised => _initialised;

    public CosmosCallHistoryManager(CosmosClient cosmosClient, ICosmosConfig cosmosConfig, ILogger<CosmosCallHistoryManager<T>> logger)
    {
        _historyContainer = cosmosClient.GetContainer(cosmosConfig.DatabaseName, cosmosConfig.ContainerName);
        _cosmosClient = cosmosClient;
        _cosmosConfig = cosmosConfig;
        _logger = logger;
    }

    public async Task AddToCallHistory(T callState, object graphNotificationPayload)
    {
        var newHistoryArray = new List<NotificationHistory> { new NotificationHistory { Payload = graphNotificationPayload, Timestamp = DateTime.Now } };
        var newCallStateList = new List<T> { callState };
        var callHistoryRecordFound = await GetCallHistory(callState);
        if (callHistoryRecordFound != null)
        {
            _logger.LogInformation($"Adding to existing call history for call {callState.CallId}");
            var callHistoryRecordExistingReplacement = new CallHistoryCosmosDoc<T>(callState);

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
            var callHistoryRecordNew = new CallHistoryCosmosDoc<T>(callState);
            callHistoryRecordNew.CallHistory = new CallHistoryEntity<T> 
            { 
                NotificationsHistory = newHistoryArray, 
                StateHistory = newCallStateList, 
                Timestamp = DateTime.UtcNow 
            };
            await _historyContainer.UpsertItemAsync(callHistoryRecordNew);

        }
    }

    public async Task<CallHistoryEntity<T>?> GetCallHistory(T callState)
    {
        CallHistoryEntity<T>? r = null;
        try
        {
            var result = await _historyContainer.ReadItemAsync<CallHistoryCosmosDoc<T>>(callState.CallId, new PartitionKey(callState.CallId));
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

    public async Task DeleteCallHistory(T callState)
    {
        await _historyContainer.DeleteItemAsync<CallHistoryEntity<T>>(callState.CallId, new PartitionKey(callState.CallId));
    }

    public async Task Initialise()
    {
        await _cosmosClient.CreateDatabaseIfNotExistsAsync(_cosmosConfig.DatabaseName);
        var db = _cosmosClient.GetDatabase(_cosmosConfig.DatabaseName);
        await db.CreateContainerIfNotExistsAsync(id: _cosmosConfig.ContainerName, partitionKeyPath: PARTITION_KEY);
        _initialised = true;
    }
}
