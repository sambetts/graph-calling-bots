namespace Engine;

public static class Extensions
{
    public async static Task<MeetingState> AddNumber(this MeetingState meeting, string number, ITeamsChatbotManager teamsChatbotManager)
    {
        // Add number
        meeting.Numbers.Add(new NumberCallState()
        {
            Number = number
        });

        // Start call
        await teamsChatbotManager.AddCall(number, meeting);

        return meeting;
    }


    public static async Task CreateMeeting(this MeetingState meeting, ITeamsChatbotManager teamsChatbotManager)
    {
        throw new NotImplementedException();
        //var newMeeting = await teamsChatbotManager.CreateNewMeeting();
        //meeting.MeetingUrl = newMeeting.OnlineMeeting.JoinWebUrl;
        //meeting.Created = DateTime.Now;

    }
}
