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

        // Use in-memory storage is no storage is configured
        if (!string.IsNullOrEmpty(config.Storage))
            services.AddSingleton<ICallStateManager<BaseActiveCallState>>(new AzTablesCallStateManager<BaseActiveCallState>(config.Storage));
        
        else 
            services.AddSingleton<ICallStateManager<BaseActiveCallState>, ConcurrentInMemoryCallStateManager<BaseActiveCallState>>();

        return services.AddSingleton<IPstnCallingBot, RickrollPstnBot>();
    }
}
