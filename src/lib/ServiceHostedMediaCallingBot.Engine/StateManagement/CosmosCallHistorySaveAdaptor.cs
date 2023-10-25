using Microsoft.Azure.Cosmos;
using Microsoft.Graph.Communications.Common;
using System.Text.Json.Serialization;

namespace ServiceHostedMediaCallingBot.Engine.StateManagement;

/// <summary>
/// Something that has to go in Cosmos DB
/// </summary>
public abstract class StatsCosmosDoc
{
    public abstract string id { get; set; }
}

/// <summary>
/// Stats for this solution runtime. Anonimised. 
/// </summary>
public class AnonUsageStatsModel : StatsCosmosDoc
{
    public AnonUsageStatsModel() { }

    public string AnonClientId { get; set; } = null;

    public override string id { set => AnonClientId = value; get => AnonClientId; } // For Cosmos PK

    /// <summary>
    /// Amount of records retrieved thanks to AI calls to Azure
    /// </summary>
    public int? DataPointsFromAITotal { get; set; } = null;
    public string ConfiguredSolutionsEnabledDescription { get; set; } = null;
    public string ConfiguredImportsEnabledDescription { get; set; } = null;
    public List<TableStat> TableStats { get; set; } = null;
    public string BuildVersionLabel { get; set; } = null;

    public DateTime? Generated { get; set; } = null;

    [JsonIgnore] public bool IsValid => !string.IsNullOrEmpty(AnonClientId) && Generated != null && Generated.Value > DateTime.MinValue;


    public class TableStat
    {
        public string TableName { get; set; }
        public decimal TotalSpaceMB { get; set; }
        public Int64 Rows { get; set; }

        public override string ToString()
        {
            return $"{TableName}: {Rows} rows, {TotalSpaceMB}mb";
        }
    }

    /// <summary>
    /// Update this with whatever an update has
    /// </summary>
    public AnonUsageStatsModel UpdateWith(AnonUsageStatsModel updateFromClientWithNewId)
    {
        if (updateFromClientWithNewId != null && updateFromClientWithNewId.IsValid)
        {
            if (updateFromClientWithNewId.TableStats != null)
            {
                this.TableStats = new List<TableStat>(updateFromClientWithNewId.TableStats);
            }

            if (!string.IsNullOrEmpty(updateFromClientWithNewId.ConfiguredImportsEnabledDescription))
            {
                this.ConfiguredImportsEnabledDescription = updateFromClientWithNewId.ConfiguredImportsEnabledDescription;
            }
            if (!string.IsNullOrEmpty(updateFromClientWithNewId.ConfiguredSolutionsEnabledDescription))
            {
                this.ConfiguredSolutionsEnabledDescription = updateFromClientWithNewId.ConfiguredSolutionsEnabledDescription;
            }
            if (!string.IsNullOrEmpty(updateFromClientWithNewId.BuildVersionLabel))
            {
                this.BuildVersionLabel = updateFromClientWithNewId.BuildVersionLabel;
            }
            if (updateFromClientWithNewId.DataPointsFromAITotal.HasValue)
            {
                this.DataPointsFromAITotal = updateFromClientWithNewId.DataPointsFromAITotal;
            }

            this.Generated = updateFromClientWithNewId.Generated;
        }

        return this;
    }

    public override string ToString()
    {
        return $"AnonClientId: {AnonClientId}, Generated: {Generated}, DataPointsFromAITotal: {DataPointsFromAITotal}, ConfiguredSolutionsEnabledDescription: {ConfiguredSolutionsEnabledDescription}, ConfiguredImportsEnabledDescription: {ConfiguredImportsEnabledDescription}, TableStats: {TableStats}, BuildVersionLabel: {BuildVersionLabel}";
    }

}


public class CosmosTelemetrySaveAdaptor 
{
    private static string PARTITION_KEY = "/" + nameof(AnonUsageStatsModel.AnonClientId);
    private readonly Container _historyStatsContainer;
    private readonly Container _currentStatsContainer;
    private readonly CosmosClient _cosmosClient;
    private readonly IStatsServiceCosmosConfig _webAppConfig;

    public CosmosTelemetrySaveAdaptor(CosmosClient cosmosClient, IStatsServiceCosmosConfig webAppConfig)
    {
        _historyStatsContainer = cosmosClient.GetContainer(webAppConfig.DatabaseName, webAppConfig.ContainerNameHistory);
        _currentStatsContainer = cosmosClient.GetContainer(webAppConfig.DatabaseName, webAppConfig.ContainerNameCurrent);
        _cosmosClient = cosmosClient;
        _webAppConfig = webAppConfig;
    }

    public async Task<AnonUsageStatsModel> LoadCurrentRecordByClientId(AnonUsageStatsModel model)
    {
        AnonUsageStatsModel r = null;
        try
        {
            var result = await _currentStatsContainer.ReadItemAsync<AnonUsageStatsModel>(model.AnonClientId, new PartitionKey(model.AnonClientId));
            if (result != null && result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                r = result.Resource;
            }
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Ignore
        }

        return r;
    }
    public async Task SaveOrUpdate(AnonUsageStatsModel model)
    {
        var historicalUpdate = new HistoricalUpdate(model);
        await _historyStatsContainer.UpsertItemAsync(historicalUpdate);
        await _currentStatsContainer.UpsertItemAsync(model);
    }


    public async Task Init()
    {
        await _cosmosClient.CreateDatabaseIfNotExistsAsync(_webAppConfig.DatabaseName);
        var db = _cosmosClient.GetDatabase(_webAppConfig.DatabaseName);
        await db.CreateContainerIfNotExistsAsync(id: _webAppConfig.ContainerNameHistory, partitionKeyPath: PARTITION_KEY);
        await db.CreateContainerIfNotExistsAsync(id: _webAppConfig.ContainerNameCurrent, partitionKeyPath: PARTITION_KEY);
    }
}