using CommonUtils.Config;
using Microsoft.Extensions.Configuration;

namespace Engine;


public class MeetingState
{
    public DateTime Created { get; set; }
    public string MeetingUrl { get; set; } = string.Empty;

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
    public string MicrosoftAppId { get; set; } = string.Empty;

    [ConfigValue]
    public string MicrosoftAppPassword { get; set; } = string.Empty;

    [ConfigValue]
    public string TenantId { get; set; } = string.Empty;

    [ConfigValue(true)]
    public string AppInsightsInstrumentationKey { get; set; } = string.Empty;

    [ConfigValue(true)]
    public string Storage { get; set; } = string.Empty;

}

