using Microsoft.Extensions.Logging;
using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Graph;
using System.Net.Http.Json;
using System.Text.Json;
using SimpleCallingBotEngine.Models;
using SimpleCallingBotEngine.Http;
using SimpleCallingBotEngineEngine;

namespace SimpleCallingBotEngine;

/// <summary>
/// A simple, stateless bot that can make outbound calls and play prompts.
/// </summary>
public abstract class StatelessGraphCallingBot
{
    protected readonly BotOptions _botOptions;
    protected readonly ILogger _logger;
    private readonly ICallStateManager _callStateManager;
    protected ConfidentialClientApplicationThrottledHttpClient _httpClient;
    private readonly IRequestAuthenticationProvider _authenticationProvider;
    private readonly BotNotificationsHandler _botNotificationsHandler;

    public BotNotificationsHandler BotNotificationsHandler => _botNotificationsHandler;

    public StatelessGraphCallingBot(BotOptions botOptions, ICallStateManager callStateManager, ILogger logger)
    {
        _botOptions = botOptions;
        _logger = logger;
        _callStateManager = callStateManager;
        _httpClient = new ConfidentialClientApplicationThrottledHttpClient(_botOptions.AppId, _botOptions.AppSecret, _botOptions.TenantId, false, logger);

        var name = this.GetType().Assembly.GetName().Name ?? "CallingBot";
        _authenticationProvider = new AuthenticationProvider(name, _botOptions.AppId, _botOptions.AppSecret, _logger);

        // Create a callback handler for notifications. Do so on each request as no state is held.
        var callBacks = new NotificationCallbackInfo
        {
            CallConnectedWithAudio = CallConnected,
            NewTonePressed = NewTonePressed,
        };
        _botNotificationsHandler = new BotNotificationsHandler(_callStateManager, callBacks, _logger);
    }

    public async Task<bool> ValidateNotificationRequestAsync(HttpRequest request)
    {
        try
        {
            var httpRequest = request.CreateRequestMessage();
            var results = await this._authenticationProvider.ValidateInboundRequestAsync(httpRequest).ConfigureAwait(false);

            // Check tenant IDs match
            return results.IsValid && results.TenantId == _botOptions.TenantId;
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


    protected abstract Task CallConnected(ActiveCallState callState);
    protected virtual Task NewTonePressed(ActiveCallState callState, Tone tone) 
    { 
        _logger.LogInformation($"New tone pressed: {tone}");
        return Task.CompletedTask;  
    }


    #region Bot Actions

    protected async Task<Call> StartNewCall(Call newCall)
    {
        var callCreated = await PostDataAndReturnResult<Call>("/communications/calls", newCall);

        _logger.LogInformation($"Call {callCreated.Id} created");
        return callCreated;
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/graph/api/call-playprompt
    /// </summary>
    protected async Task<PlayPromptOperation> PlayPromptAsync(string callId, List<MediaPrompt> mediaPrompts)
    {
        _logger.LogInformation($"Playing {mediaPrompts.Count} media prompts to call {callId}");
        return await PostDataAndReturnResult<PlayPromptOperation>($"/communications/calls/{callId}/playPrompt", new PlayPromptRequest { Prompts = mediaPrompts });
    }

    protected async Task SubscribeToToneAsync(string callId)
    {
        _logger.LogInformation($"Subscribing to tones for call {callId}");
        await PostData($"/communications/calls/{callId}/subscribeToTone", new ClientContextModel());
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
        var r = await _httpClient.PostAsJsonAsync($"https://graph.microsoft.com/v1.0" + urlMinusRoot, payload);

        var content = await r.Content.ReadAsStringAsync();
        if (!r.IsSuccessStatusCode)
        {
            _logger.LogError($"Error response {r.StatusCode} calling Graph API url {urlMinusRoot}: {content}");
        }
        r.EnsureSuccessStatusCode();

        return content ?? throw new Exception("Unexpected Graph response");
    }

    #endregion
}
