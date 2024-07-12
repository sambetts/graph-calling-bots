using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;

namespace GraphCallingBots.StateManagement;

/// <summary>
/// Abstract base class for Azure tables storage managers. Async initialization.
/// </summary>
public abstract class AbstractAzTablesStorageManager
{
    protected readonly TableServiceClient _tableServiceClient;
    protected readonly ILogger _logger;
    protected TableClient? _tableClient = null;

    public bool Initialised => _tableClient != null;

    public AbstractAzTablesStorageManager(TableServiceClient tableServiceClient, ILogger logger)
    {
        _tableServiceClient = tableServiceClient;
        _logger = logger;
    }

    protected void InitCheck()
    {
        if (_tableClient == null) throw new InvalidOperationException($"{nameof(TableClient)} not initialized for read/write operations");
    }

    public abstract string TableName { get; }

    public async Task Initialise()
    {
        try
        {
            await _tableServiceClient.CreateTableIfNotExistsAsync(TableName);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "TableAlreadyExists")
        {
            // Supposedly CreateTableIfNotExistsAsync should silently fail if already exists, but this doesn't seem to happen
        }

        _tableClient = _tableServiceClient.GetTableClient(TableName);
    }
}
