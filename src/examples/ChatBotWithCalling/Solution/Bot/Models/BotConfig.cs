using CommonUtils.Config;
using Microsoft.Extensions.Configuration;
using ServiceHostedMediaCallingBot.Engine.Models;

namespace GroupCallingChatBot.Models;


public class BotConfig : PropertyBoundConfig
{
    public BotConfig(IConfiguration config) : base(config)
    {
    }

    [ConfigValue]
    public string MicrosoftAppId { get; set; } = null!;

    [ConfigValue]
    public string MicrosoftAppPassword { get; set; } = null!;

    [ConfigValue]
    public string TenantId { get; set; } = null!;

    [ConfigValue(true)]
    public string AppInsightsInstrumentationKey { get; set; } = null!;

    [ConfigValue(true)]
    public string Storage { get; set; } = null!;

    [ConfigValue]
    public string AppInstanceObjectId { get; set; } = null!;

    [ConfigValue]
    public string BotBaseUrl { get; set; } = null!;

    public RemoteMediaCallingBotConfiguration ToRemoteMediaCallingBotConfiguration(string relativeUrlCallingEndPoint)
    {
        return new RemoteMediaCallingBotConfiguration
        {
            AppId = MicrosoftAppId,
            AppInstanceObjectId = AppInstanceObjectId,
            AppSecret = MicrosoftAppPassword,
            AppInstanceObjectName = "CallAndRedirectBot",
            BotBaseUrl = BotBaseUrl,
            CallingEndpoint = BotBaseUrl + relativeUrlCallingEndPoint,
            TenantId = TenantId
        };
    }
}
