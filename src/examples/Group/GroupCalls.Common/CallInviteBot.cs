using Microsoft.Extensions.Logging;
using Microsoft.Graph.Communications.Calls.Item.Participants.Invite;
using Microsoft.Graph.Models;
using ServiceHostedMediaCallingBot.Engine;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace GroupCalls.Common;

/// <summary>
/// Bot that invites a single person to a group call.
/// </summary>
public class CallInviteBot : PstnCallingBot<GroupCallInviteActiveCallState>
{
    public const string TRANSFERING_PROMPT_ID = "transferingPrompt";
    public CallInviteBot(RemoteMediaCallingBotConfiguration botOptions, ICallStateManager<GroupCallInviteActiveCallState> callStateManager,
        ICallHistoryManager<GroupCallInviteActiveCallState, CallNotification> callHistoryManager, ILogger<GroupCallBot> logger, BotCallRedirector botCallRedirector)
        : base(botOptions, callStateManager, callHistoryManager, logger, botCallRedirector) { }

    /// <summary>
    /// Call someone and ask if they can join a group call.
    /// </summary>
    public async Task<Call?> CallCandidateForGroupCall(AttendeeCallInfo initialAdd, StartGroupCallData groupMeetingRequest, Call createdGroupCall)
    {
        if (createdGroupCall == null || createdGroupCall.Id == null)
        {
            throw new ArgumentNullException(nameof(createdGroupCall));
        }

        // Work out what audio to play, if anything
        var playList = new List<MediaInfo>();

        // Add default prompt. Will automatically play
        if (!string.IsNullOrEmpty(groupMeetingRequest.MessageInviteUrl)) playList.Add(new MediaInfo { Uri = groupMeetingRequest.MessageInviteUrl, ResourceId = DEFAULT_PROMPT_ID });

        // Add any message transfering audio
        if (!string.IsNullOrEmpty(groupMeetingRequest.MessageTransferingUrl)) playList.Add(new MediaInfo { Uri = groupMeetingRequest.MessageTransferingUrl, ResourceId = TRANSFERING_PROMPT_ID });

        // Remember initial state
        await InitCallStateAndStoreMediaInfoForCreatedCall(createdGroupCall, playList, createdCallState => createdCallState.GroupCallId = createdGroupCall.Id);

        var singleAttendeeCallReq = await CreateCallRequest(new InvitationParticipantInfo { Identity = initialAdd.ToIdentity() }, playList, groupMeetingRequest.HasPSTN, false);

        // Start call
        var singleAttendeeCall = await CreateNewCall(singleAttendeeCallReq);

        if (singleAttendeeCall != null)
        {
            // Remember initial state of the call to transfer to and who to transfer to it
            await InitCallStateAndStoreMediaInfoForCreatedCall(singleAttendeeCall, playList,
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
