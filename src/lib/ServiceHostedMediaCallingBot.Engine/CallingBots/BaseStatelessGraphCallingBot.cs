using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Communications.Client.Authentication;
using ServiceHostedMediaCallingBot.Engine.Http;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;
using System.Net.Http.Json;
using System.Text.Json;

namespace ServiceHostedMediaCallingBot.Engine.CallingBots;

/// <summary>
/// A simple, stateless bot that can make outbound calls, and play prompts.
/// State is held in the call state manager.
/// </summary>
public abstract class BaseStatelessGraphCallingBot<CALLSTATETYPE> : IGraphCallingBot where CALLSTATETYPE : BaseActiveCallState, new()
{
    public const string DefaultNotificationPrompt = "DefaultNotificationPrompt";

    protected readonly RemoteMediaCallingBotConfiguration _botConfig;
    protected readonly ILogger _logger;
    protected readonly ICallStateManager<CALLSTATETYPE> _callStateManager;
    private readonly ICallHistoryManager<CALLSTATETYPE> _callHistoryManager;
    protected ConfidentialClientApplicationThrottledHttpClient _httpClient;
    private readonly IRequestAuthenticationProvider _authenticationProvider;
    private readonly BotNotificationsHandler<CALLSTATETYPE> _botNotificationsHandler;

