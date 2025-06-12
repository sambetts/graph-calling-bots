using GraphCallingBots.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphCallingBots.StateManagement.Cosmos;

/// <summary>
/// Something that has to go in Cosmos DB
/// </summary>
public abstract class CosmosCallDoc
{
    public string id { get => CallId; set => CallId = value; }
    public string CallId { get; set; } = string.Empty;
}


/// <summary>
/// Class to encapsulate CallHistoryEntity<T> in a Cosmos DB way. 
/// </summary>
public class CallHistoryCosmosDoc<CALLSTATETYPE> : CosmosCallDoc
    where CALLSTATETYPE : BaseActiveCallState
{
    public CallHistoryCosmosDoc() : this(null) { }
    public CallHistoryCosmosDoc(CALLSTATETYPE? callState)
    {
        CallId = callState?.CallId ?? string.Empty;
        LastUpdated = DateTime.UtcNow;
    }

    public CallStateAndNotificationsHistoryEntity<CALLSTATETYPE> CallHistory { get; set; } = null!;


    public DateTime? LastUpdated { get; set; } = null;
}


public class CallStateCosmosDoc<CALLSTATETYPE> : CosmosCallDoc
    where CALLSTATETYPE : BaseActiveCallState
{

    public CallStateCosmosDoc() : this(null) { }
    public CallStateCosmosDoc(CALLSTATETYPE? callState)
    {
        CallId = callState?.CallId ?? string.Empty;
        LastUpdated = DateTime.UtcNow;
    }
    public CALLSTATETYPE? State { get; set; }
    public DateTime? LastUpdated { get; set; } = null;
}
