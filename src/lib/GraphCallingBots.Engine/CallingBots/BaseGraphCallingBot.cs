using Azure.Identity;
using GraphCallingBots.Http;
using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Communications.Calls.Item.PlayPrompt;
using Microsoft.Graph.Contracts;
using Microsoft.Graph.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace GraphCallingBots.CallingBots;

/// <summary>
/// Bot that uses Graph API for calling. Contains common methods for calling Graph API.
/// </summary>
public abstract class BaseGraphCallingBot<CALLSTATETYPE> : BaseBot<CALLSTATETYPE>, IGraphCallingBot, ICommsNotificationsPayloadHandler
    where CALLSTATETYPE : BaseActiveCallState, new()
{
    protected readonly GraphServiceClient _graphServiceClient;
    protected ConfidentialClientApplicationThrottledHttpClient _httpClient;     // Used for Graph API calls where there's no native SDK support
    private readonly BotCallRedirector _botCallRedirector;

    public BaseGraphCallingBot(RemoteMediaCallingBotConfiguration botConfig, ICallStateManager<CALLSTATETYPE> callStateManager,
        ICallHistoryManager<CALLSTATETYPE> callHistoryManager, ILogger logger, BotCallRedirector botCallRedirector)
        : base(botConfig, callStateManager, callHistoryManager, logger)
    {
        var clientSecretCredential = new ClientSecretCredential(_botConfig.TenantId, _botConfig.AppId, _botConfig.AppSecret);

        _graphServiceClient = new GraphServiceClient(clientSecretCredential, ["https://graph.microsoft.com/.default"]);
        _httpClient = new ConfidentialClientApplicationThrottledHttpClient(_botConfig.AppId, _botConfig.AppSecret, _botConfig.TenantId, false, logger);
        _botCallRedirector = botCallRedirector;
    }


    /// <summary>
    /// A common way to init the ICallStateManager and create a call request
    /// </summary>
    protected async Task<Call> CreateCallRequest(InvitationParticipantInfo? initialAdd, bool addBotIdentityForPSTN)
    {
        return await CreateCallRequest(initialAdd, new List<MediaInfo>(), addBotIdentityForPSTN, false);
    }


    /// <summary>
    /// A common way to init the ICallStateManager and create a call request. Also optionally tests if the WAV file exists.
    /// </summary>
    protected async Task<Call> CreateCallRequest(InvitationParticipantInfo? initialAdd, MediaInfo mediaPromptOnCallConnected, bool addBotIdentityForPSTN, bool testMedia)
    {
        return await CreateCallRequest(initialAdd, new List<MediaInfo> { mediaPromptOnCallConnected }, addBotIdentityForPSTN, testMedia);
    }

    /// <summary>
    /// A common way to init the ICallStateManager and create a call request. Also optionally tests if WAV files exists.
    /// </summary>
    protected async Task<Call> CreateCallRequest(InvitationParticipantInfo? initialAdd, List<MediaInfo> callMediaList, bool addBotIdentityForPSTN, bool testMedia)
    {
        if (!_callStateManager.Initialised)
        {
            await _callStateManager.Initialise();
        }

        var defaultMediaConfig = new ServiceHostedMediaConfig { PreFetchMedia = new List<MediaInfo>() };

        // We test each media item before making a call. No point calling in silence
        foreach (var defaultMedia in callMediaList)
        {
            bool fileExists = await TestMediaExists(defaultMedia.Uri);
            if (!fileExists)
            {
                throw new ArgumentOutOfRangeException(nameof(defaultMedia), $"Media file {defaultMedia.Uri} does not exist. Aborting call");
            }
            defaultMediaConfig = new ServiceHostedMediaConfig { PreFetchMedia = new List<MediaInfo> { defaultMedia } };
        }
        if (callMediaList.Count == 0 && testMedia)
        {
            _logger.LogInformation($"No media URI found for call. Won't play any initial message via bot.");
        }

        // Create call for initial participants
        var newCall = new Call
        {
            MediaConfig = defaultMediaConfig,
            RequestedModalities = new List<Modality?> { Modality.Audio },
            TenantId = _botConfig.TenantId,
            CallbackUri = _botConfig.CallingEndpoint,
            Direction = CallDirection.Outgoing
        };

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

        newCall.Targets = new List<InvitationParticipantInfo>();
        if (initialAdd != null)
        {
            newCall.Targets.Add(initialAdd);
        }

        return newCall;
    }

    protected async Task<bool> TestMediaExists(string? uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return false;
        }

        try
        {
            var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri));
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug($"Validated media url: {uri}");
            }
            else
            {
                _logger.LogError($"Media file {uri} does not exist.");
            }
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    #region Bot App Actions

    /// <summary>
    /// Create call with Graph API; logs error if fails.
    /// </summary>
    protected async Task<Call?> CreateNewCall(Call newCallRequest)
    {
        _logger.LogInformation($"{BotTypeName}: Creating new call with Graph API...");
        _logger.LogDebug($"{BotTypeName}: Media info: {JsonSerializer.Serialize(newCallRequest.MediaConfig)}");
        try
        {
            var callCreated = await _graphServiceClient.Communications.Calls.PostAsync(newCallRequest);

            if (callCreated?.Id != null)
            {
                _logger.LogInformation($"{BotTypeName}: Call {callCreated.Id} created");
                _botCallRedirector.AddCall(callCreated.Id, this);
            }
            else
            {
                _logger.LogWarning($"{BotTypeName}: Call created but no call ID returned?");
            }
            return callCreated;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"{BotTypeName}: Can't create call: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/graph/api/call-playprompt
    /// </summary>
    protected async Task<PlayPromptOperation?> PlayPromptAsync(BaseActiveCallState callState, EquatableMediaPrompt mediaPrompt)
    {
        _logger.LogInformation($"{BotTypeName}: Playing {mediaPrompt?.MediaInfo?.Uri} media prompt on call {callState.CallId}");

        callState.MediaPromptsPlaying.Add(mediaPrompt);
        return await _graphServiceClient.Communications.Calls[callState.CallId].PlayPrompt.PostAsync(
            new PlayPromptPostRequestBody
            {
                Prompts = new List<Prompt> { mediaPrompt }
            });
    }

    protected async Task SubscribeToToneAsync(string callId)
    {
        _logger.LogInformation($"{BotTypeName}: Subscribing to tones for call {callId}");
        await PostData($"/communications/calls/{callId}/subscribeToTone", new EmptyModelWithClientContext());
    }


    /// <summary>
    /// https://learn.microsoft.com/en-us/graph/api/call-delete
    /// </summary>
    protected async Task HangUp(string callId)
    {
        _logger.LogInformation($"{BotTypeName}: Hanging up call {callId}");
        await Delete($"/communications/calls/{callId}");
    }

    #endregion

    #region HTTP Calls

    // Calls for when Graph SDK doesn't support the API call needed. Will one day be deprecated in favour of SDK calls.
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
