using CommonUtils.Config;
using GraphCallingBots.StateManagement;
using Microsoft.Extensions.Configuration;

namespace GraphCallingBots.UnitTests;

public class UnitTestConfig : PropertyBoundConfig, ICosmosConfig
{
    public UnitTestConfig()
    {
    }

    public UnitTestConfig(IConfiguration config) : base(config) { }

    [ConfigValue(true)]
    public string CosmosDb { get; set; } = null!;


    [ConfigValue(true)]
    public string DatabaseName { get; set; } = null!;

    [ConfigValue(true)]
    public string ContainerName { get; set; } = null!;

}
