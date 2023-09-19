using Microsoft.Graph;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace GroupCalls.Common;

public class StartGroupCallData
{
    public List<AttendeeCallInfo> Attendees { get; set; } = new();

    public (List<InvitationParticipantInfo>, List<string>) GetInitialParticipantsAndInvites(string tenantId)
    {
        var initialAddList = new List<InvitationParticipantInfo>();
        var inviteNumberList = new List<string>();

        // To start a group call, we can't add Teams + PSTN users at once. We have to add all Teams users first, then add PSTN users.
        foreach (var attendee in Attendees.Where(a => a.Type == MeetingAttendeeType.Teams))
        {
            var newTarget = new InvitationParticipantInfo
            {
                Identity = new IdentitySet { User = new Identity { Id = attendee.Id, DisplayName = attendee.DisplayId } }
            };
            newTarget.SetInAdditionalData("tenantId", tenantId);
            initialAddList.Add(newTarget);
        }

        string? initialPhoneNumberAdded = null;
        if (initialAddList.Count == 0)
        {
            // If no Teams users, start call with single PSTN user and each the rest as invitations
            var phoneUsers = Attendees.Where(attendees => attendees.Type == MeetingAttendeeType.Phone).ToList();
            if (phoneUsers.Count == 0)
            {
                throw new Exception("No attendees provided");
            }

            // Start call with 1st PSTN user and invite the rest
            var firstPhoneUser = new InvitationParticipantInfo { Identity = new IdentitySet() };
            firstPhoneUser.Identity.SetPhone(new Identity { Id = phoneUsers[0].Id, DisplayName = phoneUsers[0].DisplayId });
            initialAddList.Add(firstPhoneUser);
            initialPhoneNumberAdded = phoneUsers[0].Id;
        }

        // Add anyone left to invites
        foreach (var attendee in Attendees.Where(a => a.Type == MeetingAttendeeType.Phone))
        {
            // If this call starts with a PSTN number, don't add it to the invite list
            if (attendee.Id != initialPhoneNumberAdded)
            {
                inviteNumberList.Add(attendee.Id);
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
    public List<string> Invites { get; set; } = new();
}
