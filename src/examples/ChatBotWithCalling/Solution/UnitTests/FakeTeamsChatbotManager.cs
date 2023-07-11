using Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests;


public class FakeTeamsChatbotManager : ITeamsChatbotManager
{
    public Task AddCall(string number, MeetingState meeting)
    {
        return Task.CompletedTask;
    }

    public Task<string> CreateNewMeeting()
    {
        return Task.FromResult("123");
    }
}
