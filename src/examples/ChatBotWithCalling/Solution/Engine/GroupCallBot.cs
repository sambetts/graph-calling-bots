using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using SimpleCallingBotEngine;
using SimpleCallingBotEngine.Bots;
using SimpleCallingBotEngine.Models;

namespace Engine;

public class GroupCallBot : PstnCallingBot
{
    public const string NotificationPromptName = "NotificationPrompt";
    private readonly ITeamsChatbotManager _teamsChatbotManager;

    public GroupCallBot(ITeamsChatbotManager teamsChatbotManager, RemoteMediaCallingBotConfiguration botOptions, ICallStateManager callStateManager, ILogger<GroupCallBot> logger) 
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

        var targetsList = new List<InvitationParticipantInfo>();
        foreach (var attendee in meetingRequest.Attendees)
        {
            var newTarget = new InvitationParticipantInfo { Identity = new IdentitySet() };
            if (attendee.Type == AttendeeType.Phone)
            {
                newTarget.Identity.SetPhone(new Identity { Id = attendee.Id, DisplayName = attendee.Id });
            }
            else if (attendee.Type == AttendeeType.Teams)
            {
                newTarget.Identity.User = new Identity { Id = attendee.Id, DisplayName = attendee.DisplayId };
            }
            targetsList.Add(newTarget);
        }
        newCall.Targets = targetsList;

        // Set source as this bot
        newCall.Source.Identity.SetApplicationInstance(
            new Identity
            {
                Id = _botConfig.AppInstanceObjectId,
                DisplayName = _botConfig.AppInstanceObjectName,
            });

        return await StartNewCall(newCall);
    }

    protected override async Task UserJoined(ActiveCallState callState)
    {
        await base.PlayPromptAsync(callState.CallId!, MediaMap.Select(m => m.Value));
        await base.UserJoined(callState);
    }

    protected override async Task NewTonePressed(ActiveCallState callState, Tone tone)
    {
        if (tone == Tone.Tone1)
        {
            await _teamsChatbotManager.Transfer(callState);
        }
        await base.NewTonePressed(callState, tone);
    }
}
