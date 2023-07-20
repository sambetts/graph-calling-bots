using Microsoft.Extensions.DependencyInjection;
using PstnBot.Shared;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace PstnBot.FunctionApp.Extensions;

public static class BotBuilderExtensions
{

    public static IServiceCollection AddCallingBot(this IServiceCollection services, CallingBotConfig config)
    {
        services.AddSingleton(config.ToRemoteMediaCallingBotConfiguration(HttpRouteConstants.CallNotificationsRoute));

        // Use in-memory storage for the call state for now
        services.AddSingleton<ICallStateManager<BaseActiveCallState>, ConcurrentInMemoryCallStateManager<BaseActiveCallState>>();

        return services.AddSingleton<RickrollPstnBot>();
    }

}
