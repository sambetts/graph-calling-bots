using GraphCallingBots.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace GraphCallingBots.StateManagement.Cosmos;

public abstract class CosmosService<CALLSTATETYPE> : IAsyncInit where CALLSTATETYPE : BaseActiveCallState
{

    private bool _initialised = false;
    public abstract string PARTITION_KEY { get; }
    public string ContainerName { get; set; }
    public string DatabaseName { get; set;  }

    public bool Initialised => _initialised;

    protected readonly Container container;
    protected readonly CosmosClient _cosmosClient;

    public CosmosService(CosmosClient cosmosClient, string containerName, string databaseName)
    {
        _cosmosClient = cosmosClient;
        ContainerName = containerName;
        DatabaseName = databaseName;
        container = cosmosClient.GetContainer(DatabaseName, ContainerName);
    }
    public async Task Initialise()
    {
        await _cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseName);
        var db = _cosmosClient.GetDatabase(DatabaseName);
        await db.CreateContainerIfNotExistsAsync(id: ContainerName, partitionKeyPath: PARTITION_KEY);
        _initialised = true;
    }
}
