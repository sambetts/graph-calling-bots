using Azure.Data.Tables;
using GroupCalls.Common;
using Microsoft.Extensions.DependencyInjection;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace GroupCallingBot.FunctionApp.Extensions;

public static class BotBuilderExtensions
{
    internal static IServiceCollection AddCallingBot(this IServiceCollection services, FunctionsAppCallingBotConfig config)
    {
        services.AddSingleton<RemoteMediaCallingBotConfiguration>(config.ToRemoteMediaCallingBotConfiguration(HttpRouteConstants.CallNotificationsRoute));
        services.AddSingleton(new TableServiceClient(config.Storage));

        // Use in-memory storage is no storage is configured
        if (!string.IsNullOrEmpty(config.Storage))
        {
            services.AddSingleton<ICallStateManager<GroupCallActiveCallState>, AzTablesCallStateManager<GroupCallActiveCallState>>();
            services.AddSingleton<ICallHistoryManager<GroupCallActiveCallState>, AzTablesCallHistoryManager<GroupCallActiveCallState>>();
        }
        else
        {
            services.AddSingleton<ICallStateManager<GroupCallActiveCallState>, ConcurrentInMemoryCallStateManager<GroupCallActiveCallState>>();
            services.AddSingleton<ICallHistoryManager<GroupCallActiveCallState>, ConcurrentInMemoryCallHistoryManager<GroupCallActiveCallState>>();
        }
            

        return services.AddSingleton<GroupCallStartBot>();
    }
}
