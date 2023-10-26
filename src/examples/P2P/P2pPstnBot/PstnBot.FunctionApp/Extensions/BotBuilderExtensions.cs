using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using PstnBot.Shared;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace PstnBot.FunctionApp.Extensions;

public static class BotBuilderExtensions
{
    internal static IServiceCollection AddCallingBot(this IServiceCollection services, FunctionsAppCallingBotConfig config)
    {
        services.AddSingleton(config.ToRemoteMediaCallingBotConfiguration(HttpRouteConstants.CallNotificationsRoute));

        // Configure the common config options for engine
        services.AddSingleton(config.ToRemoteMediaCallingBotConfiguration(HttpRouteConstants.CallNotificationsRoute));
        services.AddSingleton(new TableServiceClient(config.Storage));

        // Add our own implementation of the config for specialised configuration option just for this bot
        services.AddSingleton(config);

        // Storage must be Azure Tables. Value isn't optional.
        services.AddSingleton<ICallStateManager<BaseActiveCallState>, AzTablesCallStateManager<BaseActiveCallState>>();
        services.AddSingleton<ICallHistoryManager<BaseActiveCallState>, CosmosCallHistoryManager<BaseActiveCallState>>();

        return services.AddSingleton<IPstnCallingBot, RickrollPstnBot>();
    }
}
