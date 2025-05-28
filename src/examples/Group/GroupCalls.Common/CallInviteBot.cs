using GraphCallingBots;
using GraphCallingBots.CallingBots;
using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Communications.Calls.Item.Participants.Invite;
using Microsoft.Graph.Models;

namespace GroupCalls.Common;

/// <summary>
/// Bot that invites a single person to a group call.
/// </summary>
public class CallInviteBot : AudioPlaybackAndDTMFCallingBot<GroupCallInviteActiveCallState>
{
    public const string TRANSFERING_PROMPT_ID = "transferingPrompt";
    public CallInviteBot(RemoteMediaCallingBotConfiguration botOptions, BotCallRedirector<BaseGraphCallingBot<GroupCallInviteActiveCallState>, GroupCallInviteActiveCallState> botCallRedirector, ICallStateManager<GroupCallInviteActiveCallState> callStateManager,
        ICallHistoryManager<GroupCallInviteActiveCallState> callHistoryManager, ILogger<GroupCallBot> logger)
        : base(botOptions, botCallRedirector, callStateManager, callHistoryManager, logger) { }

    /// <summary>
    /// Call someone and ask if they can join a group call.
    /// </summary>
    public async Task<Call?> CallCandidateForGroupCall(AttendeeCallInfo initialAdd, StartGroupCallData groupMeetingRequest, Call createdGroupCall)
    {
        if (createdGroupCall == null || createdGroupCall.Id == null)
        {
            _logger.LogError("Invalid group call ID for invites");
            throw new ArgumentNullException(nameof(createdGroupCall));
        }

        // Work out what audio to play, if anything
        var callMediaPlayList = new List<MediaInfo>();

        // Add default media prompt. Will automatically play when call is connected.
        if (!string.IsNullOrEmpty(groupMeetingRequest.MessageInviteUrl))
            callMediaPlayList.Add(new MediaInfo { Uri = groupMeetingRequest.MessageInviteUrl, ResourceId = DEFAULT_PROMPT_ID });

        // Add any message transfering audio
        if (!string.IsNullOrEmpty(groupMeetingRequest.MessageTransferingUrl))
            callMediaPlayList.Add(new MediaInfo { Uri = groupMeetingRequest.MessageTransferingUrl, ResourceId = TRANSFERING_PROMPT_ID });

        // Start P2P call
        var singleAttendeeCallReq = await CreateCallRequest(new InvitationParticipantInfo { Identity = initialAdd.ToIdentity() }, callMediaPlayList, groupMeetingRequest.HasPSTN, false);
        var singleAttendeeCall = await CreateNewCall(singleAttendeeCallReq);

        if (singleAttendeeCall != null)
        {
            // Remember initial state of the call: which group-call to transfer to and who to transfer
            await InitCallStateAndStoreMediaInfoForCreatedCall(singleAttendeeCall, callMediaPlayList,
                createdCallState =>
                {
                    createdCallState.GroupCallId = createdGroupCall.Id;
                    createdCallState.AtendeeIdentity = initialAdd.ToIdentity();
                });
        }

        return singleAttendeeCall;
    }

    protected async override Task NewTonePressed(GroupCallInviteActiveCallState callState, Tone tone)
    {
        if (tone == Tone.Tone1)
        {
            _logger.LogInformation($"Tone 1 pressed on invite call {callState.CallId}, inviting to group call {callState.GroupCallId}...");

            // Play "transfering" WAV.
            await PlayConfiguredMediaIfNotAlreadyPlaying(callState, TRANSFERING_PROMPT_ID);

            await Task.Delay(3000);     // Wait for transfering prompt to play. Bit of a hack. Should be done with a callback.

            // Transfer P2P call to group call, replacing the call used for the invite
            var transferReq = new InvitePostRequestBody
            {
                Participants = new List<InvitationParticipantInfo>
                {
                    new InvitationParticipantInfo
                    {
                        Identity = callState.AtendeeIdentity,
                        ReplacesCallId = callState.CallId
                    },

                },
            };

            await _graphServiceClient.Communications.Calls[callState.GroupCallId].Participants.Invite.PostAsync(transferReq);
            _logger.LogInformation($"Invite call {callState.CallId} to group call {callState.GroupCallId} was successful");
        }
        else
        {
            _logger.LogInformation($"Tone {tone} pressed on invite call {callState.CallId} - ignoring");
        }
    }
}
