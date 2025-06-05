using GraphCallingBots.Models;
using Microsoft.Azure.Cosmos;

namespace GraphCallingBots.StateManagement.Cosmos;

public class CosmosCallStateManager<CALLSTATETYPE> : CosmosService<CALLSTATETYPE>, ICallStateManager<CALLSTATETYPE> where CALLSTATETYPE : BaseActiveCallState
{
    public override string PARTITION_KEY => "/" + nameof(CosmosCallDoc.CallId);

    public CosmosCallStateManager(CosmosClient cosmosClient, string containerName, string databaseName) 
        : base(cosmosClient, containerName, databaseName)
    {
    }

    public async Task AddCallStateOrUpdate(CALLSTATETYPE callState)
    {
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
            // If the document does not exist, create it
            await container.UpsertItemAsync(
                new CallStateCosmosDoc<CALLSTATETYPE>(callState)
                {
                    LastUpdated = DateTime.UtcNow
                },
                new PartitionKey(callState.CallId)
            );
        }
    }

    public async Task<List<CALLSTATETYPE>> GetActiveCalls()
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.CallId != ''"); // Adjust the query as needed
        var iterator = container.GetItemQueryIterator<CALLSTATETYPE>(query);
        var results = new List<CALLSTATETYPE>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public async Task<string?> GetBotTypeNameByCallId(string callId)
    {
        var query = new QueryDefinition("SELECT c.BotTypeName FROM c WHERE c.CallId = @callId")
            .WithParameter("@callId", callId);
        var iterator = container.GetItemQueryIterator<string>(query);
        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            return response.FirstOrDefault();
        }
        return null;
    }

    public async Task<CALLSTATETYPE?> GetByNotificationResourceUrl(string resourceUrl)
    {
        var callId = BaseActiveCallState.GetCallId(resourceUrl);

        if (callId == null)
        {
            return null;
        }
        var query = new QueryDefinition("SELECT * FROM c WHERE c.CallId = @callId")
            .WithParameter("@callId", callId);
        var iterator = container.GetItemQueryIterator<CALLSTATETYPE>(query);
        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            return response.FirstOrDefault();
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

    public Task UpdateCurrentCallState(CALLSTATETYPE callState)
    {
        throw new NotImplementedException();
    }
}
