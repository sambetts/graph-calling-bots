using Azure.Data.Tables;
using CallingTestBot.FunctionApp.Engine;
using GraphCallingBots.CallingBots;
using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace CallingTestBot.FunctionApp.Extensions;

public static class BotBuilderExtensions
{
    internal static IServiceCollection AddTestCallingBot(this IServiceCollection services, CallingTestBotConfig config)
    {
        // Configure the common config options for engine
        services.AddSingleton(config.ToRemoteMediaCallingBotConfiguration(HttpRouteConstants.CallNotificationsRoute));
        services.AddSingleton(new TableServiceClient(config.Storage));

        // Add our own implementation of the config for specialised configuration option just for this bot
        services.AddSingleton<ICosmosConfig>(config);

        // Storage must be Azure Tables. Value isn't optional.
        services.AddSingleton<ICallStateManager<BaseActiveCallState>, AzTablesCallStateManager<BaseActiveCallState>>();

        var cosmosClient = new CosmosClient(config.CosmosDb);
        services.AddSingleton<ICallHistoryManager<BaseActiveCallState, CallNotification>, CosmosCallHistoryManager<BaseActiveCallState, CallNotification>>();

        services.AddSingleton<IBotTestsLogger, AzTablesBotTestsLogger>();
        return services.AddSingleton<IPstnCallingBot, TestCallPstnBot>();
    }
}
