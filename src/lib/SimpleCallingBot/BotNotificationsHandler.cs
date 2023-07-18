using Microsoft.Graph;
using Microsoft.Extensions.Logging;
using SimpleCallingBotEngine;
using SimpleCallingBotEngine.Models;
using System.Reflection;

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
    /// <param name="notificationPayload"></param>
    public async Task HandleNotificationsAsync(CommsNotificationsPayload? notificationPayload)
    {
        if (notificationPayload == null) return;

        foreach (var callnotification in notificationPayload.CommsNotifications)
        {
            var updateCall = false;
            var callState = await _callStateManager.GetByNotificationResourceUrl(callnotification.ResourceUrl);

            if (callState != null && callState.HasValidCallId)
            {
                // Is this notification for a call we're tracking?
                if (callnotification.AssociatedCall != null)
                {
                    if (callnotification.ChangeType == CallConstants.NOTIFICATION_TYPE_UPDATED)
                    {
                        // An update happened to the call
                        if (callState.State != callnotification.AssociatedCall.State && callnotification.AssociatedCall.State == CallState.Established)
                        {
                            // Call state changed to established from previous state
                            _logger.LogInformation($"Call {callState.CallId} established");

                            // Update call state
                            updateCall = true;
                            callState.State = callnotification.AssociatedCall.State;
                        }
                        else if (callnotification.AssociatedCall.MediaState != null && callnotification.AssociatedCall.MediaState.Audio.HasValue && callnotification.AssociatedCall.MediaState.Audio.Value == MediaState.Active)
                        {
                            // No change in call state - but audio is now active
                            _logger.LogInformation($"Call {callState.CallId} connected with audio");
                            if (_callbackInfo.CallConnectedWithAudio != null) await _callbackInfo.CallConnectedWithAudio(callState);
                        }
                    }
                    else if (callnotification.ChangeType == CallConstants.NOTIFICATION_TYPE_DELETED && callnotification.AssociatedCall.State == CallState.Terminated)
                    {
                        // Hang up and remove state
                        if (!string.IsNullOrEmpty(callState.CallId))
                        {
                            _logger.LogInformation($"Call {callState.CallId} finished");
                            await _callStateManager.Remove(callState.ResourceUrl);
                            if (_callbackInfo.CallTerminated != null) await _callbackInfo.CallTerminated(callState.CallId);
                        }
                        else
                        {
                            _logger.LogWarning($"Unkown call finished");
                        }
                    }
                }


                if (callnotification.AssociatedCall?.ToneInfo != null)
                {
                    // Is this notification for a tone on a call we're tracking?
                    updateCall = true;
                    await HandleToneNotificationAsync(callnotification.AssociatedCall.ToneInfo, callState);
                }
                else if (callnotification.AssociatedPlayPromptOperation != null && callnotification.AssociatedPlayPromptOperation.Status == OperationStatus.Completed)
                {
                    if (_callbackInfo.PlayPromptFinished != null) await _callbackInfo.PlayPromptFinished(callState);
                }

                if (updateCall)
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
    public Func<ActiveCallState, Task>? CallConnectedWithAudio { get; set; }
    public Func<ActiveCallState, Task>? PlayPromptFinished { get; set; }
    public Func<string, Task>? CallTerminated { get; set; }
    public Func<ActiveCallState, Tone, Task>? NewTonePressed { get; set; }
}
