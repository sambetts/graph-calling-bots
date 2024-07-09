using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace GroupCalls.Common;

public class CallInviteBot : PstnCallingBot<GroupCallInviteActiveCallState>
{
    public CallInviteBot(RemoteMediaCallingBotConfiguration botOptions, ICallStateManager<GroupCallInviteActiveCallState> callStateManager, ICallHistoryManager<GroupCallInviteActiveCallState, CallNotification> callHistoryManager, ILogger<GroupCallBot> logger)
        : base(botOptions, callStateManager, callHistoryManager, logger) { }

    internal async Task<Call?> CallCandidateForGroupCall(StartGroupCallData meetingRequest, Call createdGroupCall)
    {
        if (createdGroupCall == null || createdGroupCall.Id == null)
        {
            throw new ArgumentNullException(nameof(createdGroupCall));
        }

        // Work out what audio to play, if anything
        var mediaInfoItem = string.IsNullOrEmpty(meetingRequest.MessageUrl) ? null : new MediaInfo { Uri = meetingRequest.MessageUrl, ResourceId = Guid.NewGuid().ToString() };

        // Remember initial state
        await InitCallStateAndStoreMediaInfoForCreatedCall(createdGroupCall, mediaInfoItem, createdCallState => createdCallState.GroupCallId = createdGroupCall.Id);

        var (initialAdd, inviteNumberList) = meetingRequest.GetInitialParticipantsAndInvites();
        var newTarget = new InvitationParticipantInfo
        {
            Identity = initialAdd.Identity
        };

        var singleAttendeeCallReq = await CreateCallRequest(newTarget, mediaInfoItem, meetingRequest.HasPSTN, false);


        // Start call
        var singleAttendeeCall = await CreateNewCall(singleAttendeeCallReq);

        if (singleAttendeeCall != null)
        {
            // Remember initial state
            await InitCallStateAndStoreMediaInfoForCreatedCall(singleAttendeeCall, mediaInfoItem, 
                createdCallState => createdCallState.GroupCallId = createdGroupCall.Id);
        }


        return createdGroupCall;
    }

    protected async override Task NewTonePressed(GroupCallInviteActiveCallState callState, Tone tone)
    {
        await base.NewTonePressed(callState, tone);
        if (tone == Tone.Tone1)
        {
            // Transfer
            await _graphServiceClient.Communications.Calls[callState.GroupCallId].Transfer.PostAsync(callState);
        }
    }

    protected async override Task UsersJoinedGroupCall(GroupCallInviteActiveCallState callState, List<CallParticipant> participants)
    {
        await base.UsersJoinedGroupCall(callState, participants);
        await PlayConfiguredMediaIfNotAlreadyPlaying(callState);
    }

    protected async override Task CallConnectedWithP2PAudio(GroupCallInviteActiveCallState callState)
    {
        await base.CallConnectedWithP2PAudio(callState);
        await PlayConfiguredMediaIfNotAlreadyPlaying(callState);
    }
}
