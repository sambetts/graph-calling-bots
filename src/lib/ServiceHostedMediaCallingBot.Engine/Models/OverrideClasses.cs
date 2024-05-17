using Microsoft.Graph.Models;
using System.Text.Json;

namespace ServiceHostedMediaCallingBot.Engine.Models;

/// <summary>
/// To implement IEqutable, we need to override the default implementation of these classes
/// </summary>

public class EquatableMediaPrompt : MediaPrompt, IEquatable<EquatableMediaPrompt>
{
    public bool Equals(EquatableMediaPrompt? other)
    {
        return other != null && JsonSerializer.Serialize(other) == JsonSerializer.Serialize(this);
    }
}

public class CallParticipant : Participant, IEquatable<CallParticipant>
{
    public bool Equals(CallParticipant? other)
    {
        return other != null && JsonSerializer.Serialize(other) == JsonSerializer.Serialize(this);
    }
}
