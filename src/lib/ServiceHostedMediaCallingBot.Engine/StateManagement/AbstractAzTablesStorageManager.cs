using Azure.Data.Tables;

namespace ServiceHostedMediaCallingBot.Engine.StateManagement;

/// <summary>
/// Abstract base class for Azure tables storage managers. Async initialization.
/// </summary>
public abstract class AbstractAzTablesStorageManager
{
    protected readonly TableServiceClient _tableServiceClient;
    protected TableClient? _tableClient;

    public bool Initialised => _tableClient != null;

    public AbstractAzTablesStorageManager(string storageConnectionString)
    {
        _tableServiceClient = new TableServiceClient(storageConnectionString);
    }

    protected void InitCheck(TableClient? tableClient)
    {
        if (tableClient == null) throw new InvalidOperationException("Not initialized");
    }
}
