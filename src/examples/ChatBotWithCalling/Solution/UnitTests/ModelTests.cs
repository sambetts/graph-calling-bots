using GroupCallingChatBot.Web.Models;
using Microsoft.Extensions.Logging;

namespace GroupCallingChatBot.UnitTests;

[TestClass]
public class ModelTests
{
    protected BotConfig _config = null!;
    protected ILogger _tracer = null!;


    [TestMethod]
    public void MeetingRequestInvites()
    {
        var twoTeamsPlusOnePhoneUserMeeting = new MeetingRequest()
        {
            Attendees = new List<AttendeeCallInfo>
            {
                new AttendeeCallInfo{ Type = MeetingAttendeeType.Teams, Id = "t1" },
                new AttendeeCallInfo{ Type = MeetingAttendeeType.Teams, Id = "t2" },
                new AttendeeCallInfo{ Type = MeetingAttendeeType.Phone, Id = "p1" }
            }
        };

        var (initialAddList, inviteNumberList) = twoTeamsPlusOnePhoneUserMeeting.GetInitialParticipantsAndInvites(string.Empty);
        Assert.AreEqual(2, initialAddList.Count);
        Assert.AreEqual(1, inviteNumberList.Count);


        var threePhoneUsersMeeting = new MeetingRequest()
        {
            Attendees = new List<AttendeeCallInfo>
            {
                new AttendeeCallInfo{ Type = MeetingAttendeeType.Phone, Id = "p1" },
                new AttendeeCallInfo{ Type = MeetingAttendeeType.Phone, Id = "p2" },
                new AttendeeCallInfo{ Type = MeetingAttendeeType.Phone, Id = "p3" },
            }
        };

        (initialAddList, inviteNumberList) = threePhoneUsersMeeting.GetInitialParticipantsAndInvites(string.Empty);
        Assert.AreEqual(1, initialAddList.Count);
        Assert.AreEqual(2, inviteNumberList.Count);
    }
}
