using GraphCallingBots.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System.Dynamic;
using System.Text.Json;

namespace GraphCallingBots.StateManagement.Cosmos;

public class CosmosCallStateManager<CALLSTATETYPE> : CosmosService<CALLSTATETYPE>, ICallStateManager<CALLSTATETYPE> 
    where CALLSTATETYPE : BaseActiveCallState
{
    private readonly ILogger<CosmosCallStateManager<CALLSTATETYPE>> _logger;

    public override string PARTITION_KEY => "/" + nameof(CosmosCallDoc.CallId);

    public CosmosCallStateManager(CosmosClient cosmosClient, ICosmosConfig cosmosConfig, ILogger<CosmosCallStateManager<CALLSTATETYPE>> logger) 
        : base(cosmosClient, cosmosConfig.ContainerNameCallState, cosmosConfig.CosmosDatabaseName) 
    {
        _logger = logger;
    }

    public async Task AddCallStateOrUpdate(CALLSTATETYPE callState)
    {
        _logger.LogWarning($"Adding or updating call state for CallId: {callState.CallId}...");

        var updatedStatePatch = BuildDynamicNonNullProperties(callState);

        _logger.LogInformation(JsonSerializer.Serialize(updatedStatePatch, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }));




        var patchOperations = new List<PatchOperation>
        {
            PatchOperation.Set("/State", updatedStatePatch),
            PatchOperation.Set("/LastUpdated", DateTime.UtcNow)
        };

        try
        {
            await container.PatchItemAsync<CallStateCosmosDoc<CALLSTATETYPE>>(
                id: callState.CallId,
                partitionKey: new PartitionKey(callState.CallId),
                patchOperations: patchOperations
            );
            _logger.LogDebug($"Call state for CallId: {callState.CallId} updated successfully.");
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {            
            var newDoc = new CallStateCosmosDoc<CALLSTATETYPE>(callState)
            {
                State = callState,
                LastUpdated = DateTime.UtcNow
            };
            var res = await container.UpsertItemAsync(newDoc);
            _logger.LogDebug($"Call state for CallId: {callState.CallId} created successfully.");
        }

        var newState = await GetStateByCallId(callState.CallId);
        _logger.LogWarning($"Call state for CallId: {callState.CallId} after update:");
        _logger.LogInformation(JsonSerializer.Serialize(callState, new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }));

    }

    public async Task<List<CALLSTATETYPE>> GetActiveCalls()
    {
        _logger.LogTrace("Retrieving active calls from Cosmos DB");
        var query = new QueryDefinition("SELECT * FROM c WHERE c.CallId != ''"); // Adjust the query as needed
        var iterator = container.GetItemQueryIterator<CallStateCosmosDoc<CALLSTATETYPE>>(query);
        var results = new List<CALLSTATETYPE>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response.Select(r => r.State).Where(state => state != null)!);
        }
        return results;
    }


    public async Task<CALLSTATETYPE?> GetStateByCallId(string callId)
    {
        if (string.IsNullOrEmpty(callId))
        {
            return null;
        }

        _logger.LogTrace($"Retrieving call state for CallId: {callId} from Cosmos DB");
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

    public async Task<bool> RemoveCurrentCall(string callId)
    {
        if (callId == null)
        {
            return false;
        }

        _logger.LogTrace($"Removing call state for CallId: {callId} from Cosmos DB");
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
        _logger.LogTrace("Removing all call states from Cosmos DB");
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
    private static object BuildDynamicNonNullProperties(object obj)
    {
        if (obj == null) return new { };

        var type = obj.GetType();
        var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var dict = new Dictionary<string, object?>();

        foreach (var prop in properties)
        {
            var value = prop.GetValue(obj);
            if (value != null)
            {
                dict[prop.Name] = value;
            }
        }

        // Return as an ExpandoObject for dynamic usage
        IDictionary<string, object?> expando = new System.Dynamic.ExpandoObject();
        foreach (var kvp in dict)
        {
            expando[kvp.Key] = kvp.Value;
        }
        return (ExpandoObject)expando;
    }
}
