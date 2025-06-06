using GraphCallingBots;
using GraphCallingBots.CallingBots;
using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Communications.Calls.Item.Participants.Invite;
using Microsoft.Graph.Models;
using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GroupCalls.Common;

/// <summary>
/// Bot that invites a single person to a group call.
/// </summary>
public class CallInviteBot : AudioPlaybackAndDTMFCallingBot<GroupCallInviteActiveCallState, CallInviteBot>
{
    public const string TRANSFERING_PROMPT_ID = "transferingPrompt";
    public CallInviteBot(RemoteMediaCallingBotConfiguration botOptions, 
        BotCallRedirector<CallInviteBot, GroupCallInviteActiveCallState> botCallRedirector, 
        ICallStateManager<GroupCallInviteActiveCallState> callStateManager,
        ICallHistoryManager<GroupCallInviteActiveCallState> callHistoryManager, 
        ILogger<CallInviteBot> logger)
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
        var singleAttendeeCallWithMediaReq = await CreateCallRequest(new InvitationParticipantInfo { Identity = initialAdd.ToIdentity() }, callMediaPlayList, groupMeetingRequest.HasPSTN, false);
        var singleAttendeeCall = await CreateNewCall(singleAttendeeCallWithMediaReq);

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
            _logger.LogInformation($"{BotTypeName} - Tone 1 pressed on invite call {callState.CallId}, inviting to group call {callState.GroupCallId}...");

            // Play "transfering" WAV.
            await PlayConfiguredMediaIfNotAlreadyPlaying(callState, TRANSFERING_PROMPT_ID);

            await Task.Delay(3000);     // Wait for transfering prompt to play. Bit of a hack. Should be done with a callback.

            // Transfer P2P call to group call, replacing this call for the group call in the invite
            var transferReq = new InvitePostRequestBody
            {
                Participants = new List<InvitationParticipantInfo>
                {
                    new InvitationParticipantInfo
                    {
                        OdataType = "#microsoft.graph.invitationParticipantInfo",
                        Identity = callState.AtendeeIdentity,
                        ReplacesCallId = callState.CallId
                    },

                },
            };

            // https://learn.microsoft.com/en-us/graph/api/participant-invite
            await _graphServiceClient.Communications.Calls[callState.GroupCallId].Participants.Invite.PostAsync(transferReq);
            _logger.LogInformation($"{BotTypeName} - Invite call {callState.CallId} to group call {callState.GroupCallId} was successful");
        }
        else
        {
            _logger.LogInformation($"{BotTypeName} - Unexpected tone '{tone}' pressed on invite call {callState.CallId} - ignoring");
        }
    }
}
