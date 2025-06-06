using GraphCallingBots.Models;
using Microsoft.Graph.Contracts;
using Microsoft.Graph.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GroupCalls.Common;

/// <summary>
/// Configuration for the group call bot. Who to call, and what to play.
/// </summary>
public class StartGroupCallData
{
    /// <summary>
    /// List of people to invite to the call.
    /// </summary>
    public List<AttendeeCallInfo> Attendees { get; set; } = new();

    /// <summary>
    /// Absolute URL to WAV file to play to attendees when the bot 1st calls.
    /// </summary>
    public string? MessageInviteUrl { get; set; } = null;

    /// <summary>
    /// Absolute URL to WAV file to play to attendees when the user confirms they want to join the group call.
    /// </summary>
    public string? MessageTransferingUrl { get; set; } = null;

    /// <summary>
    /// Id of the organizer of the group call.
    /// </summary>
    public string? OrganizerUserId { get; set; }

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

        // To start a group call, we can't add all users at once, for some reason. Teams just fails to actually call, and even if it worked is limited to 5 users.
        // So to workaround this we add one initial user to the call, then invite the rest.
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

    [JsonIgnore]
    public bool IsValid => !string.IsNullOrEmpty(OrganizerUserId) && Attendees.Count > 0;

    /// <summary>
    /// Optional join URL for the group call. If not specified, a new meeting will be created.
    /// </summary>
    public JoinMeetingInfo? JoinMeetingInfo { get; set; } = null;
}

public class JoinMeetingInfo
{
    public required string JoinUrl { get; set; }
}

public class AttendeeCallInfo
{
    public string Id { get; set; } = null!;
    public string? DisplayName { get; set; } = null!;
    public GroupMeetingAttendeeType Type { get; set; }

    public IdentitySet ToIdentity()
    {
        var idSet = new IdentitySet { OdataType = "#microsoft.graph.identitySet" };
        if (this.Type == GroupMeetingAttendeeType.Phone)
        {
            idSet.SetPhone(new Identity { Id = Id, DisplayName = DisplayName, OdataType = "#microsoft.graph.identity" });
            return idSet;
        }
        else if (Type == GroupMeetingAttendeeType.Teams)
        {
            idSet.User = new Identity { Id = this.Id, OdataType = "#microsoft.graph.identity" };
            return idSet;
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

public class GroupCallInviteActiveCallState : BaseActiveCallState, IEquatable<GroupCallInviteActiveCallState>
{
    public string GroupCallId { get; set; } = null!;
    public IdentitySet? AtendeeIdentity { get; set; } = null!;
    
    public bool Equals(GroupCallInviteActiveCallState? other)
    {
        if (other is null) return false;

        // Compare AtendeeIdentity. Serialise both if not null, otherwise compare for null.
        var atendeeIdentityEqual = AtendeeIdentity == null && other.AtendeeIdentity == null
            || AtendeeIdentity != null && other.AtendeeIdentity != null
            && JsonSerializer.Serialize(AtendeeIdentity) == JsonSerializer.Serialize(other.AtendeeIdentity);

        return this.GroupCallId == other.GroupCallId && atendeeIdentityEqual && base.Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GroupCallId, AtendeeIdentity?.GetHashCode() ?? 0);
    }
}
