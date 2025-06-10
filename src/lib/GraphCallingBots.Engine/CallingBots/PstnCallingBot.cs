using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Contracts;
using Microsoft.Graph.Models;

namespace GraphCallingBots.CallingBots;

/// <summary>
/// A bot that calls you over the phone.
/// </summary>
public abstract class PstnCallingBot<CALLSTATETYPE, BOTTYPE> : AudioPlaybackAndDTMFCallingBot<CALLSTATETYPE, BOTTYPE>, IPstnCallingBot 
    where CALLSTATETYPE : BaseActiveCallState, new()
    where BOTTYPE : BaseBot<CALLSTATETYPE>
{
    protected PstnCallingBot(RemoteMediaCallingBotConfiguration botConfig, BotCallRedirector<BOTTYPE, CALLSTATETYPE> botCallRedirector, ICallStateManager<CALLSTATETYPE> callStateManager, ICallHistoryManager<CALLSTATETYPE> callHistoryManager,
        ILogger logger)
        : base(botConfig, botCallRedirector, callStateManager, callHistoryManager, logger)
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
        var pstnCallRequest = await CreateCallRequest(new InvitationParticipantInfo { Identity = target }, mediaInfoItem, true, true);

        var createdCall = await CreateNewCall(pstnCallRequest);
        if (createdCall != null)
            await UpdateCallStateAndStoreMediaInfoForCreatedCall(createdCall, mediaInfoItem);

        return createdCall;
    }
}
