using Microsoft.Graph;
using Microsoft.Extensions.Logging;
using SimpleCallingBotEngine;
using SimpleCallingBotEngine.Models;

namespace SimpleCallingBotEngineEngine;

/// <summary>
/// Turns Graph call notifications into callbacks
/// </summary>
public class BotNotificationsHandler
{
    private readonly ILogger _logger;
    private readonly ICallStateManager _callStateManager;
    private readonly NotificationCallbackInfo _callbackInfo;

    public BotNotificationsHandler(ICallStateManager callStateManager, NotificationCallbackInfo callbackInfo, ILogger logger)
    {
        _logger = logger;
        _callStateManager = callStateManager;
        _callbackInfo = callbackInfo;
    }

    /// <summary>
    /// Handle notifications from Graph and raise events as appropriate
    /// </summary>
    public async Task HandleNotificationsAsync(CommsNotificationsPayload? notificationPayload)
    {
        if (notificationPayload == null) return;

        foreach (var callnotification in notificationPayload.CommsNotifications)
        {
            var callState = await _callStateManager.GetByNotificationResourceUrl(callnotification.ResourceUrl);

            if (callState != null && callState.HasValidCallId)
            {
                var updateCallState = false;

                // Is this notification for a call we're tracking?
                updateCallState = await HandleCallObjectUpdate(callState, callnotification);

                // If we're not updating the call state, check for other events
                if (!updateCallState)
                {
                    // More call events
                    if (callnotification.AssociatedCall?.ToneInfo != null)
                    {
                        // Is this notification for a tone on a call we're tracking?
                        updateCallState = true;
                        await HandleToneNotificationAsync(callnotification.AssociatedCall.ToneInfo, callState);
                    }
                    else if (callnotification.AssociatedPlayPromptOperation != null && callnotification.AssociatedPlayPromptOperation.Status == OperationStatus.Completed)
                    {
                        // Tone finished playing
                        _logger.LogInformation($"Call {callState.CallId} finished playing tone");
                        var playingTone = callState.MediaPromptsPlaying.Where(p=> p.MediaInfo.ResourceId == callnotification.AssociatedPlayPromptOperation.Id);
                        if (playingTone.Any())
                        {
                            callState.MediaPromptsPlaying.Remove(playingTone.First());
                            updateCallState = true;
                        }
                        if (_callbackInfo.PlayPromptFinished != null) await _callbackInfo.PlayPromptFinished(callState);
                    }
                    else if (callnotification.JoinedParticipants != null)
                    {
                        if (_callbackInfo.UserJoined != null) await _callbackInfo.UserJoined(callState);
                    }
                }
                // Processing ended. Update?
                if (updateCallState)
                {
                    await _callStateManager.Update(callState);
                }
            }
            else
            {
                // Not seen this call before. Is this notification for a new call?
                if (callnotification.AssociatedCall != null && callnotification.AssociatedCall.State == CallState.Establishing)
                {
                    // Remember the call ID for later
                    var newCallState = new ActiveCallState(callnotification);
                    await _callStateManager.AddCallState(newCallState);

                    _logger.LogWarning($"Call {newCallState.CallId} is connecting");
                }
                else
                {
                    _logger.LogWarning($"Received notification for unknown call {callnotification.ResourceUrl}");
                }
            }
        }
    }

    private async Task<bool> HandleCallObjectUpdate(ActiveCallState callState, CallNotification callNotification)
    {
        if (callNotification.ChangeType == CallConstants.NOTIFICATION_TYPE_UPDATED)
        {
            // An update happened to the call
            if (callNotification.AssociatedCall != null && callState.StateEnum != callNotification.AssociatedCall.State && callNotification.AssociatedCall.State == CallState.Established)
            {
                // Call state changed to established from previous state
                _logger.LogInformation($"Call {callState.CallId} established");

                callState.StateEnum = callNotification.AssociatedCall.State;
                if (_callbackInfo.CallEstablished != null) await _callbackInfo.CallEstablished(callState);

                // Update call state
                return true;
            }
            else if (callNotification.AssociatedCall?.MediaState != null && callNotification.AssociatedCall.MediaState.Audio.HasValue && callNotification.AssociatedCall.MediaState.Audio.Value == MediaState.Active)
            {
                // No change in call state - but audio is now active
                _logger.LogInformation($"Call {callState.CallId} connected with audio");
                if (_callbackInfo.CallConnectedWithP2PAudio != null) await _callbackInfo.CallConnectedWithP2PAudio(callState);
            }
        }
        else if (callNotification.ChangeType == CallConstants.NOTIFICATION_TYPE_DELETED && callNotification.ResourceUrl == callState.ResourceUrl)
        {
            // Hang up and remove state
            if (!string.IsNullOrEmpty(callState.CallId))
            {
                _logger.LogInformation($"Call {callState.CallId} terminated");
                await _callStateManager.Remove(callState.ResourceUrl);
                if (_callbackInfo.CallTerminated != null) await _callbackInfo.CallTerminated(callState.CallId);
            }
            else
            {
                _logger.LogWarning($"Unkown call finished");
            }
        }

        return false;
    }

    async Task HandleToneNotificationAsync(ToneInfo toneInfo, ActiveCallState callState)
    {
        if (toneInfo.Tone != null)
        {
            _logger.LogTrace($"Received tone {toneInfo.Tone.Value} on call {callState.CallId}");
            callState.TonesPressed.Add(toneInfo.Tone.Value);

            if (_callbackInfo.NewTonePressed != null)
            {
                await _callbackInfo.NewTonePressed(callState, toneInfo.Tone.Value);
            }
        }
        else
        {
            _logger.LogWarning($"Received notification for unknown tone on call {callState.CallId}");
        }
    }
}

public class NotificationCallbackInfo
{
    public Func<ActiveCallState, Task>? CallConnectedWithP2PAudio { get; set; }
    public Func<ActiveCallState, Task>? CallEstablished { get; set; }
    public Func<ActiveCallState, Task>? PlayPromptFinished { get; set; }
    public Func<string, Task>? CallTerminated { get; set; }
    public Func<ActiveCallState, Tone, Task>? NewTonePressed { get; set; }

    public Func<ActiveCallState, Task>? UserJoined { get; set; }
}
