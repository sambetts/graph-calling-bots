using GroupCallingChatBot.Web.Models;
using GroupCalls.Common;
using Microsoft.Extensions.Logging;

namespace GroupCallingChatBot.UnitTests;

[TestClass]
public class ModelTests
{
    protected TeamsChatbotBotConfig _config = null!;
    protected ILogger _tracer = null!;


    [TestMethod]
    public void MeetingRequestInvites()
    {
        var twoTeamsPlusOnePhoneUserMeeting = new StartGroupCallData()
        {
            Attendees = new List<AttendeeCallInfo>
            {
                new AttendeeCallInfo{ Type = GroupMeetingAttendeeType.Teams, Id = "t1" },
                new AttendeeCallInfo{ Type = GroupMeetingAttendeeType.Teams, Id = "t2" },
                new AttendeeCallInfo{ Type = GroupMeetingAttendeeType.Phone, Id = "p1" }
            }
        };

        var (initialAdd, inviteNumberList) = twoTeamsPlusOnePhoneUserMeeting.GetInitialParticipantsAndInvites();
        Assert.IsNotNull(initialAdd);
        Assert.AreEqual(2, inviteNumberList.Count);


        var threePhoneUsersMeeting = new StartGroupCallData()
        {
            Attendees = new List<AttendeeCallInfo>
            {
                new AttendeeCallInfo{ Type = GroupMeetingAttendeeType.Phone, Id = "p1" },
                new AttendeeCallInfo{ Type = GroupMeetingAttendeeType.Phone, Id = "p2" },
                new AttendeeCallInfo{ Type = GroupMeetingAttendeeType.Phone, Id = "p3" },
            }
        };

        (initialAdd, inviteNumberList) = threePhoneUsersMeeting.GetInitialParticipantsAndInvites();
        Assert.IsNotNull(initialAdd);
        Assert.AreEqual(2, inviteNumberList.Count);
    }
}
