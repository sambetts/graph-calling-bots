using Azure.Identity;
using Engine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using SimpleCallingBotEngine;

namespace UnitTests;

[TestClass]
public class TeamsChatbotManagerTests
{
    protected Config _config = null!;
    protected ILoggerFactory _loggerFactory = null!;

    [TestInitialize]
    public void Init()
    {
        var builder = new ConfigurationBuilder()
            .AddUserSecrets<TeamsChatbotManagerTests>();

        var config = builder.Build();


        _loggerFactory = LoggerFactory.Create(config =>
        {
            config.AddConsole();
            config.SetMinimumLevel(LogLevel.Debug);
        });

        config = builder.Build();
        _config = new Config(config);

    }

    [TestMethod]
    public async Task GraphTeamsChatbotManagerTests()
    {
        var options = new ClientSecretCredentialOptions
        {
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
        };
        var clientSecretCredential = new ClientSecretCredential(_config.TenantId, _config.MicrosoftAppId, _config.MicrosoftAppPassword, options);

        var scopes = new[] { "https://graph.microsoft.com/.default" };
        var graphClient = new GraphServiceClient(clientSecretCredential, scopes);

        var bot = new GroupCallBot(_config.ToRemoteMediaCallingBotConfiguration(""),
            new ConcurrentInMemoryCallStateManager<GroupCallActiveCallState>(), LoggerFactory.Create(config =>
        {
            config.AddConsole();
            config.SetMinimumLevel(LogLevel.Debug);
        }).CreateLogger< GroupCallBot>());

    }
}
