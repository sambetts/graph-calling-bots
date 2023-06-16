﻿using Microsoft.Graph;
using SimpleCallingBotEngine.Models;

namespace SimpleCallingBotEngine;

public interface ICallStateManager
{
    /// <param name="resourceId">Example: "/app/calls/4d1f5d00-1a60-4db8-bed0-706b16a6cf67"</param>
    /// <returns></returns>
    Task<ActiveCallState?> GetByNotificationResourceUrl(string resourceId);
    Task AddCallState(ActiveCallState callState);
    Task<bool> Remove(string resourceUrl);
    Task Update(ActiveCallState callState);
}


/// <summary>
/// State of a call made by the bot.
/// </summary>
public class ActiveCallState
{
    public ActiveCallState()
    {
    }
    public ActiveCallState(CallNotification fromNotification) : this()
    {
        if (fromNotification == null) throw new ArgumentNullException(nameof(fromNotification));
        if (fromNotification.ResourceUrl == null) throw new ArgumentNullException(nameof(fromNotification.ResourceUrl));

        ResourceUrl = fromNotification.ResourceUrl;
        State = fromNotification.AssociatedCall?.State;
    }

    public string ResourceUrl { get; set; } = null!;

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

    public CallState? State { get; set; } = null;
    public List<Tone> TonesPressed { get; set; } = new();
    public bool HasValidCallId => !string.IsNullOrEmpty(CallId);
}
