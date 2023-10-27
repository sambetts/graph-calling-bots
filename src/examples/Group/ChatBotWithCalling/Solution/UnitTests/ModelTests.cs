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
    public void JoinInfoTests()
    {
        Assert.ThrowsException<ArgumentException>(() => JoinInfo.ParseJoinURL("https://teams.microsoft.com/l/meetup-join/19:"));

        var (chat, b) = JoinInfo.ParseJoinURL("https://teams.microsoft.com/l/meetup-join/19%3ameeting_YzgyNjA4ZDctODdhNi00ODM3LWJiZmUtYzE4YjVjYTViMzMz%40thread.v2/0?context=%7b%22Tid%22%3a%22ceb5448c-1842-4459-a46e-89bbe6a3d40e%22%2c%22Oid%22%3a%22a1e57e0d-e571-4c34-8135-c8c19464ac83%22%7d");
    }

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
