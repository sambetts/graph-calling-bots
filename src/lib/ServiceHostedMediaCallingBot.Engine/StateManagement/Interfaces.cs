using ServiceHostedMediaCallingBot.Engine.Models;
using System.Text.Json;

namespace ServiceHostedMediaCallingBot.Engine.StateManagement;

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
    Task<int> GetCurrentCallCount();
}

public interface ICallHistoryManager<T> : IAsyncInit where T : BaseActiveCallState
{
    Task AddToCallHistory(T callState, JsonDocument graphNotificationPayload);
    Task<CallHistoryEntity<T>?> GetCallHistory(T callState);
}
