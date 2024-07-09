using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;
using System.Text.Json;

namespace GroupCalls.Common;

public class CallInviteBot : PstnCallingBot<GroupCallInviteActiveCallState>
{
    public CallInviteBot(RemoteMediaCallingBotConfiguration botOptions, ICallStateManager<GroupCallInviteActiveCallState> callStateManager, ICallHistoryManager<GroupCallInviteActiveCallState, CallNotification> callHistoryManager, ILogger<GroupCallBot> logger)
        : base(botOptions, callStateManager, callHistoryManager, logger) { }

    /// <summary>
    /// Start group call with required attendees.
    /// </summary>
    internal async Task<Call?> InviteToGroupCall(StartGroupCallData meetingRequest, InvitationParticipantInfo initialAdd, Call createdGroupCall)
    {
        if (createdGroupCall == null || createdGroupCall.Id == null)
        {
            throw new ArgumentNullException(nameof(createdGroupCall));
        }

        // Work out what audio to play, if anything
        var mediaInfoItem = string.IsNullOrEmpty(meetingRequest.MessageUrl) ? null : new MediaInfo { Uri = meetingRequest.MessageUrl, ResourceId = Guid.NewGuid().ToString() };

        // Remember initial state
        await InitCallStateAndStoreMediaInfoForCreatedCall(createdGroupCall, mediaInfoItem, createdCallState => createdCallState.GroupCallId = createdGroupCall.Id);

            var newTarget = new InvitationParticipantInfo
            {
                Identity = attendee.ToIdentity()
            };

            var singleAttendeeCallReq = await CreateCallRequest(newTarget, mediaInfoItem, attendee.Type == GroupMeetingAttendeeType.Phone, false);


            // Start call
            var singleAttendeeCall = await CreateNewCall(singleAttendeeCallReq);

            if (singleAttendeeCall != null)
            {
                // Remember initial state
                await InitCallStateAndStoreMediaInfoForCreatedCall(singleAttendeeCall, mediaInfoItem, createdCallState => createdCallState.GroupCallInvites = inviteNumberList);
            }
        

        return createdGroupCall;
    }

    protected override async Task NewTonePressed(GroupCallActiveCallState callState, Tone tone)
    {
        await base.NewTonePressed(callState, tone);
        if (tone == Tone.Tone1)
        {
            await _graphServiceClient.Communications.cal(callState);
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
