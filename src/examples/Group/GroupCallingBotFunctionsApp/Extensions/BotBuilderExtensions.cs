using Azure.Data.Tables;
using GroupCalls.Common;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace GroupCallingBot.FunctionApp.Extensions;

public static class BotBuilderExtensions
{
    internal static IServiceCollection AddCallingBot(this IServiceCollection services, FunctionsAppCallingBotConfig config)
    {
        services.AddSingleton<RemoteMediaCallingBotConfiguration>(config.ToRemoteMediaCallingBotConfiguration(HttpRouteConstants.CallNotificationsRoute));

        // Use in-memory storage is no storage is configured
        if (!string.IsNullOrEmpty(config.Storage))
        {
            services.AddSingleton(new TableServiceClient(config.Storage));

            services.AddSingleton<ICallStateManager<GroupCallActiveCallState>, AzTablesCallStateManager<GroupCallActiveCallState>>();
        }
        else
        {
            services.AddSingleton<ICallStateManager<GroupCallActiveCallState>, ConcurrentInMemoryCallStateManager<GroupCallActiveCallState>>();
        }

        if (!string.IsNullOrEmpty(config.CosmosDb))
        {
            services.AddSingleton(new CosmosClient(config.CosmosDb));
            services.AddSingleton<ICallHistoryManager<GroupCallActiveCallState, CallNotification>, CosmosCallHistoryManager<GroupCallActiveCallState, CallNotification>>();
        }
        else
        {
            services.AddSingleton<ICallHistoryManager<GroupCallActiveCallState, CallNotification>, ConcurrentInMemoryCallHistoryManager<GroupCallActiveCallState, CallNotification>>();
        }
        services.AddSingleton<ICosmosConfig>(config);

        return services.AddSingleton<GroupCallBot>();
    }
}