    public BaseStatelessGraphCallingBot(RemoteMediaCallingBotConfiguration botConfig, ICallStateManager<CALLSTATETYPE> callStateManager, ICallHistoryManager<CALLSTATETYPE> callHistoryManager, ILogger logger)
    {
        _botConfig = botConfig;
        _logger = logger;
        _callStateManager = callStateManager;
        _callHistoryManager = callHistoryManager;
        _httpClient = new ConfidentialClientApplicationThrottledHttpClient(_botConfig.AppId, _botConfig.AppSecret, _botConfig.TenantId, false, logger);

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
    /// A common way to init the ICallStateManager and create a call request. Also tests if the WAV file exists.
    /// </summary>
    protected async Task<Call> TestCallMediaAndCreateCallRequest(InvitationParticipantInfo initialAdd, MediaInfo defaultMedia, bool addBotIdentityForPSTN)
    {
        if (!_callStateManager.Initialised)
        {
            await _callStateManager.Initialise();
        }

        bool fileExists = await TestExists(defaultMedia.Uri);
        if (!fileExists)
        {
            _logger.LogError($"Media file {defaultMedia.Uri} does not exist. Aborting call");
            throw new ArgumentOutOfRangeException(nameof(defaultMedia), $"Media file {defaultMedia.Uri} does not exist. Aborting call");
        }

        // Create call for initial participants
        var newCall = new Call
        {
            MediaConfig = new ServiceHostedMediaConfig { PreFetchMedia = new List<MediaInfo> { defaultMedia } },
            RequestedModalities = new List<Modality> { Modality.Audio },
            TenantId = _botConfig.TenantId,
            CallbackUri = _botConfig.CallingEndpoint,
            Direction = CallDirection.Outgoing
        };
        _logger.LogDebug($"Validated media info: {JsonSerializer.Serialize(defaultMedia)}");

        // Set source as this bot if we're calling PSTN numbers
        if (addBotIdentityForPSTN)
        {
            newCall.Source = new ParticipantInfo
            {
                Identity = new IdentitySet { Application = new Identity { Id = _botConfig.AppId } },
            };

            newCall.Source.Identity.SetApplicationInstance(new Identity
            {
                Id = _botConfig.AppInstanceObjectId,
                DisplayName = _botConfig.AppInstanceObjectName,
            });
        }

        newCall.Targets = new List<InvitationParticipantInfo> { initialAdd };

        return newCall;
    }

    /// <summary>
    /// Init the call state manager and store the media info for the created call.
    /// </summary>
    protected async Task<bool> InitCallStateAndStoreMediaInfoForCreatedCall(Call createdCall, MediaInfo mediaInfoItem)
    {
        return await InitCallStateAndStoreMediaInfoForCreatedCall(createdCall, mediaInfoItem, null);
    }
    /// <summary>
    /// Init the call state manager and store the media info for the created call.
    /// </summary>
    protected async Task<bool> InitCallStateAndStoreMediaInfoForCreatedCall(Call createdCall, MediaInfo mediaInfoItem, Action<CALLSTATETYPE>? updateCacheCallback)
    {
        if (!_callStateManager.Initialised)
        {
            await _callStateManager.Initialise();
        }
        if (createdCall != null && !string.IsNullOrEmpty(createdCall.Id))
        {
            _logger.LogInformation($"Created call state for call {createdCall.Id}");
            await _callStateManager.AddCallStateOrUpdate(new CALLSTATETYPE
            {
                StateEnum = createdCall.State,
                ResourceUrl = $"/communications/calls/{createdCall.Id}",
                BotMediaPlaylist = new Dictionary<string, CallMediaPrompt>
                {
                    { DefaultNotificationPrompt, new CallMediaPrompt { MediaInfo = mediaInfoItem } }
                }
            });

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

    private async Task<bool> TestExists(string uri)
    {
        try
        {
            var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri));
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<bool> HandleNotificationsAndUpdateCallStateAsync(CommsNotificationsPayload notifications, string bodyRaw)
    {
        JsonDocument? body = null; 
        try
        {
            body = JsonDocument.Parse(bodyRaw);
        }
        catch (JsonException)
        {
            // Ignore
        }
        if (body != null)
        {
            await _botNotificationsHandler.HandleNotificationsAndUpdateCallStateAsync(notifications, body);
        }
        return body != null;
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
        _logger.LogInformation($"New tone pressed: {tone} on call {callState.CallId}");
        return Task.CompletedTask;
    }

    #endregion

    #region Bot Actions

    protected async Task<Call?> CreateNewCall(Call newCall)
    {
        _logger.LogInformation($"Creating new call with Graph API...");
        _logger.LogDebug($"Media info: {JsonSerializer.Serialize(newCall.MediaConfig)}");
        try
        {
            var callCreated = await PostDataAndReturnResult<Call>("/communications/calls", newCall);

            _logger.LogInformation($"Call {callCreated.Id} created");
            return callCreated;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"Can't create call: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/graph/api/call-playprompt
    /// </summary>
    protected async Task<PlayPromptOperation?> PlayPromptAsync(BaseActiveCallState callState, IEnumerable<CallMediaPrompt> mediaPrompts)
    {
        if (mediaPrompts.Count() == 0)
        {
            _logger.LogWarning($"No media prompts to play for call {callState.CallId}");
            return null;
        }
        _logger.LogInformation($"Playing {mediaPrompts.Count()} media prompts to call {callState.CallId}");

        callState.MediaPromptsPlaying.AddRange(mediaPrompts);

        return await PostDataAndReturnResult<PlayPromptOperation>($"/communications/calls/{callState.CallId}/playPrompt", new PlayPromptRequest { Prompts = mediaPrompts });
    }

    protected async Task SubscribeToToneAsync(string callId)
    {
        _logger.LogInformation($"Subscribing to tones for call {callId}");
        await PostData($"/communications/calls/{callId}/subscribeToTone", new EmptyModelWithClientContext());
    }


    // https://learn.microsoft.com/en-us/graph/api/participant-invite?view=graph-rest-1.0
    protected async Task InviteToCallAsync(string callId, List<InvitationParticipantInfo> participantInfo)
    {
        var i = new InviteInfo { Participants = participantInfo };
        _logger.LogInformation($"Inviting {participantInfo.Count} participants to call {callId}");
        await PostData($"/communications/calls/{callId}/participants/invite", i);
    }


    /// <summary>
    /// https://learn.microsoft.com/en-us/graph/api/call-delete
    /// </summary>
    protected async Task HangUp(string callId)
    {
        _logger.LogInformation($"Hanging up call {callId}");
        await this.Delete($"/communications/calls/{callId}");
    }


    #endregion

    #region HTTP Calls

    async Task<T> PostDataAndReturnResult<T>(string urlMinusRoot, object payload)
    {
        var content = await PostData(urlMinusRoot, payload);
        return JsonSerializer.Deserialize<T>(content) ?? throw new Exception("Unexpected Graph response");
    }
    async Task<string> PostData(string urlMinusRoot, object payload)
    {
        var response = await _httpClient.PostAsJsonAsync($"https://graph.microsoft.com/v1.0" + urlMinusRoot, payload);
        var content = await response.Content.ReadAsStringAsync();

        HandleResponse(response, content, urlMinusRoot);

        return content ?? throw new Exception("Unexpected Graph response");
    }

    async Task Delete(string urlMinusRoot)
    {
        var response = await _httpClient.DeleteAsync($"https://graph.microsoft.com/v1.0" + urlMinusRoot);
        var content = await response.Content.ReadAsStringAsync();

        HandleResponse(response, content, urlMinusRoot);
    }

    void HandleResponse(HttpResponseMessage response, string content, string urlMinusRoot)
    {
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Error response {response.StatusCode} calling Graph API url {urlMinusRoot}: {content}");
        }
        response.EnsureSuccessStatusCode();
    }

    #endregion
}
