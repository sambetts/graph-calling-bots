using Azure.Identity;
using Engine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using SimpleCallingBotEngine;
using SimpleCallingBotEngine.Models;
using UnitTests.FakeService;

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
    public async Task FakeTests()
    {
        var fakeTeamsChatbotManager = new FakeTeamsChatbotManager();    
        var url = await fakeTeamsChatbotManager.CreateNewMeeting(_config.ToRemoteMediaCallingBotConfiguration(""));
        Assert.AreEqual("123", url);

        var meeting = new MeetingRequest();

        await meeting.CreateMeeting(fakeTeamsChatbotManager);

        await meeting.AddNumber("555", fakeTeamsChatbotManager);
        Assert.IsTrue(meeting.Attendees.Any());
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

        var bot = new GroupCallBot(new FakeTeamsChatbotManager(), 
            _config.ToRemoteMediaCallingBotConfiguration(""),
            new ConcurrentInMemoryCallStateManager(), LoggerFactory.Create(config =>
        {
            config.AddConsole();
            config.SetMinimumLevel(LogLevel.Debug);
        }).CreateLogger< GroupCallBot>());

        var manager = new GraphTeamsChatbotManager(graphClient, _loggerFactory.CreateLogger<GraphTeamsChatbotManager>());

        //var call = await manager.GroupCall(meeting);

    }
}
