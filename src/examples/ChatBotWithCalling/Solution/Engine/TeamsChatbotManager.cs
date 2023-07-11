
using Microsoft.Graph;
using SimpleCallingBotEngine;

namespace Engine;

public interface ITeamsChatbotManager
{
    Task AddCall(string number, MeetingState meeting);
    Task<string> CreateNewMeeting();
    Task Transfer(ActiveCallState callState);
}

public class GraphTeamsChatbotManager : ITeamsChatbotManager
{
    private readonly GraphServiceClient _graphServiceClient;
    private readonly CallAndRedirectBot _bot;

    public GraphTeamsChatbotManager(GraphServiceClient graphServiceClient, CallAndRedirectBot bot)
    {
        _graphServiceClient = graphServiceClient;
        _bot = bot;
    }

    public async Task AddCall(string number, MeetingState meeting)
    {
        await _bot.StartPTSNCall(number);
    }

    public async Task<string> CreateNewMeeting()
    {
        var newMeetingDetails = new OnlineMeeting
        {
            StartDateTime = DateTimeOffset.UtcNow,
            EndDateTime = DateTimeOffset.UtcNow.AddHours(1),
            Subject = "Test Meeting",
            Participants = new MeetingParticipants
            {
                Organizer = new MeetingParticipantInfo
                {
                    Identity = new IdentitySet
                    {
                        User = new Identity
                        {
                            Id = _bot.BotConfig.AppInstanceObjectId,
                        },
                    },
                },
            },
        };


        var m = await _graphServiceClient.Users[_bot.BotConfig.AppInstanceObjectId].OnlineMeetings.Request().AddAsync(newMeetingDetails);
        return m.JoinWebUrl;
    }

    public Task Transfer(ActiveCallState callState)
    {
        throw new NotImplementedException();
    }
}
