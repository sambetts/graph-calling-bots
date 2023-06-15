using Azure.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using PstnBot;
using System.Net.Http.Headers;

namespace Graph.SimpleCallingBot.Http;

/// <summary>
/// HttpClient that can handle HTTP 429s automatically
/// </summary>
public class ConfidentialClientApplicationThrottledHttpClient : AutoThrottleHttpClient
{
    public ConfidentialClientApplicationThrottledHttpClient(HttpMessageHandler server, ILogger debugTracer) : base(server, debugTracer)
    {
    }

    public ConfidentialClientApplicationThrottledHttpClient(string clientId, string secret, string tenantId, bool ignoreRetryHeader, ILogger debugTracer)
        : base(ignoreRetryHeader, debugTracer, new ConfidentialClientApplicationHttpHandler(clientId, secret, tenantId))
    {
    }
}

public class ConfidentialClientApplicationHttpHandler : DelegatingHandler
{
    private readonly string _clientId;
    private readonly string _secret;
    private readonly string _tenantId;
    private AuthenticationResult? _auth;
    public ConfidentialClientApplicationHttpHandler(string clientId, string secret, string tenantId)
    {
        InnerHandler = new HttpClientHandler();
        _clientId = clientId;
        _secret = secret;
        _tenantId = tenantId;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_auth == null || _auth.ExpiresOn < DateTimeOffset.Now.AddMinutes(5))
        {
            var app = ConfidentialClientApplicationBuilder.Create(_clientId)
                                              .WithClientSecret(_secret)
                                              .WithAuthority($"https://login.microsoftonline.com/{_tenantId}")
                                              .Build();

            var scopes = new string[] { $".default" };
            var result = app.AcquireTokenForClient(scopes);

            _auth = await result.ExecuteAsync();
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
