using Azure.Data.Tables;
using GraphCallingBots;
using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
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
        services.AddSingleton<BotCallRedirector>();
        services.AddSingleton<GroupCallOrchestrator>();

        // Use in-memory storage is no storage is configured
        if (!string.IsNullOrEmpty(config.Storage))
        {
            services.AddSingleton(new TableServiceClient(config.Storage));

            services.AddSingleton<ICallStateManager<BaseActiveCallState>, AzTablesCallStateManager<BaseActiveCallState>>();
            services.AddSingleton<ICallStateManager<GroupCallInviteActiveCallState>, AzTablesCallStateManager<GroupCallInviteActiveCallState>>();
        }
        else
        {
            services.AddSingleton<ICallStateManager<BaseActiveCallState>, ConcurrentInMemoryCallStateManager<BaseActiveCallState>>();
            services.AddSingleton<ICallStateManager<GroupCallInviteActiveCallState>, ConcurrentInMemoryCallStateManager<GroupCallInviteActiveCallState>>();
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
            if (!string.IsNullOrEmpty(config.CosmosDb))
            {
                services.AddSingleton(new CosmosClient(config.CosmosDb));
                services.AddSingleton<ICallHistoryManager<BaseActiveCallState>, CosmosCallHistoryManager<BaseActiveCallState>>();
                services.AddSingleton<ICallHistoryManager<GroupCallInviteActiveCallState>, CosmosCallHistoryManager<GroupCallInviteActiveCallState>>();
            }
            else
            {
                services.AddSingleton<ICallHistoryManager<BaseActiveCallState>, ConcurrentInMemoryCallHistoryManager<BaseActiveCallState>>();
                services.AddSingleton<ICallHistoryManager<GroupCallInviteActiveCallState>, ConcurrentInMemoryCallHistoryManager<GroupCallInviteActiveCallState>>();
            }
        }

        services.AddSingleton<ICosmosConfig>(config);
        services.AddSingleton<CallInviteBot>();
        return services.AddSingleton<GroupCallBot>();
    }
}
