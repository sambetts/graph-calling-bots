using Microsoft.Graph;
using System.Text.Json.Serialization;

namespace ServiceHostedMediaCallingBot.Engine.Models;

/// <summary>
/// State of a call made by the bot. Base implementation.
/// </summary>
public class BaseActiveCallState : IEquatable<BaseActiveCallState>
{
    public string ResourceUrl { get; set; } = null!;

    public CallMediaState? MediaState { get; set; } = null;

    /// <summary>
    /// Calculated from notification <see cref="ResourceUrl"/>   
    /// </summary>
    public string? CallId => GetCallId(ResourceUrl);

    /// <summary>
    /// Returns "6f1f5c00-8c1b-47f1-be9d-660c501041a9" from "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9"
    /// </summary>
    public static string? GetCallId(string resourceUrl)
    {
        if (string.IsNullOrEmpty(resourceUrl)) return null;
        var parts = resourceUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3) return null;
        if (parts[0].ToLower() != "communications" || parts[1].ToLower() != "calls") return null;
        return parts[2];
    }

    public void PopulateFromCallNotification(CallNotification fromNotification)
    {
        if (fromNotification == null) throw new ArgumentNullException(nameof(fromNotification));
        if (fromNotification.ResourceUrl == null) throw new ArgumentNullException(nameof(fromNotification.ResourceUrl));

        ResourceUrl = fromNotification.ResourceUrl;
        StateEnum = fromNotification.AssociatedCall?.State;
    }

    public bool Equals(BaseActiveCallState? other)
    {
        return other != null && other.ResourceUrl == ResourceUrl 
            && other.CallId == CallId
            && other.StateEnum == StateEnum
            && other.BotMediaPlaylist.Select(p => p.Key).SequenceEqual(BotMediaPlaylist.Select(p => p.Key))
            && other.BotMediaPlaylist.Select(p => p.Value).SequenceEqual(BotMediaPlaylist.Select(p => p.Value))
            && other.JoinedParticipants.SequenceEqual(JoinedParticipants)
            && other.TonesPressed.SequenceEqual(TonesPressed);
    }

    /// <summary>
    /// Sounds to play on call
    /// </summary>
    public Dictionary<string, CallMediaPrompt> BotMediaPlaylist { get; set; } = new();

    /// <summary>
    /// Connected, Establishing, etc
    /// </summary>
    public CallState? StateEnum { get; set; } = null;

    public List<Tone> TonesPressed { get; set; } = new();
    public List<CallMediaPrompt> MediaPromptsPlaying { get; set; } = new();

    public List<CallParticipant> JoinedParticipants { get; set; } = new();
    
    [JsonIgnore]
    public bool HasValidCallId => !string.IsNullOrEmpty(CallId);
}
