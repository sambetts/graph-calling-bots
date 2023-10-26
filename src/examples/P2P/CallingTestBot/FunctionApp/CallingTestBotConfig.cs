using CommonUtils.Config;
using Microsoft.Extensions.Configuration;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace CallingTestBot.FunctionApp;

public class CallingTestBotConfig : PropertyBoundConfig, ICosmosConfig
{
    public CallingTestBotConfig()
    {
    }

    public CallingTestBotConfig(IConfiguration config) : base(config) { }

    [ConfigValue]
    public string MicrosoftAppId { get; set; } = null!;

    [ConfigValue]
    public string MicrosoftAppPassword { get; set; } = null!;

    [ConfigValue]
    public string TenantId { get; set; } = null!;

    [ConfigValue]
    public string Storage { get; set; } = null!;


    [ConfigValue(true)]
    public string CosmosDb { get; set; } = null!;

    [ConfigValue]
    public string AppInstanceObjectId { get; set; } = null!;

    [ConfigValue]
    public string BotBaseUrl { get; set; } = null!;


    [ConfigValue]
    public string TestNumber { get; set; } = null!;

    [ConfigValue(true)]
    public string DatabaseName { get; set; } = null!;

    [ConfigValue(true)]
    public string ContainerName { get; set; } = null!;

    public SingleWavFileBotConfig ToRemoteMediaCallingBotConfiguration(string relativeUrlCallingEndPoint)
    {
        return new SingleWavFileBotConfig
        {
            AppId = MicrosoftAppId,
            AppInstanceObjectId = AppInstanceObjectId,
            AppSecret = MicrosoftAppPassword,
            AppInstanceObjectName = "Test Call Bot",
            BotBaseUrl = BotBaseUrl,
            CallingEndpoint = BotBaseUrl + relativeUrlCallingEndPoint,
            RelativeWavCallbackUrl = HttpRouteConstants.WavFileRoute,
            TenantId = TenantId
        };
    }
}
