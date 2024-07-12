using Microsoft.Extensions.Logging;
using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Communications.Common;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace GraphCallingBots.Http;

/// <summary>
/// Ensures call notifications from Graph are real and authenticated correctly.
/// </summary>
public class AuthenticationProvider : IRequestAuthenticationProvider
{

    private readonly string _appName;
    private readonly string _appId;
    private readonly string _appSecret;

    private readonly TimeSpan _openIdConfigRefreshInterval = TimeSpan.FromHours(2);
    private readonly ILogger _logger;
    private DateTime _prevOpenIdConfigUpdateTimestamp = DateTime.MinValue;

    private OpenIdConnectConfiguration? openIdConnectConfiguration = null;

    public AuthenticationProvider(string appName, string appId, string appSecret, ILogger logger)
    {
        _appName = appName.NotNullOrWhitespace(nameof(appName));
        _appId = appId.NotNullOrWhitespace(nameof(appId));
        _appSecret = appSecret.NotNullOrWhitespace(nameof(appSecret));
        _logger = logger;
    }

    public async Task AuthenticateOutboundRequestAsync(HttpRequestMessage request, string tenant)
    {
        const string schema = "Bearer";
        const string resource = "https://graph.microsoft.com/.default";

        tenant = string.IsNullOrWhiteSpace(tenant) ? "common" : tenant;

        _logger.LogTrace($"{nameof(AuthenticationProvider)}: Generating OAuth token.");

        var app = ConfidentialClientApplicationBuilder.Create(_appId)
                              .WithClientSecret(_appSecret)
                              .WithAuthority($"https://login.microsoftonline.com/{tenant}")
                              .Build();

        var scopes = new string[] { resource };
        var result = app.AcquireTokenForClient(scopes);

        var auth = await result.ExecuteAsync();

        _logger.LogTrace($"Authentication Provider: Generated OAuth token. Expires in {auth.ExpiresOn.Subtract(DateTimeOffset.UtcNow).TotalMinutes} minutes.");

        request.Headers.Authorization = new AuthenticationHeaderValue(schema, auth.AccessToken);
    }

    [Obsolete]
    public async Task<RequestValidationResult> ValidateInboundRequestAsync(HttpRequestMessage request)
    {
        var token = request.Headers?.Authorization?.Parameter;
        if (string.IsNullOrWhiteSpace(token))
        {
            return new RequestValidationResult { IsValid = false };
        }

        const string authDomain = "https://api.aps.skype.com/v1/.well-known/OpenIdConfiguration";
        if (openIdConnectConfiguration == null || DateTime.Now > _prevOpenIdConfigUpdateTimestamp.Add(_openIdConfigRefreshInterval))
        {
            _logger.LogTrace("Updating OpenID configuration");

            IConfigurationManager<OpenIdConnectConfiguration> configurationManager =
                new ConfigurationManager<OpenIdConnectConfiguration>(

                    authDomain,
                    new OpenIdConnectConfigurationRetriever());
            openIdConnectConfiguration = await configurationManager.GetConfigurationAsync(CancellationToken.None).ConfigureAwait(false);

            _prevOpenIdConfigUpdateTimestamp = DateTime.Now;
        }

        var authIssuers = new[]
        {
            "https://graph.microsoft.com",
            "https://api.botframework.com",
        };

        TokenValidationParameters validationParameters = new TokenValidationParameters
        {
            ValidIssuers = authIssuers,
            ValidAudience = _appId,
            IssuerSigningKeys = openIdConnectConfiguration.SigningKeys,
        };

        ClaimsPrincipal claimsPrincipal;
        try
        {
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            claimsPrincipal = handler.ValidateToken(token, validationParameters, out _);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to validate token for client: {_appId}.");
            return new RequestValidationResult() { IsValid = false };
        }

        const string ClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";
        var tenantClaim = claimsPrincipal.FindFirst(claim => claim.Type.Equals(ClaimType, StringComparison.Ordinal));

        if (string.IsNullOrEmpty(tenantClaim?.Value))
        {
            return new RequestValidationResult { IsValid = false };
        }

        request.Properties.Add(HttpConstants.HeaderNames.Tenant, tenantClaim.Value);
        return new RequestValidationResult { IsValid = true, TenantId = tenantClaim.Value };
    }

}
