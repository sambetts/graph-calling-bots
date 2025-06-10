using GraphCallingBots.Http;
using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Models;

namespace GraphCallingBots.CallingBots;

/// <summary>
/// Base bot class that handles notifications and call state management.
/// </summary>
public abstract class BaseBot<CALLSTATETYPE> : ICommsNotificationsPayloadHandler
    where CALLSTATETYPE : BaseActiveCallState, new()
{
    protected readonly RemoteMediaCallingBotConfiguration _botConfig;
    protected readonly ILogger _logger;
    protected readonly ICallStateManager<CALLSTATETYPE> _callStateManager;
    private readonly ICallHistoryManager<CALLSTATETYPE> _callHistoryManager;
    private readonly IRequestAuthenticationProvider _authenticationProvider;

    public string BotTypeName => GetType().Name;

    public BaseBot(RemoteMediaCallingBotConfiguration botConfig, ICallStateManager<CALLSTATETYPE> callStateManager, ICallHistoryManager<CALLSTATETYPE> callHistoryManager, ILogger logger)
    {
        _botConfig = botConfig;
        _logger = logger;
        _callStateManager = callStateManager;
        _callHistoryManager = callHistoryManager;

        var name = GetType().Assembly.GetName().Name ?? "CallingBot";
        _authenticationProvider = new AuthenticationProvider(name, _botConfig.AppId, _botConfig.AppSecret, _logger);
    }

    /// <summary>
    /// Validate call notifications request again AuthenticationProvider
    /// </summary>
    public async Task<bool> ValidateNotificationRequestAsync(HttpRequest request)
    {
        try
        {
            var httpRequest = request.CreateRequestMessage();
            var results = await _authenticationProvider.ValidateInboundRequestAsync(httpRequest).ConfigureAwait(false);

            // Check tenant IDs match
            return results.IsValid && results.TenantId == _botConfig.TenantId;
        }
        catch (ServiceException e)
        {
            _logger.LogError(e, "Graph error processing notification");

        }
        catch (Exception e)
        {
            _logger.LogError(e, "General error processing notification");
        }

        return false;
    }


    /// <summary>
    /// Init the call state manager and store the media info for the created call.
    /// </summary>
    protected async Task<bool> UpdateCallStateAndStoreMediaInfoForCreatedCall(Call createdCall, MediaInfo callMedia)
    {
        return await UpdateCallStateAndStoreMediaInfoForCreatedCall(createdCall, new List<MediaInfo> { callMedia }, null);
    }


    /// <summary>
    /// Init the call state manager and store the media info for the created call.
    /// </summary>
    protected async Task<bool> UpdateCallStateAndStoreMediaInfoForCreatedCall(Call createdCall, List<MediaInfo> callMedia, Action<CALLSTATETYPE>? updateCacheCallback)
    {
        if (!_callStateManager.Initialised) await _callStateManager.Initialise();

        if (createdCall != null && !string.IsNullOrEmpty(createdCall.Id))
        {
            var updatedCallState = new CALLSTATETYPE
            {
                ResourceUrl = BaseActiveCallState.GetResourceUrlFromCallId(createdCall.Id),
                StateEnum = createdCall.State
            };

            // Is there anything to play?
            if (callMedia.Count > 0)
            {
                var playlistDic = new Dictionary<string, EquatableMediaPrompt>();
                callMedia.Select(m => new EquatableMediaPrompt { MediaInfo = m }).ToList().ForEach(e => playlistDic.Add(Guid.NewGuid().ToString(), e));
                updatedCallState.BotMediaPlaylist = playlistDic;
            }

            // Get state and save invite list for when call is established
            updateCacheCallback?.Invoke(updatedCallState);
            await _callStateManager.AddCallStateOrUpdate(updatedCallState);
            _logger.LogInformation($"InitCallStateAndStoreMediaInfoForCreatedCall: {BotTypeName} - Updated call state for call {createdCall.Id}");
            
            return true;
        }
        else
        {
            _logger.LogError($"{BotTypeName} - Call not created or no call ID found");
            throw new ArgumentOutOfRangeException(nameof(createdCall), "Call not created or no call ID found");
        }
    }

    public async Task<NotificationStats> HandleNotificationsAndUpdateCallStateAsync(CommsNotificationsPayload notifications)
    {
        // Ensure that GetType().FullName is not null before passing it to the method
        var botTypeName = this.GetType().FullName ?? throw new InvalidOperationException("Bot type name cannot be null.");

        // Create a callback handler for notifications. Do so on each request as no state is held.
        var callBacks = new NotificationCallbackInfo<CALLSTATETYPE>
        {
            CallEstablishing = CallEstablishing,
            CallEstablished = CallEstablished,
            CallConnectedWithP2PAudio = CallConnectedWithP2PAudio,
            NewTonePressed = NewTonePressed,
            CallTerminated = CallTerminated,
            PlayPromptFinished = PlayPromptFinished,
            UsersJoinedGroupCall = UsersJoinedGroupCall
        };

        var stats = await BotNotificationsHandler<CALLSTATETYPE>.HandleNotificationsAndUpdateCallStateAsync(
            notifications,
            botTypeName,
            _callStateManager,
            _callHistoryManager,
            callBacks,
            _logger
        );
        return stats;
    }

    public static BOTTYPE HydrateBot<BOTTYPE>(
        RemoteMediaCallingBotConfiguration botConfig,
        BotCallRedirector<BOTTYPE, CALLSTATETYPE> botCallRedirector,
        ICallStateManager<CALLSTATETYPE> callStateManager,
        ICallHistoryManager<CALLSTATETYPE> callHistoryManager,
        ILogger<BOTTYPE> logger)
        where BOTTYPE : BaseBot<CALLSTATETYPE>
    {
        // Use Activator.CreateInstance with parameters and handle potential null return
        var instance = Activator.CreateInstance(typeof(BOTTYPE), botConfig, botCallRedirector, callStateManager, callHistoryManager, logger);

        if (instance is null)
        {
            throw new InvalidOperationException($"Failed to create an instance of type {typeof(BOTTYPE).FullName}");
        }

        return (BOTTYPE)instance;
    }

    #region Bot Events

    protected virtual Task CallEstablishing(CALLSTATETYPE callState)
    {
        return Task.CompletedTask;
    }

    protected virtual Task CallEstablished(CALLSTATETYPE callState)
    {
        return Task.CompletedTask;
    }
    protected virtual Task CallTerminated(string callId, ResultInfo resultInfo)
    {
        return Task.CompletedTask;
    }
    protected virtual Task CallConnectedWithP2PAudio(CALLSTATETYPE callState)
    {
        return Task.CompletedTask;
    }
    protected virtual Task PlayPromptFinished(CALLSTATETYPE callState)
    {
        return Task.CompletedTask;
    }
    protected virtual Task UsersJoinedGroupCall(CALLSTATETYPE callState, List<CallParticipant> participants)
    {
        return Task.CompletedTask;
    }

    protected virtual Task NewTonePressed(CALLSTATETYPE callState, Tone tone)
    {
        _logger.LogInformation($"{GetType().Name}: New tone pressed: {tone} on call {callState.CallId}");
        return Task.CompletedTask;
    }

    #endregion
}
