using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphCallingBots.EventQueue;


public interface IJsonQueueAdapter<T>
{
    Task EnqueueAsync(T payload);
    Task<T?> DequeueAsync(CancellationToken cancellationToken = default);
}

public interface IJsonClassWithOriginalContent
{
    /// <summary>
    /// Original content of the JSON message, if available.
    /// </summary>
    string? OriginalContent { get; set; }
}
