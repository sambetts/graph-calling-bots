namespace Engine;

public static class Extensions
{
    public async static Task<MeetingRequest> AddNumber(this MeetingRequest meeting, string number, ITeamsChatbotManager teamsChatbotManager)
    {
        // Add number
        meeting.Attendees.Add(new AttendeeCallInfo()
        {
            Id = number
        });

        // Start call
        await teamsChatbotManager.AddCall(number, meeting);

        return meeting;
    }


    public static async Task CreateMeeting(this MeetingRequest meeting, ITeamsChatbotManager teamsChatbotManager)
    {
        throw new NotImplementedException();
        //var newMeeting = await teamsChatbotManager.CreateNewMeeting();
        //meeting.MeetingUrl = newMeeting.OnlineMeeting.JoinWebUrl;
        //meeting.Created = DateTime.Now;

    }
}
