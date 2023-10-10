using Microsoft.Extensions.Logging;
using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Graph;
using System.Net.Http.Json;
using System.Text.Json;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;
using ServiceHostedMediaCallingBot.Engine.Http;

namespace ServiceHostedMediaCallingBot.Engine.CallingBots;

/// <summary>
/// A simple, stateless bot that can make outbound calls, and play prompts.
/// State is held in the call state manager.
/// </summary>
public abstract class BaseStatelessGraphCallingBot<CALLSTATETYPE> : IGraphCallingBot where CALLSTATETYPE : BaseActiveCallState, new()
{
    protected readonly RemoteMediaCallingBotConfiguration _botConfig;
    protected readonly ILogger _logger;
    protected readonly ICallStateManager<CALLSTATETYPE> _callStateManager;
    protected ConfidentialClientApplicationThrottledHttpClient _httpClient;
    private readonly IRequestAuthenticationProvider _authenticationProvider;
    private readonly BotNotificationsHandler<CALLSTATETYPE> _botNotificationsHandler;

    public BaseStatelessGraphCallingBot(RemoteMediaCallingBotConfiguration botConfig, ICallStateManager<CALLSTATETYPE> callStateManager, ILogger logger)
    {
        _botConfig = botConfig;
        _logger = logger;
        _callStateManager = callStateManager;
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
        _botNotificationsHandler = new BotNotificationsHandler<CALLSTATETYPE>(_callStateManager, callBacks, _logger);
    }

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

    public async Task HandleNotificationsAndUpdateCallStateAsync(CommsNotificationsPayload notifications)
    {
        await _botNotificationsHandler.HandleNotificationsAndUpdateCallStateAsync(notifications);
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
    protected virtual Task UsersJoinedGroupCall(CALLSTATETYPE callState, List<Participant> participants)
    {
        return Task.CompletedTask;
    }

    protected virtual Task NewTonePressed(CALLSTATETYPE callState, Tone tone)
    {
        _logger.LogInformation($"New tone pressed: {tone}");
        return Task.CompletedTask;
    }

    #endregion

    #region Bot Actions

    protected async Task<Call?> StartNewCall(Call newCall)
    {
        _logger.LogInformation($"Creating new call...");
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
    protected async Task<PlayPromptOperation> PlayPromptAsync(BaseActiveCallState callState, IEnumerable<MediaPrompt> mediaPrompts)
    {
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
        if (!response.IsSuccessStatusCode)
        {
            // Oops
        }
        response.EnsureSuccessStatusCode();
    }


    #endregion
}
