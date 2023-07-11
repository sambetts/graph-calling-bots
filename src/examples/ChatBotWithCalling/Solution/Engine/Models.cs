using CommonUtils.Config;
using Microsoft.Extensions.Configuration;
using SimpleCallingBotEngine.Models;

namespace Engine;


public class MeetingState
{
    public DateTime Created { get; set; }
    public string MeetingUrl { get; set; } = null!;

    public bool IsMeetingCreated => !string.IsNullOrEmpty(MeetingUrl);
    public List<NumberCallState> Numbers { get; set; } = new();

}

public class NumberCallState
{
    public string Number { get; set; } = null!;

    public static bool IsValidNumber(string number)
    {
        return !string.IsNullOrEmpty(number);
    }
}


public class Config : PropertyBoundConfig
{
    public Config(IConfiguration config) : base(config)
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

