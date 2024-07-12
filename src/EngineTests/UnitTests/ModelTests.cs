using GraphCallingBots.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;

namespace GraphCallingBots.UnitTests;

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
    public void BaseActiveCallStateEqualsTests()
    {
        var state1 = new BaseActiveCallState { ResourceUrl = "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9" };
        var state2 = new BaseActiveCallState { ResourceUrl = "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9" };

        Assert.AreEqual(state1, state2);
        state1.StateEnum = CallState.TransferAccepted;
        Assert.AreNotEqual(state1, state2);

        var state4 = new BaseActiveCallState { ResourceUrl = "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9", BotMediaPlaylist = { { "1", new EquatableMediaPrompt() } } };
        var state5 = new BaseActiveCallState { ResourceUrl = "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9", BotMediaPlaylist = { { "1", new EquatableMediaPrompt() } } };

        Assert.AreEqual(state4, state5);
        var state6 = new BaseActiveCallState { ResourceUrl = "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9", BotMediaPlaylist = { { "1", new EquatableMediaPrompt() } }, JoinedParticipants = { new CallParticipant { Id = "1" } } };
        var state7 = new BaseActiveCallState { ResourceUrl = "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9", BotMediaPlaylist = { { "1", new EquatableMediaPrompt() } }, JoinedParticipants = { new CallParticipant { Id = "1" } } };

        Assert.AreEqual(state6, state7);

        var state8 = new BaseActiveCallState { ResourceUrl = "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9", BotMediaPlaylist = { { "1", new EquatableMediaPrompt() } }, JoinedParticipants = { new CallParticipant { Id = "1" } }, TonesPressed = new List<Tone> { Tone.Tone1 } };
        var state9 = new BaseActiveCallState { ResourceUrl = "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9", BotMediaPlaylist = { { "1", new EquatableMediaPrompt() } }, JoinedParticipants = { new CallParticipant { Id = "1" } }, TonesPressed = new List<Tone> { Tone.Tone1 } };

        Assert.AreEqual(state8, state9);

        Assert.AreNotEqual(state1, state4);
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
        var oldList = new List<CallParticipant>
        {
            new CallParticipant { Id = "1" },
            new CallParticipant { Id = "2" },
            new CallParticipant { Id = "3" },
        };

        var newList = new List<CallParticipant>
        {
            new CallParticipant { Id = "1" },
            new CallParticipant { Id = "3" },
            new CallParticipant { Id = "4" },
        };

        var joined = newList.GetJoinedParticipants(oldList);
        Assert.AreEqual(1, joined.Count);
        Assert.AreEqual("4", joined[0].Id);
    }

    [TestMethod]
    public void GetDisconnectedParticipants()
    {
        var oldList = new List<CallParticipant>
        {
            new CallParticipant { Id = "1" },
            new CallParticipant { Id = "2" },
            new CallParticipant { Id = "3" },
        };

        var newList = new List<CallParticipant>
        {
            new CallParticipant { Id = "1" },
            new CallParticipant { Id = "3" },
            new CallParticipant { Id = "4" },
        };

        var disconnected = newList.GetDisconnectedParticipants(oldList);
        Assert.AreEqual(1, disconnected.Count);
        Assert.AreEqual("2", disconnected[0].Id);
    }
}
