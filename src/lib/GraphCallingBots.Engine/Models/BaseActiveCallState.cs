﻿using Microsoft.Graph.Models;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace GraphCallingBots.Models;

public record NotificationStats
{
    public int Processed { get; set; }
    public int Skipped { get; set; }
}

/// <summary>
/// State of a call made by the bot. Base implementation.
/// </summary>
public class BaseActiveCallState : IEquatable<BaseActiveCallState>
{
    /// <summary>
    /// Name of the bot class that is handling this call.
    /// </summary>
    public string BotClassNameFull { get; set; } = null!;
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

    /// <summary>
    /// Normally the data we get is a resource URL like "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9" so most call ID parsing is done on that. 
    /// But sometimes we just have the call ID directly without a resource URL, so we fake it here.
    /// </summary>
    public static string GetResourceUrlFromCallId(string callId)
    {
        if (string.IsNullOrEmpty(callId)) throw new ArgumentNullException(nameof(callId));
        return $"/communications/calls/{callId}";
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
        if (other == null)
            return false;

        bool mediaPromptsEqual = (MediaPromptsPlaying == null && other.MediaPromptsPlaying == null)
            || (MediaPromptsPlaying != null && other.MediaPromptsPlaying != null
                && MediaPromptsPlaying.SequenceEqual(other.MediaPromptsPlaying));

        bool botMediaPlaylistEqual = (BotMediaPlaylist == null && other.BotMediaPlaylist == null)
            || (BotMediaPlaylist != null && other.BotMediaPlaylist != null
                && BotMediaPlaylist.Select(p => p.Key).SequenceEqual(other.BotMediaPlaylist.Select(p => p.Key))
                && BotMediaPlaylist.Select(p => p.Value).SequenceEqual(other.BotMediaPlaylist.Select(p => p.Value)));

        return other.ResourceUrl == ResourceUrl
            && other.BotClassNameFull == BotClassNameFull
            && other.CallId == CallId
            && other.StateEnum == StateEnum
            && botMediaPlaylistEqual
            && other.JoinedParticipants.SequenceEqual(JoinedParticipants)
            && other.TonesPressed != null && other.TonesPressed.SequenceEqual(TonesPressed ?? new List<Tone>())
            && mediaPromptsEqual;
    }

    /// <summary>
    /// Sounds to play on call
    /// </summary>
    public Dictionary<string, EquatableMediaPrompt>? BotMediaPlaylist { get; set; } = null;

    /// <summary>
    /// Connected, Establishing, etc
    /// </summary>
    public CallState? StateEnum { get; set; } = null;

    public List<Tone>? TonesPressed { get; set; } = new();
    public List<MediaPrompt>? MediaPromptsPlaying { get; set; } = null;

    public List<CallParticipant> JoinedParticipants { get; set; } = new();

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasValidCallId => !string.IsNullOrEmpty(CallId);

    public void AddToBotMediaPlaylist(string key, EquatableMediaPrompt prompt)
    {
        if (BotMediaPlaylist == null)
            BotMediaPlaylist = new Dictionary<string, EquatableMediaPrompt>();
        BotMediaPlaylist.Add(key, prompt);
    }

    public static void EnsureBotMediaPlaylist(BaseActiveCallState callState, string key, EquatableMediaPrompt prompt)
    {
        if (callState.BotMediaPlaylist != null && callState.BotMediaPlaylist.ContainsKey(key))
        {
            return;
        }

        if (callState.BotMediaPlaylist == null)
            callState.BotMediaPlaylist = new Dictionary<string, EquatableMediaPrompt>();
        callState.BotMediaPlaylist.Add(key, prompt);
    }
}
