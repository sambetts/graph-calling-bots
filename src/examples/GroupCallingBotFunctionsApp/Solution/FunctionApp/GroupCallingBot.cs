using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace GroupCallingBotFunctionsApp.FunctionApp;

public class GroupCallingBot : PstnCallingBot<GroupCallActiveCallState>
{
    public const string NotificationPromptName = "NotificationPrompt";

    public GroupCallingBot(SingleWavFileBotConfig botOptions, ICallStateManager<GroupCallActiveCallState> callStateManager, ILogger<GroupCallingBot> logger)
        : base(botOptions, callStateManager, logger)
    {

        // Generate media prompts. Used later in call & need to have consistent IDs.
        MediaMap[NotificationPromptName] = new MediaPrompt
        {
            MediaInfo = new MediaInfo
            {
                Uri = new Uri(botOptions.BotBaseUrl + "/audio/rickroll.wav").ToString(),
                ResourceId = Guid.NewGuid().ToString(),
            },
        };
    }

    /// <summary>
    /// Start group call with required attendees.
    /// </summary>
    public async Task<Call> StartGroupCall(StartCallData meetingRequest)
    {
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
            Direction = CallDirection.Outgoing,
            Source = new ParticipantInfo
            {
                Identity = new IdentitySet
                {
                    Application = new Identity { Id = _botConfig.AppId },
                },
            }
        };

        // Work out who to call first & who to invite
        var (initialAddList, inviteNumberList) = meetingRequest.GetInitialParticipantsAndInvites(_botConfig.TenantId);
        newCall.Targets = initialAddList;

        // Set source as this bot
        newCall.Source.Identity.SetApplicationInstance(
            new Identity
            {
                Id = _botConfig.AppInstanceObjectId,
                DisplayName = _botConfig.AppInstanceObjectName,
            });

        // Start call
        var createdCall = await StartNewCall(newCall);

        // Wait 2 seconds for call to be created and notification to be recieved (so we have a call state to update)
        await Task.Delay(2000);

        // Get state and save invite list for when call is established
        var createdCallState = await _callStateManager.GetByNotificationResourceUrl($"/communications/calls/{createdCall.Id}");
        if (createdCallState != null)
        {
            createdCallState.Invites = inviteNumberList;
        }

        return createdCall;
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
            await PlayPromptAsync(callState, MediaMap.Select(m => m.Value));
        }
        await base.UserJoined(callState);
    }
}
