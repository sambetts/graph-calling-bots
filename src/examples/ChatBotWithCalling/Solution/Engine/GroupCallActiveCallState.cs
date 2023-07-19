using SimpleCallingBotEngine;

namespace Engine;

public class GroupCallActiveCallState : ActiveCallState
{
    public List<string> Invites { get; set; } = new();
}
