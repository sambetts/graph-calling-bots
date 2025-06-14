using GraphCallingBots.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using GroupCalls.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

        var state4 = new BaseActiveCallState
        {
            ResourceUrl = "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9",
            BotMediaPlaylist = new Dictionary<string, EquatableMediaPrompt> { { "1", new EquatableMediaPrompt() } }
        };
        var state5 = new BaseActiveCallState
        {
            ResourceUrl = "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9",
            BotMediaPlaylist = new Dictionary<string, EquatableMediaPrompt> { { "1", new EquatableMediaPrompt() } }
        };

        Assert.AreEqual(state4, state5);
        var state6 = new BaseActiveCallState
        {
            ResourceUrl = "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9",
            BotMediaPlaylist = new Dictionary<string, EquatableMediaPrompt> { { "1", new EquatableMediaPrompt() } },
            JoinedParticipants = new List<CallParticipant> { new CallParticipant { Id = "1" } }
        };
        var state7 = new BaseActiveCallState
        {
            ResourceUrl = "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9",
            BotMediaPlaylist = new Dictionary<string, EquatableMediaPrompt> { { "1", new EquatableMediaPrompt() } },
            JoinedParticipants = new List<CallParticipant> { new CallParticipant { Id = "1" } }
        };

        Assert.AreEqual(state6, state7);

        var state8 = new BaseActiveCallState
        {
            ResourceUrl = "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9",
            BotMediaPlaylist = new Dictionary<string, EquatableMediaPrompt> { { "1", new EquatableMediaPrompt() } },
            JoinedParticipants = new List<CallParticipant> { new CallParticipant { Id = "1" } },
            TonesPressed = new List<Tone> { Tone.Tone1 }
        };
        var state9 = new BaseActiveCallState
        {
            ResourceUrl = "/communications/calls/6f1f5c00-8c1b-47f1-be9d-660c501041a9",
            BotMediaPlaylist = new Dictionary<string, EquatableMediaPrompt> { { "1", new EquatableMediaPrompt() } },
            JoinedParticipants = new List<CallParticipant> { new CallParticipant { Id = "1" } },
            TonesPressed = new List<Tone> { Tone.Tone1 }
        };

        Assert.AreEqual(state8, state9);

        Assert.AreNotEqual(state1, state4);
    }

    [TestMethod]
    public void BaseActiveCallStateEquals_NullableCollections()
    {
        // Both null
        var s1 = new BaseActiveCallState { ResourceUrl = "/communications/calls/1" };
        var s2 = new BaseActiveCallState { ResourceUrl = "/communications/calls/1" };
        Assert.AreEqual(s1, s2);

        // One null, one empty
        s1.BotMediaPlaylist = null;
        s2.BotMediaPlaylist = new Dictionary<string, EquatableMediaPrompt>();
        Assert.AreNotEqual(s1, s2);

        // Both empty
        s1.BotMediaPlaylist = new Dictionary<string, EquatableMediaPrompt>();
        Assert.AreEqual(s1, s2);

        // One null, one with value
        s1.MediaPromptsPlaying = null;
        s2.MediaPromptsPlaying = new List<MediaPrompt> { new MediaPrompt() };
        Assert.AreNotEqual(s1, s2);

        // Both with same value
        var prompt = new MediaPrompt();
        s1.MediaPromptsPlaying = new List<MediaPrompt> { prompt };
        s2.MediaPromptsPlaying = new List<MediaPrompt> { prompt };
        Assert.AreEqual(s1, s2);

        // Both null again
        s1.MediaPromptsPlaying = null;
        s2.MediaPromptsPlaying = null;
        Assert.AreEqual(s1, s2);

        // BotMediaPlaylist with different keys
        s1.BotMediaPlaylist = new Dictionary<string, EquatableMediaPrompt> { { "a", new EquatableMediaPrompt() } };
        s2.BotMediaPlaylist = new Dictionary<string, EquatableMediaPrompt> { { "b", new EquatableMediaPrompt() } };
        Assert.AreNotEqual(s1, s2);

        // BotMediaPlaylist with same keys/values
        var eqPrompt = new EquatableMediaPrompt();
        s1.BotMediaPlaylist = new Dictionary<string, EquatableMediaPrompt> { { "a", eqPrompt } };
        s2.BotMediaPlaylist = new Dictionary<string, EquatableMediaPrompt> { { "a", eqPrompt } };
        Assert.AreEqual(s1, s2);
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

[TestClass]
public class GroupCallInviteActiveCallStateTests
{
    [TestMethod]
    public void Equals_ReturnsTrue_ForIdenticalObjects()
    {
        var idSet1 = new IdentitySet { OdataType = "#microsoft.graph.identitySet" };
        var idSet2 = new IdentitySet { OdataType = "#microsoft.graph.identitySet" };
        var state1 = new GroupCallInviteActiveCallState
        {
            GroupCallId = "group1",
            AtendeeIdentity = idSet1,
            BotClassNameFull = "Bot",
            ResourceUrl = "/calls/1"
        };
        var state2 = new GroupCallInviteActiveCallState
        {
            GroupCallId = "group1",
            AtendeeIdentity = idSet2,
            BotClassNameFull = "Bot",
            ResourceUrl = "/calls/1"
        };

        Assert.IsTrue(state1.Equals(state2));
    }

    [TestMethod]
    public void Equals_ReturnsFalse_ForDifferentGroupCallId()
    {
        var idSet = new IdentitySet { OdataType = "#microsoft.graph.identitySet" };
        var state1 = new GroupCallInviteActiveCallState
        {
            GroupCallId = "group1",
            AtendeeIdentity = idSet,
            BotClassNameFull = "Bot",
            ResourceUrl = "/calls/1"
        };
        var state2 = new GroupCallInviteActiveCallState
        {
            GroupCallId = "group2",
            AtendeeIdentity = idSet,
            BotClassNameFull = "Bot",
            ResourceUrl = "/calls/1"
        };

        Assert.IsFalse(state1.Equals(state2));
    }

    [TestMethod]
    public void Equals_ReturnsFalse_ForDifferentAtendeeIdentity()
    {
        var idSet1 = new IdentitySet { OdataType = "#microsoft.graph.identitySet" };
        var idSet2 = new IdentitySet { OdataType = "#microsoft.graph.identitySet", User = new Identity { Id = "user2" } };
        var state1 = new GroupCallInviteActiveCallState
        {
            GroupCallId = "group1",
            AtendeeIdentity = idSet1,
            BotClassNameFull = "Bot",
            ResourceUrl = "/calls/1"
        };
        var state2 = new GroupCallInviteActiveCallState
        {
            GroupCallId = "group1",
            AtendeeIdentity = idSet2,
            BotClassNameFull = "Bot",
            ResourceUrl = "/calls/1"
        };

        Assert.IsFalse(state1.Equals(state2));
    }

    [TestMethod]
    public void Equals_ReturnsFalse_ForDifferentBaseProperties()
    {
        var idSet = new IdentitySet { OdataType = "#microsoft.graph.identitySet" };
        var state1 = new GroupCallInviteActiveCallState
        {
            GroupCallId = "group1",
            AtendeeIdentity = idSet,
            BotClassNameFull = "Bot1",
            ResourceUrl = "/calls/1"
        };
        var state2 = new GroupCallInviteActiveCallState
        {
            GroupCallId = "group1",
            AtendeeIdentity = idSet,
            BotClassNameFull = "Bot2", // Different base property
            ResourceUrl = "/calls/1"
        };

        Assert.IsFalse(state1.Equals(state2));
    }

    [TestMethod]
    public void Equals_ReturnsFalse_WhenOtherIsNull()
    {
        var state1 = new GroupCallInviteActiveCallState
        {
            GroupCallId = "group1",
            AtendeeIdentity = new IdentitySet { OdataType = "#microsoft.graph.identitySet" },
            BotClassNameFull = "Bot",
            ResourceUrl = "/calls/1"
        };

        Assert.IsFalse(state1.Equals(null));
    }
}
