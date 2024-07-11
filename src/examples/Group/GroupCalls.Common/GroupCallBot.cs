﻿using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using ServiceHostedMediaCallingBot.Engine;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;
using System.Text.Json;

namespace GroupCalls.Common;

/// <summary>
/// A bot that creates a group meeting to invite people to later.
/// </summary>
public class GroupCallBot : PstnCallingBot<BaseActiveCallState>
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
            var exampleRequest = new StartGroupCallData
            {
                OrganizerUserId = Guid.NewGuid().ToString(),
                Attendees = new List<AttendeeCallInfo> { new AttendeeCallInfo { DisplayName = "Teams user", Id = Guid.NewGuid().ToString(), Type = GroupMeetingAttendeeType.Teams } },
                MessageInviteUrl = "https://example.com/callintroaudio.wav"
            };
            _logger.LogError($"Invalid meeting request. Required request: {JsonSerializer.Serialize(exampleRequest)}");
            return null;
        }

        // Create group call with nobody in. We'll transfer people into it later.
        var inviteNumberList = meetingRequest.Attendees.Select(a => new InvitationParticipantInfo { Identity = a.ToIdentity() });

        var groupCallReq = await CreateCallRequest(null, meetingRequest.HasPSTN, false);

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

        _logger.LogInformation($"Group call created.");

        return createdGroupCall;
    }
}
