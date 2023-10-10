using ServiceHostedMediaCallingBot.Engine.Models;

namespace ServiceHostedMediaCallingBot.Engine.StateManagement;

/// <summary>
/// Manages the state of calls made by the bot in Graph.
/// </summary>
public interface ICallStateManager<T> where T : BaseActiveCallState
{
    Task Initialise();
    bool Initialised { get; }

    /// <param name="resourceId">Example: "/app/calls/4d1f5d00-1a60-4db8-bed0-706b16a6cf67"</param>
    Task<T?> GetByNotificationResourceUrl(string resourceId);
    Task AddCallState(T callState);
    Task<bool> Remove(string resourceUrl);
    Task Update(T callState);
    Task<int> GetCount();
}
