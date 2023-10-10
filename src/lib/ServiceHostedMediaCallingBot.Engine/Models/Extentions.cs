using Microsoft.Graph;

namespace ServiceHostedMediaCallingBot.Engine.Models;

public static class Extentions
{
    public static bool IsConnected(this CallMediaState? callMediaState)
    {
        return callMediaState != null && callMediaState.Audio.HasValue && callMediaState.Audio.Value == MediaState.Active;
    }

    public static List<Participant> GetJoinedParticipants(this List<Participant>? newPartipantList, IEnumerable<Participant>? oldList)
    {
        if (newPartipantList == null) return new List<Participant>();
        if (oldList == null) return newPartipantList;

        var joinedParticipants = new List<Participant>();

        foreach (var newParticipant in newPartipantList)
        {
            if (!oldList.Any(p => p.Id == newParticipant.Id))
            {
                joinedParticipants.Add(newParticipant);
            }
        }

        return joinedParticipants;
    }

    public static List<Participant> GetDisconnectedParticipants(this List<Participant>? newPartipantList, IEnumerable<Participant>? oldList)
    {
        if (newPartipantList == null) return new List<Participant>();
        if (oldList == null) return new List<Participant>();

        var disconnectedParticipants = new List<Participant>();

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
