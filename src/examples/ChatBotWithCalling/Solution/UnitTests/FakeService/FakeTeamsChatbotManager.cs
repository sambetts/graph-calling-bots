using Engine;
using Microsoft.Graph;
using SimpleCallingBotEngine;
using SimpleCallingBotEngine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.FakeService;


public class FakeTeamsChatbotManager : ITeamsChatbotManager
{
    public Task AddCall(string number, MeetingState meeting)
    {
        return Task.CompletedTask;
    }

    public Task<string> CreateNewMeeting(RemoteMediaCallingBotConfiguration configuration)
    {
        return Task.FromResult("123");
    }

    public Task<Call> GroupCall(Engine.OnlineMeetingInfo meeting)
    {
        throw new NotImplementedException();
    }

    public Task Transfer(ActiveCallState callState)
    {
        return Task.CompletedTask;  
    }

    Task<Engine.OnlineMeetingInfo> ITeamsChatbotManager.CreateNewMeeting(RemoteMediaCallingBotConfiguration configuration)
    {
        throw new NotImplementedException();
    }
}
