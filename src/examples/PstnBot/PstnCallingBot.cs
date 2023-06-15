﻿using Microsoft.Graph;
using SimpleCallingBot;

namespace PstnBot;

/// <summary>
/// The core bot logic.
/// </summary>
public class PstnCallingBot : WebApiGraphCallingBot
{
    /// <remarks>
    /// message: "There is an incident occured. Press '1' to join the incident meeting. Press '0' to listen to the instruction again. ".
    /// </remarks>
    public const string NotificationPromptName = "NotificationPrompt";

    public Dictionary<string, MediaPrompt> MediaMap { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PstnCallingBot" /> class.
    /// </summary>
    public PstnCallingBot(BotOptions botOptions, ILogger logger, ICallStateManager callStateManager) : base(botOptions, logger, callStateManager)
    {

        this.MediaMap[NotificationPromptName] = new MediaPrompt
        {
            MediaInfo = new MediaInfo
            {
                Uri = new Uri(botOptions.BotBaseUrl + "/audio/responder-notification.wav").ToString(),
                ResourceId = Guid.NewGuid().ToString(),
            },
        };
    }

    /// <summary>
    /// Raise an incident.
    /// </summary>
    public async Task<Call> StartP2PCall(string phoneNumber)
    {
        var scenarioId = Guid.NewGuid();
        var target = new IdentitySet();
        target.SetPhone(
            new Identity
            {
                Id = phoneNumber,
                DisplayName = phoneNumber
            });

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
            CallbackUri = _botOptions.BotBaseUrl + HttpRouteConstants.OnIncomingRequestRoute,
            Direction = CallDirection.Outgoing,
            Source = new ParticipantInfo
            {
                Identity = new IdentitySet
                {
                    Application = new Identity { Id = _botOptions.AppId },
                },
            }
        };

        newCall.Source.Identity.SetApplicationInstance(
            new Identity
            {
                Id = _botOptions.AppInstanceObjectId,
                DisplayName = _botOptions.AppInstanceObjectName,
            });

        return await StartNewCall(newCall);
    }

    protected override async Task CallConnected(ActiveCallState callState)
    {
        await base.SubscribeToToneAsync(callState.CallId);
        await base.PlayPromptAsync(callState.CallId, new List<MediaPrompt> { MediaMap[NotificationPromptName] });
    }
}
