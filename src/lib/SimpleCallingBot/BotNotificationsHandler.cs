using Microsoft.Graph;
using Microsoft.Extensions.Logging;
using SimpleCallingBotEngine;
using SimpleCallingBotEngine.Models;

namespace SimpleCallingBotEngineEngine;

/// <summary>
/// Turns Graph notifications into callbacks
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

    public async Task HandleNotificationsAsync(CommsNotificationsPayload notificationPayload)
    {
        foreach (var callnotification in notificationPayload.CommsNotifications)
        {
            var updateCall = false;
            var callState = await _callStateManager.GetByNotificationResourceId(callnotification.ResourceUrl);

            if (callState != null && callState.HasValidCallId)
            {
                // Is this notification for a call we're tracking?
                if (callnotification.AssociatedCall?.CallChainId != null)
                {
                    // Update call state
                    callState.State = callnotification.AssociatedCall.State;
                    updateCall = true;
                    await HandleCallNotificationAsync(callnotification, callState);

                }
                else if (callnotification.AssociatedCall?.ToneInfo != null)
                {
                    updateCall = true;
                    await HandleToneNotificationAsync(callnotification.AssociatedCall.ToneInfo, callState);
                }

                if (updateCall)
                {
                    await _callStateManager.UpdateByResourceId(callState);
                }
            }
            else
            {
                _logger.LogWarning($"Received notification for unknown call {callnotification.ResourceUrl}");
            }
        }
    }

    async Task HandleToneNotificationAsync(ToneInfo toneInfo, ActiveCallState callState)
    {
        if (toneInfo.Tone != null)
        {
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

    async Task HandleCallNotificationAsync(CallNotification callnotification, ActiveCallState callState)
    {
        if (callnotification.ChangeType == CallConstants.NOTIFICATION_TYPE_UPDATED && callnotification.AssociatedCall?.State == CallState.Established)
        {
            _logger.LogInformation($"Call {callState.CallId} connected");
            if (_callbackInfo.CallConnected != null) await _callbackInfo.CallConnected(callState);
        }
    }
}


public class NotificationCallbackInfo
{
    public Func<ActiveCallState, Task>? CallConnected { get; set; }
    public Func<ActiveCallState, Tone, Task>? NewTonePressed { get; set; }
}
