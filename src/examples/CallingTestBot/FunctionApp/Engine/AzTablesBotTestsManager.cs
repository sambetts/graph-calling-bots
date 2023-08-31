using Azure;
using Azure.Data.Tables;
using Microsoft.Graph;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace CallingTestBot.FunctionApp.Engine;

public class AzTablesBotTestsManager : AbstractAzTablesStorageManager
{
    public AzTablesBotTestsManager(string storageConnectionString) : base(storageConnectionString)
    {
    }

    public async Task CallConnectedSuccesfully(string callId)
    {
        InitCheck(_tableClient);

        var existingCallTestLog = new TestCallState { CallId = callId, CallConnected = true };
        await _tableClient!.UpsertEntityAsync(existingCallTestLog);
    }

    public async Task CallTerminated(string callId, ResultInfo resultInfo)
    {
        InitCheck(_tableClient);

        var existingCallTestLog = new TestCallState { CallId = callId, CallTerminateCode = resultInfo.Code, CallTerminateMessage = resultInfo.Message };
        await _tableClient!.UpsertEntityAsync(existingCallTestLog);
    }

    public async Task NewCallEstablishing(string callId)
    {
        InitCheck(_tableClient);

        var newCallTestLog = new TestCallState { CallId = callId, CallConnected = false };
        await _tableClient!.UpsertEntityAsync(newCallTestLog);
    }

}

public class TestCallState : ITableEntity
{
    public const string PARTITION_KEY = "TestCallState";


    public string PartitionKey
    {
        get
        {
            return PARTITION_KEY;
        }
        set
        {
            // ignore
        }
    }

    public string RowKey
    {
        get
        {
            return CallId ?? throw new ArgumentNullException(nameof(CallId));
        }
        set
        {
            // ignore
        }
    }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string CallId { get; set; } = null!;

    public bool CallConnected { get; set; } = false;

    public int? CallTerminateCode { get; set; } = null;
    public string? CallTerminateMessage { get; set; } = null;
}
