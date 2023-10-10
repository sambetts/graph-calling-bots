using CallingTestBot.FunctionApp.Engine;
using Microsoft.Extensions.DependencyInjection;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace CallingTestBot.FunctionApp.Extensions;

public static class BotBuilderExtensions
{
    internal static IServiceCollection AddTestCallingBot(this IServiceCollection services, CallingTestBotConfig config)
    {
        // Configure the common config options for engine
        services.AddSingleton(config.ToRemoteMediaCallingBotConfiguration(HttpRouteConstants.CallNotificationsRoute));

        // Add our own implementation of the config for specialised configuration option just for this bot
        services.AddSingleton(config);

        // Storage must be Azure Tables. Value isn't optional.
        services.AddSingleton<ICallStateManager<BaseActiveCallState>>(new AzTablesCallStateManager<BaseActiveCallState>(config.Storage));

        services.AddSingleton<IBotTestsLogger, AzTablesBotTestsLogger>();
        return services.AddSingleton<IPstnCallingBot, TestCallPstnBot>();
    }
}
