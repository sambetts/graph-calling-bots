
using Microsoft.Graph;
using SimpleCallingBotEngine;
using SimpleCallingBotEngine.Models;

namespace Engine;

public interface ITeamsChatbotManager
{
    Task AddCall(string number, MeetingState meeting);
    Task<OnlineMeetingInfo> CreateNewMeeting(RemoteMediaCallingBotConfiguration configuration);
    Task Transfer(ActiveCallState callState);
    Task<Call> GroupCall(OnlineMeetingInfo meeting);
}

public class OnlineMeetingInfo
{
    public OnlineMeeting OnlineMeeting { get; set; }
    public ChatInfo ChatInfo { get; set; }
    public MeetingInfo MeetingInfo { get; set; }
    
}

public class GraphTeamsChatbotManager : ITeamsChatbotManager
{
    private readonly GraphServiceClient _graphServiceClient;

    public GraphTeamsChatbotManager(GraphServiceClient graphServiceClient)
    {
        _graphServiceClient = graphServiceClient;
    }

    public Task AddCall(string number, MeetingState meeting)
    {
        throw new NotImplementedException();
    }

    public async Task<OnlineMeetingInfo> CreateNewMeeting(RemoteMediaCallingBotConfiguration configuration)
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
                            Id = configuration.AppInstanceObjectId,
                        },
                    },
                },
            },
        };

        var m = await _graphServiceClient.Users[configuration.AppInstanceObjectId].OnlineMeetings.Request().AddAsync(newMeetingDetails);

        var i = JoinInfo.ParseJoinURL(m.JoinWebUrl);
        return new OnlineMeetingInfo 
        { 
            ChatInfo = i.Item1,
            MeetingInfo = i.Item2,
            OnlineMeeting = m
        };
    }

    public Task<Call> GroupCall(OnlineMeetingInfo meeting)
    {
        throw new NotImplementedException();
    }

    public Task Transfer(ActiveCallState callState)
    {
        // https://learn.microsoft.com/en-us/graph/api/call-transfer?view=graph-rest-1.0&tabs=csharp#example-2-consultative-transfer-from-a-peer-to-peer-call
        throw new NotImplementedException();
    }
}
