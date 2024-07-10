using Azure.Data.Tables;
using GroupCalls.Common;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceHostedMediaCallingBot.Engine;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;
using ServiceHostedMediaCallingBot.Engine.StateManagement.Sql;

namespace GroupCallingBot.FunctionApp.Extensions;

public static class BotBuilderExtensions
{
    internal static IServiceCollection AddCallingBot(this IServiceCollection services, FunctionsAppCallingBotConfig config)
    {
        services.AddSingleton<RemoteMediaCallingBotConfiguration>(config.ToRemoteMediaCallingBotConfiguration(HttpRouteConstants.CallNotificationsRoute));
        services.AddSingleton<BotCallRedirector>();

        // Use in-memory storage is no storage is configured
        if (!string.IsNullOrEmpty(config.Storage))
        {
            services.AddSingleton(new TableServiceClient(config.Storage));

            services.AddSingleton<ICallStateManager<GroupCallActiveCallState>, AzTablesCallStateManager<GroupCallActiveCallState>>();
            services.AddSingleton<ICallStateManager<GroupCallInviteActiveCallState>, AzTablesCallStateManager<GroupCallInviteActiveCallState>>();
        }
        else
        {
            services.AddSingleton<ICallStateManager<GroupCallActiveCallState>, ConcurrentInMemoryCallStateManager<GroupCallActiveCallState>>();
            services.AddSingleton<ICallStateManager<GroupCallInviteActiveCallState>, ConcurrentInMemoryCallStateManager<GroupCallInviteActiveCallState>>();
        }

        // Prefer SQL storage if configured, then CosmosDb, otherwise use in-memory storage
        if (!string.IsNullOrEmpty(config.SqlCallHistory))
        {
            services.AddDbContext<CallHistorySqlContext<GroupCallActiveCallState, CallNotification>>(options => options
                .UseSqlServer(config.SqlCallHistory)
            );
            services.AddDbContext<CallHistorySqlContext<GroupCallInviteActiveCallState, CallNotification>>(options => options
                .UseSqlServer(config.SqlCallHistory)
            );
            services.AddSingleton<ICallHistoryManager<GroupCallActiveCallState, CallNotification>, SqlCallHistoryManager<GroupCallActiveCallState, CallNotification>>();
            services.AddSingleton<ICallHistoryManager<GroupCallInviteActiveCallState, CallNotification>, SqlCallHistoryManager<GroupCallInviteActiveCallState, CallNotification>>();
        }
        else
        {
            if (!string.IsNullOrEmpty(config.CosmosDb))
            {
                services.AddSingleton(new CosmosClient(config.CosmosDb));
                services.AddSingleton<ICallHistoryManager<GroupCallActiveCallState, CallNotification>, CosmosCallHistoryManager<GroupCallActiveCallState, CallNotification>>();
                services.AddSingleton<ICallHistoryManager<GroupCallInviteActiveCallState, CallNotification>, CosmosCallHistoryManager<GroupCallInviteActiveCallState, CallNotification>>();
            }
            else
            {
                services.AddSingleton<ICallHistoryManager<GroupCallActiveCallState, CallNotification>, ConcurrentInMemoryCallHistoryManager<GroupCallActiveCallState, CallNotification>>();
                services.AddSingleton<ICallHistoryManager<GroupCallInviteActiveCallState, CallNotification>, ConcurrentInMemoryCallHistoryManager<GroupCallInviteActiveCallState, CallNotification>>();
            }
        }

        services.AddSingleton<ICosmosConfig>(config);
        services.AddSingleton<CallInviteBot>();
        return services.AddSingleton<GroupCallBot>();
    }
}
