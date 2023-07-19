using SimpleCallingBotEngine;

namespace Engine;

public class GroupCallActiveCallState : BaseActiveCallState
{
    /// <summary>
    /// List of invitees to the call once call is established.
    /// </summary>
    public List<string> Invites { get; set; } = new();
}
