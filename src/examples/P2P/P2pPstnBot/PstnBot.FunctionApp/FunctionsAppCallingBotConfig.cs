using CommonUtils.Config;
using Microsoft.Extensions.Configuration;
using ServiceHostedMediaCallingBot.Engine.Models;

namespace PstnBot.FunctionApp;


internal class FunctionsAppCallingBotConfig : PropertyBoundConfig
{
    public FunctionsAppCallingBotConfig(IConfiguration config) : base(config) { }

    [ConfigValue]
    public string MicrosoftAppId { get; set; } = null!;

    [ConfigValue]
    public string MicrosoftAppPassword { get; set; } = null!;

    [ConfigValue]
    public string TenantId { get; set; } = null!;

    [ConfigValue(true)]
    public string Storage { get; set; } = null!;

    [ConfigValue]
    public string AppInstanceObjectId { get; set; } = null!;

    [ConfigValue]
    public string BotBaseUrl { get; set; } = null!;

    public SingleWavFileBotConfig ToRemoteMediaCallingBotConfiguration(string relativeUrlCallingEndPoint)
    {
        return new SingleWavFileBotConfig
        {
            AppId = MicrosoftAppId,
            AppInstanceObjectId = AppInstanceObjectId,
            AppSecret = MicrosoftAppPassword,
            AppInstanceObjectName = "Rickroll Bot",
            BotBaseUrl = BotBaseUrl,
            CallingEndpoint = BotBaseUrl + relativeUrlCallingEndPoint,
            RelativeWavCallbackUrl = HttpRouteConstants.WavFileRoute,
            TenantId = TenantId
        };
    }
}
