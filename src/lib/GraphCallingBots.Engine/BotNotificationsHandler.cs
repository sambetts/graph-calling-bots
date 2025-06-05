using GraphCallingBots.CallingBots;
using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using System.Text.Json;

namespace GraphCallingBots;

/// <summary>
/// Turns Graph call notifications into callbacks and updates base call state & history.
/// </summary>
public class BotNotificationsHandler<CALLSTATETYPE>() where CALLSTATETYPE : BaseActiveCallState, new()
{
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public record NotificationStats 
    {
        public int Processed { get; set; }
        public int Skipped { get; set; }
    }

    /// <summary>
    /// Handle notifications from Graph and raise events as appropriate
    /// </summary>
    public static async Task<NotificationStats> HandleNotificationsAndUpdateCallStateAsync(CommsNotificationsPayload? notificationPayload, string botTypeName, ICallStateManager<CALLSTATETYPE> callStateManager,
    ICallHistoryManager<CALLSTATETYPE> callHistoryManager, NotificationCallbackInfo<CALLSTATETYPE> callbackInfo, ILogger logger)
    {
        if (notificationPayload == null) return new NotificationStats { Processed = 0, Skipped = 0 };

        // Ensure processing is single-threaded to maintain processing order
        await _semaphore.WaitAsync();

        if (!callStateManager.Initialised) await callStateManager.Initialise();
        if (!callHistoryManager.Initialised) await callHistoryManager.Initialise();

        var stats = new NotificationStats { Processed = 0, Skipped = 0 };
        foreach (var callnotification in notificationPayload.CommsNotifications)
        {
            var notificationInScope = false;
            var callState = await callStateManager.GetStateByCallId(callnotification.ResourceUrl);
            var updateCallState = false;

            // Is this notification for a call we're tracking?
            updateCallState = await HandleCallChangeTypeUpdate(callStateManager, callbackInfo, callState, callnotification, botTypeName, logger);
            if (updateCallState)
            {
                notificationInScope = true;
            }
            // If we're not updating the call state, check for other events
            if (!updateCallState && callState != null)
            {
                // More call events
                if (callnotification.AssociatedCall?.ToneInfo != null)
                {
                    // Is this notification for a tone on a call we're tracking?
                    updateCallState = true;
                    await HandleToneNotificationAsync(callbackInfo, callnotification.AssociatedCall.ToneInfo, callState, botTypeName, logger);
                }
                else if (callnotification.AssociatedPlayPromptOperation != null && callnotification.AssociatedPlayPromptOperation.Status == OperationStatus.Completed)
                {
                    // Tone finished playing
                    logger.LogInformation($"{botTypeName}: Call {callState.CallId} finished playing tone");
                    var playingTone = callState.MediaPromptsPlaying.Where(p => p.MediaInfo != null && p.MediaInfo.ResourceId == callnotification.AssociatedPlayPromptOperation.Id);
                    if (playingTone.Any())
                    {
                        callState.MediaPromptsPlaying.Remove(playingTone.First());
                        updateCallState = true;
                    }
                    if (callbackInfo.PlayPromptFinished != null) await callbackInfo.PlayPromptFinished(callState);
                }
                else if (callnotification.JoinedParticipants != null)
                {
                    var newPartipants = callnotification.JoinedParticipants.GetJoinedParticipants(callState.JoinedParticipants);
                    if (newPartipants.Count > 0)
                    {
                        // User joined group call
                        logger.LogInformation($"{botTypeName}: {newPartipants.Count} user(s) joined group call {callState.CallId}");
                        if (callbackInfo.UsersJoinedGroupCall != null) await callbackInfo.UsersJoinedGroupCall(callState, newPartipants);
                    }

                    var diconnectedPartipants = callnotification.JoinedParticipants.GetDisconnectedParticipants(callState.JoinedParticipants);
                    if (diconnectedPartipants.Count > 0)
                    {
                        // User left group call
                        logger.LogInformation($"{botTypeName}: {diconnectedPartipants.Count} user(s) left group call {callState.CallId}");
                        if (callbackInfo.UsersLeftGroupCall != null) await callbackInfo.UsersLeftGroupCall(callState, diconnectedPartipants);
                    }

                    callState.JoinedParticipants = callnotification.JoinedParticipants;
                    updateCallState = true;
                }
            }
            // Processing ended. Update?
            if (updateCallState && callState != null)
            {
                logger.LogInformation($"Updated call state for call {callState.CallId}");
                await callStateManager.AddCallStateOrUpdate(callState);
            }

            if (notificationInScope)
            {
                stats.Processed++;
            }
            else
            {
                stats.Skipped++;
                logger.LogDebug($"{botTypeName}: Skipped notification for call '{callState?.CallId ?? "unknown"}' - no state change or not in scope");
            }

            // Update history even if no state changes
            if (callState != null)
            {
                await callHistoryManager.AddToCallHistory(callState, JsonDocument.Parse(JsonSerializer.Serialize(callnotification)).RootElement);
                logger.LogInformation($"Updated call history for call {callState.CallId}");
            }
        }

        _semaphore.Release();

        return stats;
    }

