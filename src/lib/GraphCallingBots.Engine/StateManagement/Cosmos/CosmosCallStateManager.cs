using GraphCallingBots.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace GraphCallingBots.StateManagement.Cosmos;

public class CosmosCallStateManager<CALLSTATETYPE> : CosmosService<CALLSTATETYPE>, ICallStateManager<CALLSTATETYPE> where CALLSTATETYPE : BaseActiveCallState
{
    private readonly ILogger<CosmosCallStateManager<CALLSTATETYPE>> _logger;

    public override string PARTITION_KEY => "/" + nameof(CosmosCallDoc.CallId);

    public CosmosCallStateManager(CosmosClient cosmosClient, string containerName, string databaseName, ILogger<CosmosCallStateManager<CALLSTATETYPE>> logger) 
        : base(cosmosClient, containerName, databaseName)
    {
        _logger = logger;
    }

    public async Task AddCallStateOrUpdate(CALLSTATETYPE callState)
    {
        _logger.LogInformation($"Adding or updating call state for CallId: {callState.CallId}");
        var patchOperations = new List<PatchOperation>
        {
            PatchOperation.Set("/State", callState),
            PatchOperation.Set("/LastUpdated", DateTime.UtcNow)
        };

        try
        {
            await container.PatchItemAsync<CallStateCosmosDoc<CALLSTATETYPE>>(
                id: callState.CallId,
                partitionKey: new PartitionKey(callState.CallId),
                patchOperations: patchOperations
            );
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation($"Call state for CallId: {callState.CallId} not found, creating new document.");
            
            var newDoc = new CallStateCosmosDoc<CALLSTATETYPE>(callState)
            {
                State = callState,
                LastUpdated = DateTime.UtcNow
            };
            await container.UpsertItemAsync(newDoc);
            _logger.LogInformation($"Call state for CallId: {callState.CallId} created successfully.");
        }
    }

    public async Task<List<CALLSTATETYPE>> GetActiveCalls()
    {
        _logger.LogInformation("Retrieving active calls from Cosmos DB");
        var query = new QueryDefinition("SELECT * FROM c WHERE c.CallId != ''"); // Adjust the query as needed
        var iterator = container.GetItemQueryIterator<CallStateCosmosDoc<CALLSTATETYPE>>(query);
        var results = new List<CALLSTATETYPE>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response.Select(r => r.State));
        }
        return results;
    }


    public async Task<CALLSTATETYPE?> GetStateByCallId(string callId)
    {
        if (string.IsNullOrEmpty(callId))
        {
            return null;
        }
        var query = new QueryDefinition("SELECT * FROM c WHERE c.CallId = @callId")
            .WithParameter("@callId", callId);
        var iterator = container.GetItemQueryIterator<CallStateCosmosDoc<CALLSTATETYPE>>(query);
        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            return response.FirstOrDefault()?.State;
        }
        return null;
    }

    public async Task<bool> RemoveCurrentCall(string resourceUrl)
    {
        var callId = BaseActiveCallState.GetCallId(resourceUrl);
        if (callId == null)
        {
            return false;
        }
        try
        {
            await container.DeleteItemAsync<CallStateCosmosDoc<CALLSTATETYPE>>(
                id: callId,
                partitionKey: new PartitionKey(callId)
            );
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false; // Item not found
        }
    }

    public async Task RemoveAll()
    {
        _logger.LogInformation("Removing all call states from Cosmos DB");
        var query = new QueryDefinition("SELECT * FROM c WHERE c.CallId != ''"); // Adjust the query as needed
        var iterator = container.GetItemQueryIterator<CallStateCosmosDoc<CALLSTATETYPE>>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            foreach (var item in response)
            {
                await container.DeleteItemAsync<CallStateCosmosDoc<CALLSTATETYPE>>(
                    id: item.CallId,
                    partitionKey: new PartitionKey(item.CallId)
                );
            }
        }
    }
}
