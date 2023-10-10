using Microsoft.Graph;

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
        return other != null && other.ResourceUrl == ResourceUrl && other.CallId == CallId;
    }

    /// <summary>
    /// Connected, Establishing, etc
    /// </summary>
    public CallState? StateEnum { get; set; } = null;

    public List<Tone> TonesPressed { get; set; } = new();
    public List<MediaPrompt> MediaPromptsPlaying { get; set; } = new();

    public List<Participant>? JoinedParticipants { get; set; }
    public bool HasValidCallId => !string.IsNullOrEmpty(CallId);
}
