﻿using Microsoft.Graph;
using Microsoft.Graph.Contracts;
using Microsoft.Graph.Models;
using ServiceHostedMediaCallingBot.Engine.Models;
using System.Text.Json.Serialization;

namespace GroupCalls.Common;

public class StartGroupCallData
{
    /// <summary>
    /// List of people to invite to the call.
    /// </summary>
    public List<AttendeeCallInfo> Attendees { get; set; } = new();

    /// <summary>
    /// Absolute URL to WAV file to play to attendees.
    /// </summary>
    public string? MessageUrl { get; set; } = null;

    /// <summary>
    /// Info to join the meeting, if this is a Teams meeting.
    /// </summary>
    public JoinMeetingInfo? JoinMeetingInfo { get; set; } = null;

    [JsonIgnore]
    public bool HasPSTN => this.Attendees.Any(a => a.Type == GroupMeetingAttendeeType.Phone);

    /// <summary>
    /// Split the attendees into 2 lists, one for the initial call, and one for the invites.
    /// This is because we can't add everyone at once. 
    /// </summary>
    public (InvitationParticipantInfo, List<InvitationParticipantInfo>) GetInitialParticipantsAndInvites()
    {
        InvitationParticipantInfo initialAdd = null!;
        var inviteNumberList = new List<InvitationParticipantInfo>();

        string? initialIdAdded = null;

        // To start a group call, we can't add all users at once, for some reason. It just fails to actually call, and even if it worked is limited to 5 users.
        // So to workaround this we add one user to the call, then invite the rest.
        foreach (var attendee in Attendees)
        {
            var newTarget = new InvitationParticipantInfo
            {
                Identity = attendee.ToIdentity()
            };
            initialAdd = newTarget;
            initialIdAdded = attendee.Id;
            break;
        }

        // Add anyone left to invites
        foreach (var attendee in Attendees)
        {
            // Don't add the 1st attendee
            if (attendee.Id != initialIdAdded)
            {
                var newTarget = new InvitationParticipantInfo
                {
                    Identity = attendee.ToIdentity()
                };
                inviteNumberList.Add(newTarget);
            }
        }

        return (initialAdd, inviteNumberList);
    }
}

public class JoinMeetingInfo
{
    public string? JoinUrl { get; set; } = null!;
}

public class AttendeeCallInfo
{
    public string Id { get; set; } = null!;
    public string? DisplayName { get; set; } = null!;
    public GroupMeetingAttendeeType Type { get; set; }

    public IdentitySet ToIdentity()
    {
        if (this.Type == GroupMeetingAttendeeType.Phone)
        {
            var i = new IdentitySet();
            i.SetPhone(new Identity { Id = Id, DisplayName = DisplayName });
            return i;
        }
        else if (Type == GroupMeetingAttendeeType.Teams)
        {
            return new IdentitySet { User = new Identity { Id = this.Id } };
        }
        else
        {
            throw new Exception("Unknown attendee type");
        }
    }
}

public enum GroupMeetingAttendeeType
{
    Unknown,
    Phone,
    Teams
}

public class GroupCallActiveCallState : BaseActiveCallState
{
    /// <summary>
    /// List of invitees to the call once call is established.
    /// </summary>
    public List<InvitationParticipantInfo> GroupCallInvites { get; set; } = new();
}
