﻿using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace ServiceHostedMediaCallingBot.Engine.CallingBots;

/// <summary>
/// A bot that calls you over the phone.
/// </summary>
public abstract class PstnCallingBot<T> : AudioPlaybackAndDTMFCallingBot<T>, IPstnCallingBot where T : BaseActiveCallState, new()
{
    protected PstnCallingBot(RemoteMediaCallingBotConfiguration botConfig, ICallStateManager<T> callStateManager, ICallHistoryManager<T, CallNotification> callHistoryManager, ILogger logger)
        : base(botConfig, callStateManager, callHistoryManager, logger)
    {
    }

    /// <summary>
    /// Call someone over the phone with media set.
    /// </summary>
    public async Task<Call?> StartPTSNCall(string phoneNumber, string mediaUrl)
    {
        // PSTN call target - identity is type "phone", which the usual object model doesn't support very well
        var target = new IdentitySet();
        target.SetPhone(new Identity { Id = phoneNumber, DisplayName = phoneNumber });


        var mediaInfoItem = new MediaInfo { Uri = mediaUrl, ResourceId = Guid.NewGuid().ToString() };
        var pstnCallRequest = await TestCallMediaAndCreateCallRequest(new InvitationParticipantInfo { Identity = target }, mediaInfoItem, true);

        var createdCall = await CreateNewCall(pstnCallRequest);
        if (createdCall != null)
            await InitCallStateAndStoreMediaInfoForCreatedCall(createdCall, mediaInfoItem);

        return createdCall;
    }
}
