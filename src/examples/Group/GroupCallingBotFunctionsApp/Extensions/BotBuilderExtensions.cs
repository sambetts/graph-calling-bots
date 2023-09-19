using Microsoft.Extensions.DependencyInjection;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace GroupCallingBotFunctionsApp.FunctionApp.Extensions;

public static class BotBuilderExtensions
{
    internal static IServiceCollection AddCallingBot(this IServiceCollection services, FunctionsAppCallingBotConfig config)
    {
        services.AddSingleton(config.ToRemoteMediaCallingBotConfiguration(HttpRouteConstants.CallNotificationsRoute));

        // Use in-memory storage is no storage is configured
        if (!string.IsNullOrEmpty(config.Storage))
            services.AddSingleton<ICallStateManager<GroupCallActiveCallState>>(new AzTablesCallStateManager<GroupCallActiveCallState>(config.Storage));

        else
            services.AddSingleton<ICallStateManager<GroupCallActiveCallState>, ConcurrentInMemoryCallStateManager<GroupCallActiveCallState>>();

        return services.AddSingleton<GroupCallingBot>();
    }
}
