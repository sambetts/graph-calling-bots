﻿using Azure.Identity;
using Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;

namespace Bot.Extensions;

public static class GraphConfiguration
{
    public static IServiceCollection ConfigureGraphClient(this IServiceCollection services, Config config)
    {
        var options = new ClientSecretCredentialOptions
        {
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
        };
        var clientSecretCredential = new ClientSecretCredential(config.TenantId, config.MicrosoftAppId, config.MicrosoftAppPassword, options);

        var scopes = new[] { "https://graph.microsoft.com/.default" };

        services.AddScoped(sp =>
        {
            return new GraphServiceClient(clientSecretCredential, scopes);
        });

        return services;
    }
}