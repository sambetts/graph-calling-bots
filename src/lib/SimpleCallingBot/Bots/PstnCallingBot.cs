using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using SimpleCallingBotEngine.Models;

namespace SimpleCallingBotEngine.Bots;

/// <summary>
/// A bot that calls you over the phone.
/// </summary>
public abstract class PstnCallingBot : AudioPlaybackAndDTMFCallingBot
{
    private readonly string _callbackUrl;

    protected PstnCallingBot(RemoteMediaCallingBotConfiguration botOptions, ICallStateManager callStateManager, ILogger logger, string callbackUrl) : base(botOptions, callStateManager, logger)
    {
        _callbackUrl = callbackUrl;
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
        foreach (var m in this.MediaMap)
        {
            mediaToPrefetch.Add(m.Value.MediaInfo);
        }

        var newCall = new Call
        {
            Targets = new List<InvitationParticipantInfo>() { new InvitationParticipantInfo { Identity = target }, },
            MediaConfig = new ServiceHostedMediaConfig { PreFetchMedia = mediaToPrefetch },
            RequestedModalities = new List<Modality> { Modality.Audio },
            TenantId = _botOptions.TenantId,
            CallbackUri = _callbackUrl,
            Direction = CallDirection.Outgoing,
            Source = new ParticipantInfo
            {
                Identity = new IdentitySet
                {
                    Application = new Identity { Id = _botOptions.AppId },
                },
            }
        };

        // Set source as this bot
        newCall.Source.Identity.SetApplicationInstance(
            new Identity
            {
                Id = _botOptions.AppInstanceObjectId,
                DisplayName = _botOptions.AppInstanceObjectName,
            });

        return await StartNewCall(newCall);
    }

}
