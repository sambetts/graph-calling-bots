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
    public string CosmosConnectionString { get; set; } = null!;


    [ConfigValue(true)]
    public string CosmosDatabaseName { get; set; } = null!;

    [ConfigValue(true)]
    public string ContainerNameCallHistory { get; set; } = null!;

    [ConfigValue(true)]
    public string ContainerNameCallState { get; set; } = null!;

}
