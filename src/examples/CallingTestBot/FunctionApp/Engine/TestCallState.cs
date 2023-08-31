using Azure.Data.Tables;
using Azure;

namespace CallingTestBot.FunctionApp.Engine;


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
