﻿using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;
using System.ComponentModel;

namespace GroupCalls.Common;

/// <summary>
/// A bot that starts a call with a bunch of people, internal and external.
/// </summary>
public class GroupCallStartBot : PstnCallingBot<GroupCallActiveCallState>
{
    public const string NotificationPromptName = "NotificationPrompt";

    public GroupCallStartBot(SingleWavFileBotConfig botOptions, ICallStateManager<GroupCallActiveCallState> callStateManager, ILogger<GroupCallStartBot> logger)
        : base(botOptions, callStateManager, logger)
    {

        // Generate media prompts. Used later in call & need to have consistent IDs.
        MediaMap[NotificationPromptName] = new MediaPrompt
        {
            MediaInfo = new MediaInfo
            {
                Uri = new Uri(botOptions.BotBaseUrl + botOptions.RelativeWavCallbackUrl).ToString(),
                ResourceId = Guid.NewGuid().ToString(),
            },
        };
    }

    /// <summary>
    /// Start group call with required attendees.
    /// </summary>
    public async Task<Call?> StartGroupCall(StartGroupCallData meetingRequest)
    {
        if (!_callStateManager.Initialised)
        {
            await _callStateManager.Initialise();
        }

        // Attach media list
        var mediaToPrefetch = new List<MediaInfo>();
        foreach (var m in MediaMap) mediaToPrefetch.Add(m.Value.MediaInfo);

        // Create call for initial participants
        var newCall = new Call
        {
            MediaConfig = new ServiceHostedMediaConfig { PreFetchMedia = mediaToPrefetch },
            RequestedModalities = new List<Modality> { Modality.Audio },
            TenantId = _botConfig.TenantId,
            CallbackUri = _botConfig.CallingEndpoint,
            Direction = CallDirection.Outgoing
        };

        //newCall.Source = new ParticipantInfo
        //{
        //    Identity = new IdentitySet
        //    {
        //        Application = new Identity { Id = _botConfig.AppId },
        //    },
        //};

        // Set source as this bot
        if (meetingRequest.HasPSTN)
        {
            newCall.Source.Identity.SetApplicationInstance(
            new Identity
            {
                Id = _botConfig.AppInstanceObjectId,
                DisplayName = _botConfig.AppInstanceObjectName,
            });

        }
        


        // Work out who to call first & who to invite
        var (initialAddList, inviteNumberList) = meetingRequest.GetInitialParticipantsAndInvites(_botConfig.TenantId);
        newCall.Targets = initialAddList;

        // Start call
        var createdCall = await StartNewCall(newCall);
        if (createdCall != null)
        {
            // Wait 2 seconds for call to be created and notification to be recieved (so we have a call state to update)
            await Task.Delay(2000);

            // Get state and save invite list for when call is established
            var createdCallState = await _callStateManager.GetByNotificationResourceUrl($"/communications/calls/{createdCall.Id}");
            if (createdCallState != null)
            {
                createdCallState.Invites = inviteNumberList;
                await _callStateManager.Update(createdCallState);
            }
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
            foreach (var invite in callState.Invites)
            {
            }
            if (callState.Invites != null && callState.Invites.Count > 0)
            {
                await InviteToCallAsync(callState.CallId, callState.Invites);

                callState.Invites.Clear();
                await _callStateManager.Update(callState);
            }
        }
    }

    protected override async Task UserJoinedGroupCall(GroupCallActiveCallState callState)
    {
        // Don't play media if already playing
        var alreadyPlaying = false;
        foreach (var itemToPlay in MediaMap.Values)
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
            await PlayPromptAsync(callState, MediaMap.Select(m => m.Value));
        }
        await base.UserJoinedGroupCall(callState);
    }
}
