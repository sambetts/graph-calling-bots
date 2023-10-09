using Microsoft.Graph;
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

        // To start a group call, we can't add Teams + PSTN users at once. We have to add all Teams users first, then add PSTN users.
        foreach (var attendee in Attendees.Where(a => a.Type == MeetingAttendeeType.Teams))
        {
            var newTarget = new InvitationParticipantInfo
            {
                Identity = new IdentitySet { User = new Identity { Id = attendee.Id, DisplayName = attendee.DisplayId } }
            };
            //newTarget.SetInAdditionalData("tenantId", tenantId);
            initialAddList.Add(newTarget);
            initialIdAdded = attendee.Id;
            break;
        }


        // Add anyone left to invites
        foreach (var attendee in Attendees.Where(a => a.Type == MeetingAttendeeType.Teams))
        {
            // If this call starts with a PSTN number, don't add it to the invite list
            if (attendee.Id != initialIdAdded)
            {
                var newTarget = new InvitationParticipantInfo
                {
                    Identity = new IdentitySet { User = new Identity { Id = attendee.Id, DisplayName = attendee.DisplayId } }
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
