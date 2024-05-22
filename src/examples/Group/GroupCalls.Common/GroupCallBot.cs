using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;
using System.Text.Json;

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
        if (!meetingRequest.IsValid)
        {
            var exampleRequest = new StartGroupCallData 
            { 
                OrganizerUserId = Guid.NewGuid().ToString(), 
                Attendees = new List<AttendeeCallInfo> { new AttendeeCallInfo { DisplayName = "Teams user", Id = Guid.NewGuid().ToString(), Type = GroupMeetingAttendeeType.Teams } },
                MessageUrl = "https://example.com/audio.wav"
            };
            _logger.LogError($"Invalid meeting request. Required request: {JsonSerializer.Serialize(exampleRequest)}");
            return null;
        }

        // Work out what audio to play, if anything
        var mediaInfoItem = string.IsNullOrEmpty(meetingRequest.MessageUrl) ? null : new MediaInfo { Uri = meetingRequest.MessageUrl, ResourceId = Guid.NewGuid().ToString() };

        if (mediaInfoItem?.Uri != null)
        {
            var validMedia = await TestMediaExists(mediaInfoItem.Uri);
            if (!validMedia)
            {
                _logger.LogError("Media URL is invalid");
                return null;
            }
        }

        var (initialAdd, inviteNumberList) = meetingRequest.GetInitialParticipantsAndInvites();

        var groupCallReq = await CreateCallRequest(initialAdd, null, meetingRequest.HasPSTN, false);

        // Create a meeting for the group call as organizer. Requires the OnlineMeetings.ReadWrite.All permission.
        _logger.LogInformation($"Creating online meeting for group call for user '{meetingRequest.OrganizerUserId}'");
        var groupCallMeeting = await _graphServiceClient.Users[meetingRequest.OrganizerUserId].OnlineMeetings.PostAsync(new OnlineMeeting
        {
            StartDateTime = DateTimeOffset.UtcNow,
            EndDateTime = DateTimeOffset.UtcNow.AddHours(1),
        });

        var (chatInfo, joinInfo) = JoinInfo.ParseJoinURL(groupCallMeeting.JoinWebUrl);
        groupCallReq.MeetingInfo = joinInfo;
        groupCallReq.ChatInfo = chatInfo;

        var createdGroupCall = await CreateNewCall(groupCallReq);

        if (createdGroupCall != null)
        {
            // Remember initial state
            await InitCallStateAndStoreMediaInfoForCreatedCall(createdGroupCall, mediaInfoItem, createdCallState => createdCallState.GroupCallInvites = inviteNumberList);
        }

        // Call each attendee seperately and invite them to a common call
        foreach (var attendee in meetingRequest.Attendees)
        {
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
