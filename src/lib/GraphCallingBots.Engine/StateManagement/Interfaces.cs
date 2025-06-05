using GraphCallingBots.Models;
using System.Text.Json;

namespace GraphCallingBots.StateManagement;

/// <summary>
/// A service that needs to be initialised async before use.
/// </summary>
public interface IAsyncInit
{
    Task Initialise();
    bool Initialised { get; }
}

/// <summary>
/// Manages the state of calls made by the bot in Graph.
/// </summary>
public interface ICallStateManager<T> : IAsyncInit where T : BaseActiveCallState
{
    Task<T?> GetStateByCallId(string callId);
    Task AddCallStateOrUpdate(T callState);
    Task<bool> RemoveCurrentCall(string resourceUrl);
    Task<List<T>> GetActiveCalls();
}

/// <summary>
/// Manages saving the history of calls made by the bot in Graph. Used for debugging normally.
/// </summary>
public interface ICallHistoryManager<CALLSTATETYPE> : IAsyncInit
    where CALLSTATETYPE : BaseActiveCallState
{
    Task AddToCallHistory(CALLSTATETYPE callState, JsonElement graphNotificationPayload);
    Task<CallStateAndNotificationsHistoryEntity<CALLSTATETYPE>?> GetCallHistory(CALLSTATETYPE callState);

    Task DeleteCallHistory(CALLSTATETYPE callState);
}

public interface ICosmosConfig
{
    public string CosmosConnectionString { get; set; }
    public string CosmosDatabaseName { get; set; }
    public string ContainerNameCallHistory { get; set; }
    public string ContainerNameCallState { get; set; }
}
