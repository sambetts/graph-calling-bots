
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using SimpleCallingBotEngine;
using SimpleCallingBotEngine.Models;

namespace Engine;

public interface ITeamsChatbotManager
{
    Task AddCall(string number, MeetingRequest meeting);
    Task<Call> StartNewCall(RemoteMediaCallingBotConfiguration configuration, MeetingRequest meetingRequest);
    Task Transfer(ActiveCallState callState);
    Task<Call> GroupCall(OnlineMeetingInfo meeting);
    Task<string?> GetUserIdByEmailAsync(string contactId);
}

public class OnlineMeetingInfo
{
    public OnlineMeeting OnlineMeeting { get; set; } = null!;
    public ChatInfo ChatInfo { get; set; } = null!;
    public MeetingInfo MeetingInfo { get; set; } = null!;
}

[Obsolete]
public class GraphTeamsChatbotManager : ITeamsChatbotManager
{
    private readonly GraphServiceClient _graphServiceClient;
    private readonly ILogger _logger;

    public GraphTeamsChatbotManager(GraphServiceClient graphServiceClient, ILogger<GraphTeamsChatbotManager> logger)
    {
        _graphServiceClient = graphServiceClient;
        _logger = logger;
    }

    public Task AddCall(string number, MeetingRequest meeting)
    {
        throw new NotImplementedException();
    }

    public async Task<Call> StartNewCall(RemoteMediaCallingBotConfiguration configuration, MeetingRequest meetingRequest)
    {
        throw new NotImplementedException();
    }

    public async Task<string?> GetUserIdByEmailAsync(string email)
    {
        _logger.LogInformation($"Looking up user {email}"); 
        User? user = null;  
        try
        {
            user = await _graphServiceClient.Users[email].Request().GetAsync();
        }
        catch (ServiceException ex)
        {
            _logger.LogError($"Couldn't lookup user - {ex.Message}");
            throw;
        }
        return user?.Id;
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
