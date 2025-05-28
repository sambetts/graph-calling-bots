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
    /// <param name="resourceId">Example: "/app/calls/4d1f5d00-1a60-4db8-bed0-706b16a6cf67"</param>
    Task<T?> GetByNotificationResourceUrl(string resourceId);
    Task AddCallStateOrUpdate(T callState);
    Task<bool> RemoveCurrentCall(string resourceUrl);
    Task UpdateCurrentCallState(T callState);
    Task<List<T>> GetActiveCalls();

    Task<string?> GetBotTypeNameByCallId(string callId);
    Task AddCall(string callId, string botTypeName);
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
    public string CosmosDb { get; set; }
    public string DatabaseName { get; set; }
    public string ContainerName { get; set; }
}
