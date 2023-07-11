using Engine;

namespace UnitTests;

[TestClass]
public class TeamsChatbotManagerTests
{
    [TestMethod]
    public async Task FakeTests()
    {
        var fakeTeamsChatbotManager = new FakeTeamsChatbotManager();    
        var url = await fakeTeamsChatbotManager.CreateNewMeeting();
        Assert.AreEqual("123", url);

        var meeting = new MeetingState();
        Assert.IsFalse(meeting.IsMeetingCreated);

        await meeting.CreateMeeting(fakeTeamsChatbotManager);
        Assert.IsTrue(meeting.IsMeetingCreated);

        await meeting.AddNumber("555", fakeTeamsChatbotManager);
        Assert.IsTrue(meeting.Numbers.Any());
    }
}
