using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;

namespace GraphCallingBots.Http;

/// <summary>
/// HttpClient that can handle HTTP 429s automatically + handles token acquisition for MS Graph
/// </summary>
public class ConfidentialClientApplicationThrottledHttpClient : AutoThrottleHttpClient
{
    public ConfidentialClientApplicationThrottledHttpClient(HttpMessageHandler server, ILogger debugTracer) : base(server, debugTracer)
    {
    }

    public ConfidentialClientApplicationThrottledHttpClient(string clientId, string secret, string tenantId, bool ignoreRetryHeader, ILogger debugTracer)
        : base(ignoreRetryHeader, debugTracer, new ConfidentialClientApplicationHttpHandler(clientId, secret, tenantId, debugTracer))
    {
    }
}

public class ConfidentialClientApplicationHttpHandler : DelegatingHandler
{
    private readonly string _clientId;
    private readonly string _secret;
    private readonly string _tenantId;
    private readonly ILogger _logger;
    private AuthenticationResult? _auth;
    public ConfidentialClientApplicationHttpHandler(string clientId, string secret, string tenantId, ILogger logger)
    {
        InnerHandler = new HttpClientHandler();
        _clientId = clientId;
        _secret = secret;
        _tenantId = tenantId;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"HTTP {request.Method} {request.RequestUri}");
        var body = request.Content?.ReadAsStringAsync();
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
