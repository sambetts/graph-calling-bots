﻿using GraphCallingBots;
using GraphCallingBots.CallingBots;
using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using System.Text.Json;

namespace GroupCalls.Common;

/// <summary>
/// A bot that creates a group meeting to invite people to later. Doesn't call anyone direct - CallInviteBot does that work.
/// </summary>
public class GroupCallBot : AudioPlaybackAndDTMFCallingBot<BaseActiveCallState>
{
    public GroupCallBot(RemoteMediaCallingBotConfiguration botOptions, ICallStateManager<BaseActiveCallState> callStateManager,
        ICallHistoryManager<BaseActiveCallState, CallNotification> callHistoryManager, ILogger<GroupCallBot> logger, BotCallRedirector botCallRedirector)
        : base(botOptions, callStateManager, callHistoryManager, logger, botCallRedirector) { }

    /// <summary>
    /// Create group call so invitees can join if they accept their individual invite calls.
    /// </summary>
    public async Task<Call?> CreateGroupCall(StartGroupCallData meetingRequest)
    {
        if (!meetingRequest.IsValid)
        {
            // Invalid group call request. Send example valid request. 
            var exampleRequest = GetExampleFakeRequest();
            _logger.LogError($"Invalid meeting request. Required request: {JsonSerializer.Serialize(exampleRequest)}");
            return null;
        }

        // Create call request for group call with no media and nobody to call yet. Callers will be added later.
        var groupCallReq = await CreateCallRequest(null, meetingRequest.HasPSTN);

        // Configure meeeting for the group call - either create a new one or use an existing one
        var joinUrl = meetingRequest.JoinMeetingInfo?.JoinUrl;
        if (joinUrl == null)
        {
            // Create a meeting for the group call as organizer. Requires the OnlineMeetings.ReadWrite.All permission.
            _logger.LogInformation($"No preconfigure meeting found in request for group call. Creating online meeting for group call for organiser '{meetingRequest.OrganizerUserId}'");
            var groupCallMeeting = await _graphServiceClient.Users[meetingRequest.OrganizerUserId].OnlineMeetings.PostAsync(new OnlineMeeting
            {
                StartDateTime = DateTimeOffset.UtcNow,
                EndDateTime = DateTimeOffset.UtcNow.AddHours(1),
            });
            joinUrl = groupCallMeeting?.JoinWebUrl;
        }
        else
        {
            _logger.LogInformation($"Using existing meeting for group call");
        }

        // Associate the meeting with the group call
        if (joinUrl != null)
        {
            var (chatInfo, joinInfo) = JoinInfo.ParseJoinURL(joinUrl);
            groupCallReq.MeetingInfo = joinInfo;
            groupCallReq.ChatInfo = chatInfo;
        }

        // Create group call
        var createdGroupCall = await CreateNewCall(groupCallReq);

        _logger.LogInformation($"Group call created");

        return createdGroupCall;
    }

    StartGroupCallData GetExampleFakeRequest()
    {
        return new StartGroupCallData
        {
            OrganizerUserId = Guid.NewGuid().ToString(),
            Attendees = new List<AttendeeCallInfo> { new AttendeeCallInfo { DisplayName = "Teams user", Id = Guid.NewGuid().ToString(), Type = GroupMeetingAttendeeType.Teams } },
            MessageInviteUrl = "https://example.com/callintroaudio.wav"
        };
    }
}
