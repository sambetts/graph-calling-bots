using Newtonsoft.Json.Linq;
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
    Task<List<T>> GetActiveCalls();
}

public interface ICallHistoryManager<CALLSTATETYPE, HISTORYPAYLOADTYPE> : IAsyncInit 
    where CALLSTATETYPE : BaseActiveCallState
    where HISTORYPAYLOADTYPE : class
{
    Task AddToCallHistory(CALLSTATETYPE callState, HISTORYPAYLOADTYPE graphNotificationPayload);
    Task<CallHistoryEntity<CALLSTATETYPE, HISTORYPAYLOADTYPE>?> GetCallHistory(CALLSTATETYPE callState);

    Task DeleteCallHistory(CALLSTATETYPE callState);
}

public interface ICosmosConfig
{
    public string CosmosDb { get; set; }
    public string DatabaseName { get; set; }
    public string ContainerName { get; set; }
}