    private static async Task<bool> HandleCallChangeTypeUpdate(ICallStateManager<CALLSTATETYPE> callStateManager, NotificationCallbackInfo<CALLSTATETYPE> callbackInfo, 
        CALLSTATETYPE? callState, CallNotification callNotification, string botType, ILogger logger)
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
                logger.LogInformation($"{botType}: Created call state for call {callState.CallId} (from Graph notification)");
                await callStateManager.AddCallStateOrUpdate(callState);
            }
            if (callbackInfo.CallEstablishing != null) await callbackInfo.CallEstablishing(callState);

            logger.LogInformation($"{botType}: Call {callState.CallId} is connecting");

            // Update call-state
            callState.StateEnum = callNotification.AssociatedCall.State;
            return true;
        }

        if (callState == null)
        {
            // A notification about a call we know nothing about
            logger.LogWarning($"{botType}: Received notification for call we have no call-state for: '{callNotification?.ResourceUrl}'");
            return false;
        }

        if (callNotification.ChangeType == CallConstants.NOTIFICATION_TYPE_UPDATED)
        {
            var updateCallState = false;

            // An update happened to the call. Check for call state changes
            if (callNotification.AssociatedCall != null && callState.StateEnum != callNotification.AssociatedCall.State && callNotification.AssociatedCall.State == CallState.Established)
            {
                // Call state changed to established from previous state
                logger.LogInformation($"{botType}: Call {callState.CallId} established");
                callState.StateEnum = callNotification.AssociatedCall.State;
                if (callbackInfo.CallEstablished != null) await callbackInfo.CallEstablished(callState);

                // Update call state
                updateCallState = true;
            }
            if (callNotification.AssociatedCall?.MediaState.IsConnected() == true && callState.MediaState == null)
            {
                // Audio is now active. THREADING FUN:
                // We can be here before we've set the call state to established above if the second notification arrives before we save state on the "call established" notification
                logger.LogInformation($"{botType}: Call {callState.CallId} connected with P2P audio");
                if (callbackInfo.CallConnectedWithP2PAudio != null) await callbackInfo.CallConnectedWithP2PAudio(callState);

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
                logger.LogInformation($"{botType}: Call {callState.CallId} terminated");
                var removeSuccess = await callStateManager.RemoveCurrentCall(callState.ResourceUrl);
                if (removeSuccess)
                    logger.LogInformation($"{botType}: Call {callState.CallId} state removed");
                else
                    logger.LogWarning($"{botType}: Call {callState.CallId} state could not be removed");

                if (callbackInfo.CallTerminated != null && callNotification.AssociatedCall?.ResultInfo != null)
                    await callbackInfo.CallTerminated(callState.CallId, callNotification.AssociatedCall.ResultInfo);
                else
                {
                    if (callNotification.AssociatedCall?.ResultInfo == null)
                        logger.LogWarning($"{botType}: Call {callState.CallId} terminated with no result info");
                }
            }
            else
            {
                logger.LogWarning($"{botType}: Unknown call finished");
            }
        }

        return false;
    }

    static async Task HandleToneNotificationAsync(NotificationCallbackInfo<CALLSTATETYPE> callbackInfo, ToneInfo toneInfo, CALLSTATETYPE callState, string botType, ILogger logger)
    {
        if (toneInfo.Tone != null)
        {
            logger.LogTrace($"{botType}: Received tone {toneInfo.Tone.Value} on call {callState.CallId}");
            callState.TonesPressed.Add(toneInfo.Tone.Value);

            if (callbackInfo.NewTonePressed != null)
            {
                try
                {
                    await callbackInfo.NewTonePressed(callState, toneInfo.Tone.Value);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"{botType}: Error processing tone {toneInfo.Tone.Value} on call {callState.CallId} - {ex.Message}");
                }
            }
        }
        else
        {
            logger.LogWarning($"{botType}: Received notification for unknown tone on call {callState.CallId}");
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
