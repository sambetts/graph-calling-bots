using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using ServiceHostedMediaCallingBot.Engine.Models;

namespace ServiceHostedMediaCallingBot.UnitTests;

[TestClass]
public class ModelTests
{
    private ILogger _logger;
    public ModelTests()
    {
        _logger = LoggerFactory.Create(config =>
        {
            config.AddTraceSource(new System.Diagnostics.SourceSwitch("SourceSwitch"));
            config.AddConsole();
        }).CreateLogger("Unit tests");
    }

    [TestMethod]
    public void CallIdTests()
    {
        Assert.IsNull(new BaseActiveCallState().CallId);
        Assert.IsNull(new BaseActiveCallState { ResourceUrl = "/communications/calls/" }.CallId);
        Assert.AreEqual("6f1f5c00-8c1b-47f1-be9d-660c501041a9", new BaseActiveCallState { ResourceUrl = "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9" }.CallId);
        Assert.AreEqual("6f1f5c00-8c1b-47f1-be9d-660c501041a9", new BaseActiveCallState { ResourceUrl = "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9/" }.CallId);
        Assert.AreEqual("6f1f5c00-8c1b-47f1-be9d-660c501041a9", new BaseActiveCallState { ResourceUrl = "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9/operations/11cccef9-7eeb-4910-9189-977c0f0eae85" }.CallId);
    }



    [TestMethod]
    public void GetJoinedParticipants()
    {
        var oldList = new List<Participant>
        {
            new Participant { Id = "1" },
            new Participant { Id = "2" },
            new Participant { Id = "3" },
        };

        var newList = new List<Participant>
        {
            new Participant { Id = "1" },
            new Participant { Id = "3" },
            new Participant { Id = "4" },
        };

        var joined = newList.GetJoinedParticipants(oldList);
        Assert.AreEqual(1, joined.Count);
        Assert.AreEqual("4", joined[0].Id);
    }

    [TestMethod]
    public void GetDisconnectedParticipants()
    {
        var oldList = new List<Participant>
        {
            new Participant { Id = "1" },
            new Participant { Id = "2" },
            new Participant { Id = "3" },
        };

        var newList = new List<Participant>
        {
            new Participant { Id = "1" },
            new Participant { Id = "3" },
            new Participant { Id = "4" },
        };

        var disconnected = newList.GetDisconnectedParticipants(oldList);
        Assert.AreEqual(1, disconnected.Count);
        Assert.AreEqual("2", disconnected[0].Id);
    }
}
