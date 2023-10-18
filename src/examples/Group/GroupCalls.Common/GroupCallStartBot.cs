using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace GroupCalls.Common;

/// <summary>
/// A bot that starts a call with a bunch of people, internal and external.
/// </summary>
public class GroupCallStartBot : PstnCallingBot<GroupCallActiveCallState>
{

    public GroupCallStartBot(RemoteMediaCallingBotConfiguration botOptions, ICallStateManager<GroupCallActiveCallState> callStateManager, ICallHistoryManager<GroupCallActiveCallState> callHistoryManager, ILogger<GroupCallStartBot> logger)
        : base(botOptions, callStateManager, callHistoryManager, logger)
    {
    }

    /// <summary>
    /// Start group call with required attendees.
    /// </summary>
    public async Task<Call?> StartGroupCall(StartGroupCallData meetingRequest)
    {
        var mediaInfoItem = new MediaInfo { Uri = meetingRequest.MessageUrl, ResourceId = Guid.NewGuid().ToString() };

        // Work out who to call first & who to invite
        var (initialAdd, inviteNumberList) = meetingRequest.GetInitialParticipantsAndInvites();

        // Create call for initial participants
        var newCall = await InitAndCreateCallRequest(initialAdd, mediaInfoItem, meetingRequest.HasPSTN);

        // Start call
        var createdCall = await StartNewCall(newCall);

        if (createdCall != null)
        {
            await InitCallStateAndStoreMediaInfoForCreatedCall(createdCall, mediaInfoItem, createdCallState => createdCallState.Invites = inviteNumberList);
        }
        return createdCall;
    }


    /// <summary>
    /// Due to how group calls work with PSTN numbers especially, we need to invite everyone else after the call is established.
    /// </summary>
    protected async override Task CallEstablished(GroupCallActiveCallState callState)
    {
        if (!string.IsNullOrEmpty(callState?.CallId))
        {
            // Invite everyone else
            if (callState.Invites != null && callState.Invites.Count > 0)
            {
                await InviteToCallAsync(callState.CallId, callState.Invites);

                callState.Invites.Clear();
                await _callStateManager.UpdateCurrentCallState(callState);
            }
        }
    }

    protected override async Task UsersJoinedGroupCall(GroupCallActiveCallState callState, List<Participant> participants)
    {
        await CheckCall(callState);
    }

    protected async override Task CallConnectedWithP2PAudio(GroupCallActiveCallState callState)
    {
        await CheckCall(callState);
    }

    async Task CheckCall(GroupCallActiveCallState callState)
    {
        // Don't play media if already playing
        var alreadyPlaying = false;
        foreach (var itemToPlay in callState.BotMediaPlaylist.Values)
        {
            if (callState.MediaPromptsPlaying.Select(p => p.MediaInfo.ResourceId).Contains(itemToPlay.MediaInfo.ResourceId))
            {
                alreadyPlaying = true;
                break;
            }
        }

        // But if not playing, play notification prompt again
        if (!alreadyPlaying)
        {
            try
            {
                await PlayPromptAsync(callState, callState.BotMediaPlaylist.Select(m => m.Value));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error playing prompt");
            }
        }
    }
}
