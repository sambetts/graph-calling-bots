using CommonUtils.Config;
using Microsoft.Extensions.Configuration;
using ServiceHostedMediaCallingBot.Engine.Models;

namespace CallingTestBot.FunctionApp;

public class CallingTestBotConfig : PropertyBoundConfig
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

    [ConfigValue]
    public string AppInstanceObjectId { get; set; } = null!;

    [ConfigValue]
    public string BotBaseUrl { get; set; } = null!;


    [ConfigValue]
    public string TestNumber { get; set; } = null!;

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
            RelativeWavUrl = HttpRouteConstants.WavFileRoute,
            TenantId = TenantId
        };
    }
}
