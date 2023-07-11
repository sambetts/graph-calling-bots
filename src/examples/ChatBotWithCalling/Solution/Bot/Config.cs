using CommonUtils.Config;
using Microsoft.Extensions.Configuration;

namespace Bot;

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
