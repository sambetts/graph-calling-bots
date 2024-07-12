using Microsoft.Graph.Models;

namespace GraphCallingBots.Models;

public static class Extentions
{
    public static bool IsConnected(this CallMediaState? callMediaState)
    {
        return callMediaState != null && callMediaState.Audio.HasValue && callMediaState.Audio.Value == MediaState.Active;
    }

    public static List<CallParticipant> GetJoinedParticipants(this List<CallParticipant>? newPartipantList, IEnumerable<CallParticipant>? oldList)
    {
        if (newPartipantList == null) return new List<CallParticipant>();
        if (oldList == null) return newPartipantList;

        var joinedParticipants = new List<CallParticipant>();

        foreach (var newParticipant in newPartipantList)
        {
            if (!oldList.Any(p => p.Id == newParticipant.Id))
            {
                joinedParticipants.Add(newParticipant);
            }
        }

        return joinedParticipants;
    }

    public static List<CallParticipant> GetDisconnectedParticipants(this List<CallParticipant>? newPartipantList, IEnumerable<CallParticipant>? oldList)
    {
        if (newPartipantList == null) return new List<CallParticipant>();
        if (oldList == null) return new List<CallParticipant>();

        var disconnectedParticipants = new List<CallParticipant>();

        foreach (var oldParticipant in oldList)
        {
            if (!newPartipantList.Any(p => p.Id == oldParticipant.Id))
            {
                disconnectedParticipants.Add(oldParticipant);
            }
        }

        return disconnectedParticipants;
    }
}
