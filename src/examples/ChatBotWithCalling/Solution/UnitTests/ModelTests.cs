using Engine;
using Microsoft.Extensions.Logging;

namespace UnitTests;

[TestClass]
public class ModelTests
{
    protected Config _config = null!;
    protected ILogger _tracer = null!;


    [TestMethod]
    public void MeetingRequestInvites()
    {
        var twoTeamsPlusOnePhoneUserMeeting = new MeetingRequest() 
        { 
            Attendees = new List<AttendeeCallInfo>
            {
                new AttendeeCallInfo{ Type = AttendeeType.Teams, Id = "t1" },
                new AttendeeCallInfo{ Type = AttendeeType.Teams, Id = "t2" },
                new AttendeeCallInfo{ Type = AttendeeType.Phone, Id = "p1" }
            } 
        };

        var (initialAddList, inviteNumberList) = twoTeamsPlusOnePhoneUserMeeting.GetInitialParticipantsAndInvites();
        Assert.AreEqual(2, initialAddList.Count);
        Assert.AreEqual(1, inviteNumberList.Count);


        var threePhoneUsersMeeting = new MeetingRequest()
        {
            Attendees = new List<AttendeeCallInfo>
            {
                new AttendeeCallInfo{ Type = AttendeeType.Phone, Id = "p1" },
                new AttendeeCallInfo{ Type = AttendeeType.Phone, Id = "p2" },
                new AttendeeCallInfo{ Type = AttendeeType.Phone, Id = "p3" },
            }
        };

        (initialAddList, inviteNumberList) = threePhoneUsersMeeting.GetInitialParticipantsAndInvites();
        Assert.AreEqual(1, initialAddList.Count);
        Assert.AreEqual(2, inviteNumberList.Count);
    }
}
