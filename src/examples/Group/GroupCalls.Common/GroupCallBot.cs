using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using ServiceHostedMediaCallingBot.Engine;
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
    public GroupCallBot(RemoteMediaCallingBotConfiguration botOptions, ICallStateManager<GroupCallActiveCallState> callStateManager, 
        ICallHistoryManager<GroupCallActiveCallState, CallNotification> callHistoryManager, ILogger<GroupCallBot> logger, BotCallRedirector botCallRedirector)
        : base(botOptions, callStateManager, callHistoryManager, logger, botCallRedirector) { }

    /// <summary>
    /// Start group call with required attendees.
    /// </summary>
    public async Task<Call?> StartGroupCall(StartGroupCallData meetingRequest)
    {
        if (!meetingRequest.IsValid)
        {
            // Invalid group call request. Send example valid request. 
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

        // Create group call with nobody in. We'll transfer people into it later.
        var (_, inviteNumberList) = meetingRequest.GetInitialParticipantsAndInvites();

        var groupCallReq = await CreateCallRequest(null, null, meetingRequest.HasPSTN, false);

        // Create a meeting for the group call as organizer. Requires the OnlineMeetings.ReadWrite.All permission.
        _logger.LogInformation($"Creating online meeting for group call for organiser '{meetingRequest.OrganizerUserId}'");
        var groupCallMeeting = await _graphServiceClient.Users[meetingRequest.OrganizerUserId].OnlineMeetings.PostAsync(new OnlineMeeting
        {
            StartDateTime = DateTimeOffset.UtcNow,
            EndDateTime = DateTimeOffset.UtcNow.AddHours(1),
        });

        // Configure meeeting for the group call
        if (groupCallMeeting?.JoinWebUrl != null)
        {
            var (chatInfo, joinInfo) = JoinInfo.ParseJoinURL(groupCallMeeting.JoinWebUrl);
            groupCallReq.MeetingInfo = joinInfo;
            groupCallReq.ChatInfo = chatInfo;
        }

        // Create group call
        var createdGroupCall = await CreateNewCall(groupCallReq);

        if (createdGroupCall != null)
        {
            // Remember initial state
            await InitCallStateAndStoreMediaInfoForCreatedCall(createdGroupCall, mediaInfoItem, createdCallState => createdCallState.GroupCallInvites = inviteNumberList);
        }

        _logger.LogInformation($"Group call created.");

        return createdGroupCall;
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
