using Azure;
using Azure.Data.Tables;

namespace CallingTestBot.FunctionApp.Engine;

/// <summary>
/// Logging entity for test calls
/// </summary>
public class TestCallState : ITableEntity
{
    #region ITableEntity Properties

    /// <summary>
    /// Partition on date
    /// </summary>
    public string PartitionKey
    {
        get
        {
            return Timestamp.HasValue ? Timestamp.Value.DateTime.Date.ToString("dd-MM-yyyy") : "All";
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

    #endregion

    public string CallId { get; set; } = null!;

    public bool CallConnectedOk { get; set; } = false;

    public string NumberCalled { get; set; } = null!;
    public int? CallTerminateCode { get; set; } = null;
    public string? CallTerminateMessage { get; set; } = null;
}
