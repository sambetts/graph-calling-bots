using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace ServiceHostedMediaCallingBot.Engine.Bots;

/// <summary>
/// A bot that calls you over the phone.
/// </summary>
public abstract class PstnCallingBot<T> : AudioPlaybackAndDTMFCallingBot<T> where T : BaseActiveCallState, new()
{
    protected PstnCallingBot(RemoteMediaCallingBotConfiguration botConfig, ICallStateManager<T> callStateManager, ILogger logger)
        : base(botConfig, callStateManager, logger)
    {
    }

    /// <summary>
    /// Call someone over the phone with media set.
    /// </summary>
    public async Task<Call> StartPTSNCall(string phoneNumber)
    {
        // PSTN call
        var target = new IdentitySet();
        target.SetPhone(new Identity { Id = phoneNumber, DisplayName = phoneNumber });

        // Attach media list
        var mediaToPrefetch = new List<MediaInfo>();
        foreach (var m in MediaMap)
        {
            mediaToPrefetch.Add(m.Value.MediaInfo);
        }

        var newCall = new Call
        {
            Targets = new List<InvitationParticipantInfo>() { new InvitationParticipantInfo { Identity = target }, },
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

        // Set source as this bot
        newCall.Source.Identity.SetApplicationInstance(
            new Identity
            {
                Id = _botConfig.AppInstanceObjectId,
                DisplayName = _botConfig.AppInstanceObjectName,
            });

        return await StartNewCall(newCall);
    }

}
