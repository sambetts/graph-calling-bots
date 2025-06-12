using Azure.Data.Tables;
using Azure.Messaging.ServiceBus;
using GraphCallingBots;
using GraphCallingBots.CallingBots;
using GraphCallingBots.EventQueue;
using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using GraphCallingBots.StateManagement.Cosmos;
using GraphCallingBots.StateManagement.Sql;
using GroupCalls.Common;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GroupCallingBot.FunctionApp.Extensions;

public static class BotBuilderExtensions
{
    internal static IServiceCollection AddCallingBot(this IServiceCollection services, FunctionsAppCallingBotConfig config)
    {
        services.AddSingleton<RemoteMediaCallingBotConfiguration>(config.ToRemoteMediaCallingBotConfiguration(HttpRouteConstants.CallNotificationsRoute));
        services.AddSingleton<GroupCallOrchestrator>();

        // Use in-memory storage is no storage is configured
        if (!string.IsNullOrEmpty(config.Storage))
        {
            services.AddSingleton(new TableServiceClient(config.Storage));
        }

        if (!string.IsNullOrEmpty(config.CosmosConnectionString))
        {
            services.AddSingleton(new CosmosClient(config.CosmosConnectionString));
            services.AddSingleton<ICallStateManager<BaseActiveCallState>, CosmosCallStateManager<BaseActiveCallState>>();
            services.AddSingleton<ICallStateManager<GroupCallInviteActiveCallState>, CosmosCallStateManager<GroupCallInviteActiveCallState>>();
        }
        else
        {
            throw new ArgumentException("Persistent call state manager is required for function apps, but no storage is configured.");
        }


        if (!string.IsNullOrEmpty(config.ServiceBusRootConnectionString))
        {
            services.AddSingleton(new ServiceBusClient(config.ServiceBusRootConnectionString));
            services.AddSingleton<IJsonQueueAdapter<CommsNotificationsPayload>, GraphUpdatesAzureServiceBusJsonQueueAdapter>();
            services.AddSingleton<MessageQueueManager<CommsNotificationsPayload>>();
        }
        else
        {
            throw new ArgumentException("Service bus is required");
        }

        // Prefer SQL storage if configured, then CosmosDb, otherwise use in-memory storage
        if (!string.IsNullOrEmpty(config.SqlCallHistory))
        {
            services.AddDbContext<CallHistorySqlContext<BaseActiveCallState>>(options => options
                .UseSqlServer(config.SqlCallHistory)
            );
            services.AddDbContext<CallHistorySqlContext<GroupCallInviteActiveCallState>>(options => options
                .UseSqlServer(config.SqlCallHistory)
            );
            services.AddSingleton<ICallHistoryManager<BaseActiveCallState>, SqlCallHistoryManager<BaseActiveCallState>>();
            services.AddSingleton<ICallHistoryManager<GroupCallInviteActiveCallState>, SqlCallHistoryManager<GroupCallInviteActiveCallState>>();
        }
        else
        {
            services.AddSingleton<ICallHistoryManager<BaseActiveCallState>, ConcurrentInMemoryCallHistoryManager<BaseActiveCallState>>();
            services.AddSingleton<ICallHistoryManager<GroupCallInviteActiveCallState>, ConcurrentInMemoryCallHistoryManager<GroupCallInviteActiveCallState>>();
        }

        services.AddSingleton<ICosmosConfig>(config);


        services.AddSingleton<BotCallRedirector<CallInviteBot, GroupCallInviteActiveCallState>>();
        services.AddSingleton<BotCallRedirector<GroupCallBot, BaseActiveCallState>>();

        services.AddSingleton<CallInviteBot>();
        return services.AddSingleton<GroupCallBot>();
    }
}
