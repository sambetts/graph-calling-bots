﻿using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using SimpleCallingBotEngine;
using SimpleCallingBotEngine.Bots;
using SimpleCallingBotEngine.Models;

namespace Engine;

public class GroupCallBot : PstnCallingBot<GroupCallActiveCallState>
{
    public const string NotificationPromptName = "NotificationPrompt";
    private readonly ITeamsChatbotManager _teamsChatbotManager;

    public GroupCallBot(ITeamsChatbotManager teamsChatbotManager, RemoteMediaCallingBotConfiguration botOptions, ICallStateManager<GroupCallActiveCallState> callStateManager, ILogger<GroupCallBot> logger) 
        : base(botOptions, callStateManager, logger)
    {

        // Generate media prompts. Used later in call & need to have consistent IDs.
        this.MediaMap[NotificationPromptName] = new MediaPrompt
        {
            MediaInfo = new MediaInfo
            {
                Uri = new Uri(botOptions.BotBaseUrl + "/audio/rickroll.wav").ToString(),
                ResourceId = Guid.NewGuid().ToString(),
            },
        };
        _teamsChatbotManager = teamsChatbotManager;
    }

    public async Task<Call> StartGroupCall(MeetingRequest meetingRequest)
    {
        // Attach media list
        var mediaToPrefetch = new List<MediaInfo>();
        foreach (var m in this.MediaMap)
        {
            mediaToPrefetch.Add(m.Value.MediaInfo);
        }

        var newCall = new Call
        {
            MediaConfig = new ServiceHostedMediaConfig { PreFetchMedia = mediaToPrefetch },
            RequestedModalities = new List<Modality> { Modality.Audio },
            TenantId = _botConfig.TenantId,
            CallbackUri = _botConfig.CallingEndpoint,
            Direction = CallDirection.Outgoing,
            Source = new ParticipantInfo
            {
                Identity = new IdentitySet
                {
                    Application = new Identity { Id = _botConfig.AppId },
                },
            }
        };

        var (initialAddList, inviteNumberList) = meetingRequest.GetInitialParticipantsAndInvites();
        newCall.Targets = initialAddList;

        // Set source as this bot
        newCall.Source.Identity.SetApplicationInstance(
            new Identity
            {
                Id = _botConfig.AppInstanceObjectId,
                DisplayName = _botConfig.AppInstanceObjectName,
            });

        var call = await StartNewCall(newCall);

        // Wait 2 seconds for call to be created and notification to be recieved (so we get a call state)
        await Task.Delay(2000);

        // Get state and save invite list for when call is established
        var callState = await _callStateManager.GetByNotificationResourceUrl($"/communications/calls/{call.Id}");
        if (callState != null)
        {
            callState.Invites = inviteNumberList;
        }

        return call;
    }

    protected async override Task CallEstablished(GroupCallActiveCallState callState)
    {
        if (!string.IsNullOrEmpty(callState?.CallId))
        {
            // Invite everyone else
            foreach (var invite in callState.Invites)
            {
                await InvitePstnNumberToCallAsync(callState.CallId, invite);
            }
        }
    }

    protected override async Task UserJoined(GroupCallActiveCallState callState)
    {
        var alreadyPlaying = false;
        foreach (var itemToPlay in MediaMap.Values)
        {
            if (callState.MediaPromptsPlaying.Select(p => p.MediaInfo.ResourceId).Contains(itemToPlay.MediaInfo.ResourceId))
            {
                alreadyPlaying = true;
                break;
            }
        }

        if (!alreadyPlaying)
        {
            await base.PlayPromptAsync(callState, MediaMap.Select(m => m.Value));
        }
        await base.UserJoined(callState);
    }

    protected override async Task NewTonePressed(GroupCallActiveCallState callState, Tone tone)
    {
        if (tone == Tone.Tone1)
        {
            await _teamsChatbotManager.Transfer(callState);
        }
        await base.NewTonePressed(callState, tone);
    }
}
