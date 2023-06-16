using Microsoft.Graph;

namespace SimpleCallingBotEngine;

public interface ICallStateManager
{
    /// <param name="resourceId">Example: "/app/calls/4d1f5d00-1a60-4db8-bed0-706b16a6cf67"</param>
    /// <returns></returns>
    Task<ActiveCallState?> GetByNotificationResourceId(string resourceId);
    Task AddCallState(ActiveCallState callState);
    Task Remove(ActiveCallState callState);
    Task UpdateByResourceId(ActiveCallState callState);
}


/// <summary>
/// State of a call made by the bot.
/// </summary>
public class ActiveCallState
{
    public ActiveCallState(Call fromCall)
    {
        ResourceUrl = $"/app/calls/{fromCall.Id}";
        CallId = fromCall.Id;
        State = fromCall.State;
    }

    public string ResourceUrl { get; set; } = null!;
    public string CallId { get; set; } = null!;
    public CallState? State { get; set; } = null;
    public List<Tone> TonesPressed { get; set; } = new();
    public bool HasValidCallId => !string.IsNullOrEmpty(CallId);
}
