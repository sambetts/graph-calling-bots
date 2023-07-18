using CommonUtils.Config;
using Microsoft.Extensions.Configuration;
using SimpleCallingBotEngine.Models;

namespace Engine;


public class MeetingRequest
{
    public List<AttendeeCallInfo> Attendees { get; set; } = new();

}

public class AttendeeCallInfo
{
    public string Id { get; set; } = null!;
    public string DisplayId { get; set; } = null!;
    public AttendeeType Type { get; set; }
}
public enum AttendeeType
{
    Unknown,
    Phone,
    Teams
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

