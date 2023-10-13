using CommonUtils.Config;
using Microsoft.Extensions.Configuration;
using ServiceHostedMediaCallingBot.Engine.Models;

namespace GroupCallingChatBot.Web.Models;

/// <summary>
/// Config in default Bot Framework SDK format that's also used for the calling bot.
/// </summary>
public class TeamsChatbotBotConfig : PropertyBoundConfig
{
    public TeamsChatbotBotConfig(IConfiguration config) : base(config)
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

    public SingleWavFileBotConfig ToRemoteMediaCallingBotConfiguration(string relativeUrlCallingEndPoint)
    {
        return new SingleWavFileBotConfig
        {
            AppId = MicrosoftAppId,
            AppInstanceObjectId = AppInstanceObjectId,
            AppSecret = MicrosoftAppPassword,
            AppInstanceObjectName = "CallAndRedirectBot",
            BotBaseUrl = BotBaseUrl,
            CallingEndpoint = BotBaseUrl + relativeUrlCallingEndPoint,
            TenantId = TenantId,
            RelativeWavCallbackUrl = "/api/CallAndRedirectBot/GetWavFile",
        };
    }
}
