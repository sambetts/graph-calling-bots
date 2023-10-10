﻿using Microsoft.Graph;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace GroupCalls.Common;

public class StartGroupCallData
{
    public List<AttendeeCallInfo> Attendees { get; set; } = new();
    public bool HasPSTN => this.Attendees.Any(a => a.Type == MeetingAttendeeType.Phone);

    public (List<InvitationParticipantInfo>, List<InvitationParticipantInfo>) GetInitialParticipantsAndInvites(string tenantId)
    {
        var initialAddList = new List<InvitationParticipantInfo>();
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
            initialAddList.Add(newTarget);
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

        return (initialAddList, inviteNumberList);
    }
}

public class AttendeeCallInfo
{
    public string Id { get; set; } = null!;
    public string DisplayId { get; set; } = null!;
    public MeetingAttendeeType Type { get; set; }

    public IdentitySet ToIdentity()
    {
        if (this.Type == MeetingAttendeeType.Phone)
        {
            var i = new IdentitySet();
            i.SetPhone(new Identity { Id = Id, DisplayName = DisplayId });
            return i;
        }
        else if (Type == MeetingAttendeeType.Teams)
        {
            return new IdentitySet { User = new Identity { Id = this.Id, DisplayName = DisplayId } };
        }
        else
        {
            throw new Exception("Unknown attendee type");
        }
    }
}

public enum MeetingAttendeeType
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
    public List<InvitationParticipantInfo> Invites { get; set; } = new();
}
