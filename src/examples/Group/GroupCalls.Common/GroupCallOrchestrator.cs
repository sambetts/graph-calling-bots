using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;

namespace GroupCalls.Common;

public class GroupCallOrchestrator(GroupCallBot _groupCallingBot, CallInviteBot _callInviteBot, ILogger<GroupCallOrchestrator> _logger)
{
    /// <summary>
    /// Begin both bot calls to start a group call.
    /// </summary>
    public async Task<Call?> StartGroupCall(StartGroupCallData newCallReq)
    {
        var groupCall = await _groupCallingBot.CreateGroupCall(newCallReq);
        if (groupCall != null)
        {
            _logger.LogInformation($"Started group call with ID {groupCall.Id}");

            if (groupCall != null)
            {
                foreach (var attendee in newCallReq.Attendees)
                {
                    var inviteCall = await _callInviteBot.CallCandidateForGroupCall(attendee, newCallReq, groupCall);
                    if (inviteCall == null)
                    {
                        _logger.LogError($"Failed to invite {attendee.DisplayName} ({attendee.Id})");
                    }
                    else
                    {
                        _logger.LogInformation($"Invited '{attendee.DisplayName}' ({attendee.Id}) on new P2P call {inviteCall.Id}");
                    }
                }

                return groupCall;
            }
            else
            {
                return null;
            }
        }
        else
        {
            _logger.LogError("Failed to start group call - empty return call from Graph");
            return null;
        }
    }
}
