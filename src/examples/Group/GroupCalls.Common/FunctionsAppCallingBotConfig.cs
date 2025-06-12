using CommonUtils.Config;
using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using Microsoft.Extensions.Configuration;

namespace GroupCallingBot.FunctionApp;


public class FunctionsAppCallingBotConfig : PropertyBoundConfig, ICosmosConfig
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


    [ConfigValue(true)]
    public string CosmosConnectionString { get; set; } = null!;

    [ConfigValue]
    public string ServiceBusRootConnectionString { get; set; } = null!;


    [ConfigValue(true)]
    public string SqlCallHistory { get; set; } = null!;


    [ConfigValue(true)]
    public string CosmosDatabaseName { get; set; } = null!;

    [ConfigValue(true)]
    public string ContainerNameCallHistory { get; set; } = null!;

    [ConfigValue(true)]
    public string ContainerNameCallState { get; set; } = null!;

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
            AppInstanceObjectName = "Group Call Bot",
            BotBaseUrl = BotBaseUrl,
            CallingEndpoint = BotBaseUrl + relativeUrlCallingEndPoint,
            RelativeWavCallbackUrl = HttpRouteConstants.WavFileInviteToCallRoute,
            TenantId = TenantId
        };
    }
}
