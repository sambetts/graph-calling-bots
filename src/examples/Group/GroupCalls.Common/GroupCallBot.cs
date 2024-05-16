using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GroupCalls.Common;

/// <summary>
/// A bot that starts a call with a bunch of people, internal and external.
/// </summary>
public class GroupCallBot : PstnCallingBot<GroupCallActiveCallState>
{
    public GroupCallBot(RemoteMediaCallingBotConfiguration botOptions, ICallStateManager<GroupCallActiveCallState> callStateManager, ICallHistoryManager<GroupCallActiveCallState, CallNotification> callHistoryManager, ILogger<GroupCallBot> logger)
        : base(botOptions, callStateManager, callHistoryManager, logger) { }

    /// <summary>
    /// Start group call with required attendees.
    /// </summary>
    public async Task<Call?> StartGroupCall(StartGroupCallData meetingRequest)
    {
        MediaInfo? mediaInfoItem = string.IsNullOrEmpty(meetingRequest.MessageUrl) ? null : new MediaInfo { Uri = meetingRequest.MessageUrl, ResourceId = Guid.NewGuid().ToString() };

        // Work out who to call first & who to invite
        var (initialAdd, inviteNumberList) = meetingRequest.GetInitialParticipantsAndInvites();

        // Create call for initial participants. Will throw error if media URL is invalid
        var newCallDetails = await TestCallMediaAndCreateCallRequest(initialAdd, mediaInfoItem, meetingRequest.HasPSTN);

        // Add meeting info if this is a Teams meeting
        if (meetingRequest.JoinMeetingInfo?.JoinUrl != null)
        {
            var (chatInfo, joinInfo) = JoinInfo.ParseJoinURL(meetingRequest.JoinMeetingInfo.JoinUrl);
            newCallDetails.MeetingInfo = joinInfo;
            newCallDetails.ChatInfo = chatInfo;

            // If this is a call for an OnlineMeeting, we can't initially call someone with the new call as the call is technically already in progress.
            // So we need to invite them instead.
            inviteNumberList.Add(initialAdd);
        }

        // Start call
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        var json = JsonSerializer.Serialize(newCallDetails, options);
        var createdCall = await CreateNewCall(newCallDetails);

        if (createdCall != null)
        {
            // Remember initial state
            await InitCallStateAndStoreMediaInfoForCreatedCall(createdCall, mediaInfoItem, createdCallState => createdCallState.GroupCallInvites = inviteNumberList);
        }
        return createdCall;
    }

    /// <summary>
    /// Due to how group calls work with PSTN numbers especially, we need to invite everyone else after the call is established.
    /// </summary>
    protected async override Task CallEstablished(GroupCallActiveCallState callState)
    {
        if (!string.IsNullOrEmpty(callState?.CallId))
        {
            // Invite everyone else
            if (callState.GroupCallInvites != null && callState.GroupCallInvites.Count > 0)
            {
                await InviteToCallAsync(callState.CallId, callState.GroupCallInvites);

                callState.GroupCallInvites.Clear();
                await _callStateManager.UpdateCurrentCallState(callState);
            }
            else
            {
                _logger.LogInformation("Call established but no invites found");
            }
        }
        else
        {
            _logger.LogWarning("Call established but no call ID found");
        }
    }

    protected override async Task UsersJoinedGroupCall(GroupCallActiveCallState callState, List<CallParticipant> participants)
    {
        await base.UsersJoinedGroupCall(callState, participants);
        await PlayConfiguredMediaIfNotAlreadyPlaying(callState);
    }

    protected async override Task CallConnectedWithP2PAudio(GroupCallActiveCallState callState)
    {
        await base.CallConnectedWithP2PAudio(callState);
        await PlayConfiguredMediaIfNotAlreadyPlaying(callState);
    }
}
