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
        string url = await teamsChatbotManager.CreateNewMeeting();
        meeting.MeetingUrl = url;
        meeting.Created = DateTime.Now;

    }
}
