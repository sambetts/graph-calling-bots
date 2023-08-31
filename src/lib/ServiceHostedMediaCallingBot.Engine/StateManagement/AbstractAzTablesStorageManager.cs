﻿using Azure;
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