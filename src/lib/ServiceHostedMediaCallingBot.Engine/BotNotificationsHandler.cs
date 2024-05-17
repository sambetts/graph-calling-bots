using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace ServiceHostedMediaCallingBot.Engine;

/// <summary>
/// Turns Graph call notifications into callbacks and updates base call state & history.
/// </summary>
public class BotNotificationsHandler<CALLSTATETYPE>
    where CALLSTATETYPE : BaseActiveCallState, new()
{
    private readonly ILogger _logger;
    private readonly ICallStateManager<CALLSTATETYPE> _callStateManager;
    private readonly ICallHistoryManager<CALLSTATETYPE, CallNotification> _callHistoryManager;
    private readonly NotificationCallbackInfo<CALLSTATETYPE> _callbackInfo;

    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public BotNotificationsHandler(ICallStateManager<CALLSTATETYPE> callStateManager, ICallHistoryManager<CALLSTATETYPE, CallNotification> callHistoryManager, NotificationCallbackInfo<CALLSTATETYPE> callbackInfo, ILogger logger)
    {
        _logger = logger;
        _callStateManager = callStateManager;
        _callHistoryManager = callHistoryManager;
        _callbackInfo = callbackInfo;
    }

    /// <summary>
    /// Handle notifications from Graph and raise events as appropriate
    /// </summary>
    public async Task HandleNotificationsAndUpdateCallStateAsync(CommsNotificationsPayload? notificationPayload)
    {
        if (notificationPayload == null) return;

        // Ensure processing is single-threaded to maintain processing order
        await _semaphore.WaitAsync();

        if (!_callStateManager.Initialised) await _callStateManager.Initialise();
        if (!_callHistoryManager.Initialised) await _callHistoryManager.Initialise();

        foreach (var callnotification in notificationPayload.CommsNotifications)
        {
            var callState = await _callStateManager.GetByNotificationResourceUrl(callnotification.ResourceUrl);
            var updateCallState = false;

            // Is this notification for a call we're tracking?
            updateCallState = await HandleCallChangeTypeUpdate(callState, callnotification);

            // If we're not updating the call state, check for other events
            if (!updateCallState && callState != null)
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
                    var playingTone = callState.MediaPromptsPlaying.Where(p => p.MediaInfo != null && p.MediaInfo.ResourceId == callnotification.AssociatedPlayPromptOperation.Id);
                    if (playingTone.Any())
                    {
                        callState.MediaPromptsPlaying.Remove(playingTone.First());
                        updateCallState = true;
                    }
                    if (_callbackInfo.PlayPromptFinished != null) await _callbackInfo.PlayPromptFinished(callState);
                }
                else if (callnotification.JoinedParticipants != null)
                {
                    var newPartipants = callnotification.JoinedParticipants.GetJoinedParticipants(callState.JoinedParticipants);
                    if (newPartipants.Count > 0)
                    {
                        // User joined group call
                        _logger.LogInformation($"{newPartipants.Count} user(s) joined group call {callState.CallId}");
                        if (_callbackInfo.UsersJoinedGroupCall != null) await _callbackInfo.UsersJoinedGroupCall(callState, newPartipants);
                    }

                    var diconnectedPartipants = callnotification.JoinedParticipants.GetDisconnectedParticipants(callState.JoinedParticipants);
                    if (diconnectedPartipants.Count > 0)
                    {
                        // User left group call
                        _logger.LogInformation($"{diconnectedPartipants.Count} user(s) left group call {callState.CallId}");
                        if (_callbackInfo.UsersLeftGroupCall != null) await _callbackInfo.UsersLeftGroupCall(callState, diconnectedPartipants);
                    }

                    callState.JoinedParticipants = callnotification.JoinedParticipants;
                    updateCallState = true;
                }
            }
            // Processing ended. Update?
            if (updateCallState && callState != null)
            {
                await _callStateManager.UpdateCurrentCallState(callState);
            }

            // Update history even if no state changes
            if (callState != null)
            {
                await _callHistoryManager.AddToCallHistory(callState, callnotification);
            }
        }

        _semaphore.Release();
    }

    private async Task<bool> HandleCallChangeTypeUpdate(CALLSTATETYPE? callState, CallNotification callNotification)
    {
        // Not seen this call before. Is this notification for a new call?
        if (callNotification.AssociatedCall != null && callNotification.AssociatedCall.State == CallState.Establishing)
        {
            // Add to state manager if not already there
            var newCallStateCreated = false;
            if (callState == null)
            {
                newCallStateCreated = true;
                callState = new CALLSTATETYPE();
            }
            callState.PopulateFromCallNotification(callNotification);

            if (newCallStateCreated)
            {
                // Normally we should have an existing state but just in case....
                _logger.LogInformation($"Created call state for call {callState.CallId} (from Graph notification)");
                await _callStateManager.AddCallStateOrUpdate(callState);
            }
            if (_callbackInfo.CallEstablishing != null) await _callbackInfo.CallEstablishing(callState);

            _logger.LogInformation($"Call {callState.CallId} is connecting");

            // Update call-state
            callState.StateEnum = callNotification.AssociatedCall.State;
            return true;
        }

        if (callState == null)
        {
            // A notification about a call we know nothing about
            _logger.LogWarning($"Received notification for call we have no call-state for: '{callNotification?.ResourceUrl}'");
            return false;
        }

        if (callNotification.ChangeType == CallConstants.NOTIFICATION_TYPE_UPDATED)
        {
            var updateCallState = false;

            // An update happened to the call. Check for call state changes
            if (callNotification.AssociatedCall != null && callState.StateEnum != callNotification.AssociatedCall.State && callNotification.AssociatedCall.State == CallState.Established)
            {
                // Call state changed to established from previous state
                _logger.LogInformation($"Call {callState.CallId} established");
                callState.StateEnum = callNotification.AssociatedCall.State;
                if (_callbackInfo.CallEstablished != null) await _callbackInfo.CallEstablished(callState);

                // Update call state
                updateCallState = true;
            }
            if (callNotification.AssociatedCall?.MediaState.IsConnected() == true && callState.MediaState == null)
            {
                // Audio is now active. THREADING FUN:
                // We can be here before we've set the call state to established above if the second notification arrives before we save state on the "call established" notification
                _logger.LogInformation($"Call {callState.CallId} connected with P2P audio");
                if (_callbackInfo.CallConnectedWithP2PAudio != null) await _callbackInfo.CallConnectedWithP2PAudio(callState);

                callState.MediaState = callNotification.AssociatedCall.MediaState;
                updateCallState = true;
            }
            return updateCallState;
        }
        else if (callNotification.ChangeType == CallConstants.NOTIFICATION_TYPE_DELETED && callNotification.ResourceUrl == callState.ResourceUrl)
        {
            // Hang up and remove state
            if (!string.IsNullOrEmpty(callState.CallId))
            {
                _logger.LogInformation($"Call {callState.CallId} terminated");
                var removeSuccess = await _callStateManager.RemoveCurrentCall(callState.ResourceUrl);
                if (removeSuccess)
                    _logger.LogInformation($"Call {callState.CallId} state removed");
                else
                    _logger.LogWarning($"Call {callState.CallId} state could not be removed");

                if (_callbackInfo.CallTerminated != null && callNotification.AssociatedCall?.ResultInfo != null)
                    await _callbackInfo.CallTerminated(callState.CallId, callNotification.AssociatedCall.ResultInfo);
                else
                {
                    if (callNotification.AssociatedCall?.ResultInfo == null)
                        _logger.LogWarning($"Call {callState.CallId} terminated with no result info");
                }
            }
            else
            {
                _logger.LogWarning($"Unknown call finished");
            }
        }

        return false;
    }

    async Task HandleToneNotificationAsync(ToneInfo toneInfo, CALLSTATETYPE callState)
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

public class NotificationCallbackInfo<T> where T : BaseActiveCallState, new()
{
    public Func<T, Task>? CallConnectedWithP2PAudio { get; set; }
    public Func<T, Task>? CallEstablishing { get; set; }
    public Func<T, Task>? CallEstablished { get; set; }
    public Func<T, Task>? PlayPromptFinished { get; set; }
    public Func<string, ResultInfo, Task>? CallTerminated { get; set; }
    public Func<T, Tone, Task>? NewTonePressed { get; set; }

    public Func<T, List<CallParticipant>, Task>? UsersJoinedGroupCall { get; set; }
    public Func<T, List<CallParticipant>, Task>? UsersLeftGroupCall { get; set; }
}