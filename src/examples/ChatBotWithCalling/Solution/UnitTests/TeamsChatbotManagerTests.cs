using Azure.Identity;
using Engine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using SimpleCallingBotEngine;
using UnitTests.FakeService;

namespace UnitTests;

[TestClass]
public class TeamsChatbotManagerTests
{
    protected Config _config = null!;
    protected ILogger _tracer = null!;

    [TestInitialize]
    public void Init()
    {
        var builder = new ConfigurationBuilder()
            .AddUserSecrets<TeamsChatbotManagerTests>();

        var config = builder.Build();


        _tracer = LoggerFactory.Create(config =>
        {
            config.AddConsole();
            config.SetMinimumLevel(LogLevel.Debug);
        }).CreateLogger("Unit tests");

        config = builder.Build();
        _config = new Config(config);

    }


    [TestMethod]
    public async Task FakeTests()
    {
        var fakeTeamsChatbotManager = new FakeTeamsChatbotManager();    
        var url = await fakeTeamsChatbotManager.CreateNewMeeting();
        Assert.AreEqual("123", url);

        var meeting = new MeetingState();
        Assert.IsFalse(meeting.IsMeetingCreated);

        await meeting.CreateMeeting(fakeTeamsChatbotManager);
        Assert.IsTrue(meeting.IsMeetingCreated);

        await meeting.AddNumber("555", fakeTeamsChatbotManager);
        Assert.IsTrue(meeting.Numbers.Any());
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

        var bot = new CallAndRedirectBot(new FakeTeamsChatbotManager(), 
            new SimpleCallingBotEngine.Models.RemoteMediaCallingBotConfiguration { },
            new ConcurrentInMemoryCallStateManager(), LoggerFactory.Create(config =>
        {
            config.AddConsole();
            config.SetMinimumLevel(LogLevel.Debug);
        }).CreateLogger< CallAndRedirectBot>());

        var m = new GraphTeamsChatbotManager(graphClient, bot);

        await m.Transfer(new ActiveCallState());
    }
}
