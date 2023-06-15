using Graph.SimpleCallingBot.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.AspNetCore.Http;
using ServiceHostedMediaBot.Extensions;
using Microsoft.Graph;
using System.Net.Http.Json;
using System.Text.Json;
using SimpleCallingBot.Models;

namespace SimpleCallingBot;

/// <summary>
/// A simple, service-hosted bot that can make outbound calls and play prompts.
/// </summary>
public abstract class WebApiGraphCallingBot
{
    protected readonly BotOptions _botOptions;
    protected readonly ILogger _logger;
    private readonly ICallStateManager _callStateManager;
    protected ConfidentialClientApplicationThrottledHttpClient _httpClient;
    private readonly IRequestAuthenticationProvider _authenticationProvider;

    public WebApiGraphCallingBot(BotOptions botOptions, ILogger logger, ICallStateManager callStateManager)
    {
        _botOptions = botOptions;
        _logger = logger;
        _callStateManager = callStateManager;
        _httpClient = new ConfidentialClientApplicationThrottledHttpClient(_botOptions.AppId, _botOptions.AppSecret, _botOptions.TenantId, false, logger);

        var name = this.GetType().Assembly.GetName().Name ?? "CallingBot";
        this._authenticationProvider = new AuthenticationProvider(name, _botOptions.AppId, _botOptions.AppSecret, _logger);
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

    public async Task HandleNotificationsAsync(CommsNotificationsPayload notificationPayload)
    {
        foreach (var callnotification in notificationPayload.CommsNotifications)
        {
            var updateCall = false;
            var callState = await _callStateManager.GetByNotificationResourceId(callnotification.ResourceUrl);

            if (callState != null && callState.HasValidCallId)
            {
                // Is this notification for a call we're tracking?
                if (callnotification.AssociatedCall?.CallChainId != null)
                {
                    // Update call state
                    callState.State = callnotification.AssociatedCall.State;
                    updateCall = true;
                    await HandleCallNotificationAsync(callnotification, callState);

                }
                else if (callnotification.AssociatedCall?.ToneInfo != null)
                {
                    updateCall = true;
                    await HandleToneNotificationAsync(callnotification.AssociatedCall.ToneInfo, callState);
                }

                if (updateCall)
                {
                    await _callStateManager.UpdateByResourceId(callState);
                }
            }
            else
            {
                _logger.LogWarning($"Received notification for unknown call {callnotification.ResourceUrl}");
            }
        }
    }

    private async Task HandleToneNotificationAsync(ToneInfo toneInfo, ActiveCallState callState)
    {
        if (toneInfo.Tone != null)
        {
            callState.TonesPressed.Add(toneInfo.Tone.Value);
            await NewTonePressed(callState, toneInfo.Tone.Value);
        }
        else
        {
            _logger.LogWarning($"Received notification for unknown tone on call {callState.CallId}");
        }
    }

    private async Task HandleCallNotificationAsync(CallNotification callnotification, ActiveCallState callState)
    {
        if (callnotification.ChangeType == CallConstants.NOTIFICATION_TYPE_UPDATED && callnotification.AssociatedCall?.State == CallState.Established)
        {
            _logger.LogInformation($"Call {callState.CallId} connected");
            await CallConnected(callState);
        }
    }

    protected abstract Task CallConnected(ActiveCallState callState);
    protected virtual Task NewTonePressed(ActiveCallState callState, Tone tone) 
    { 
        _logger.LogInformation($"New tone pressed: {tone}");
        return Task.CompletedTask;  
    }

    public async Task<Call> StartNewCall(Call newCall)
    {
        var callCreated = await PostDataAndReturnResult<Call>("/communications/calls", newCall);

        // Remember the call ID for later
        var newCallState = new ActiveCallState(callCreated);
        await _callStateManager.AddCallState(newCallState);

        _logger.LogInformation($"Call {newCallState.CallId} created");
        return callCreated;
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/graph/api/call-playprompt
    /// </summary>
    public async Task<PlayPromptOperation> PlayPromptAsync(string callId, List<MediaPrompt> mediaPrompts)
    {
        _logger.LogInformation($"Playing {mediaPrompts.Count} media prompts to call {callId}");
        return await PostDataAndReturnResult<PlayPromptOperation>($"/communications/calls/{callId}/playPrompt", new PlayPromptRequest { Prompts = mediaPrompts });
    }

    public async Task SubscribeToToneAsync(string callId)
    {
        _logger.LogInformation($"Subscribing to tones for call {callId}");
        await PostData($"/communications/calls/{callId}/subscribeToTone", new ClientContextModel());
    }

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
}
