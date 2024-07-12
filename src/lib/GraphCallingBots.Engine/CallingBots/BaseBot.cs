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
public abstract class BaseBot<CALLSTATETYPE> : IGraphCallingBot, ICommsNotificationsPayloadHandler
    where CALLSTATETYPE : BaseActiveCallState, new()
{
    protected readonly RemoteMediaCallingBotConfiguration _botConfig;
    protected readonly ILogger _logger;
    protected readonly ICallStateManager<CALLSTATETYPE> _callStateManager;
    private readonly ICallHistoryManager<CALLSTATETYPE, CallNotification> _callHistoryManager;
    private readonly IRequestAuthenticationProvider _authenticationProvider;
    private readonly BotNotificationsHandler<CALLSTATETYPE> _botNotificationsHandler;

    public string BotTypeName => GetType().Name;

    public BaseBot(RemoteMediaCallingBotConfiguration botConfig, ICallStateManager<CALLSTATETYPE> callStateManager,
        ICallHistoryManager<CALLSTATETYPE, CallNotification> callHistoryManager, ILogger logger)
    {
        _botConfig = botConfig;
        _logger = logger;
        _callStateManager = callStateManager;
        _callHistoryManager = callHistoryManager;

        var name = GetType().Assembly.GetName().Name ?? "CallingBot";
        _authenticationProvider = new AuthenticationProvider(name, _botConfig.AppId, _botConfig.AppSecret, _logger);

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
        _botNotificationsHandler = new BotNotificationsHandler<CALLSTATETYPE>(_callStateManager, _callHistoryManager, callBacks, _logger);
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
    protected async Task<bool> InitCallStateAndStoreMediaInfoForCreatedCall(Call createdCall, MediaInfo callMedia)
    {
        return await InitCallStateAndStoreMediaInfoForCreatedCall(createdCall, new List<MediaInfo> { callMedia }, null);
    }

    /// <summary>
    /// Init the call state manager and store the media info for the created call.
    /// </summary>
    protected async Task<bool> InitCallStateAndStoreMediaInfoForCreatedCall(Call createdCall, Action<CALLSTATETYPE>? updateCacheCallback)
    {
        return await InitCallStateAndStoreMediaInfoForCreatedCall(createdCall, new List<MediaInfo>(), updateCacheCallback);
    }

    /// <summary>
    /// Init the call state manager and store the media info for the created call.
    /// </summary>
    protected async Task<bool> InitCallStateAndStoreMediaInfoForCreatedCall(Call createdCall, List<MediaInfo> callMedia, Action<CALLSTATETYPE>? updateCacheCallback)
    {
        if (!_callStateManager.Initialised)
        {
            await _callStateManager.Initialise();
        }
        if (createdCall != null && !string.IsNullOrEmpty(createdCall.Id))
        {
            _logger.LogInformation($"Created call state for call {createdCall.Id}");

            var initialCallState = new CALLSTATETYPE
            {
                ResourceUrl = $"/communications/calls/{createdCall.Id}",
                StateEnum = createdCall.State
            };

            // Is there anything to play?
            if (callMedia.Count > 0)
            {
                var playlistDic = new Dictionary<string, EquatableMediaPrompt>();
                callMedia.Select(m => new EquatableMediaPrompt { MediaInfo = m }).ToList().ForEach(e => playlistDic.Add(Guid.NewGuid().ToString(), e));
                initialCallState.BotMediaPlaylist = playlistDic;
            }
            await _callStateManager.AddCallStateOrUpdate(initialCallState);

            // Get state and save invite list for when call is established
            var createdCallState = await _callStateManager.GetByNotificationResourceUrl($"/communications/calls/{createdCall.Id}");
            if (createdCallState != null)
            {
                updateCacheCallback?.Invoke(createdCallState);
                await _callStateManager.UpdateCurrentCallState(createdCallState);
            }
            else
            {
                _logger.LogError("Unable to find call state for call {CallId}", createdCall.Id);
            }
            return true;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(createdCall), "Call not created or no call ID found");
        }
    }

    public async Task HandleNotificationsAndUpdateCallStateAsync(CommsNotificationsPayload notifications)
    {
        await _botNotificationsHandler.HandleNotificationsAndUpdateCallStateAsync(notifications, this);
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
