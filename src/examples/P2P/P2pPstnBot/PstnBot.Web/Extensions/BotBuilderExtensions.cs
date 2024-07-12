using GraphCallingBots.CallingBots;
using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using PstnBot.Shared;

namespace PstnBot.Web.Extensions;

public static class BotBuilderExtensions
{
    public static IServiceCollection AddCallingBot(this IServiceCollection services)
        => services.AddCallingBot(_ => { });

    public static IServiceCollection AddCallingBot(this IServiceCollection services, Action<SingleWavFileBotConfig> botOptionsAction)
    {
        var options = new SingleWavFileBotConfig();
        botOptionsAction(options);
        options.CallingEndpoint = options.BotBaseUrl + HttpRouteConstants.OnIncomingRequestRoute;
        services.AddSingleton(options);


        // Just use in-memory storage for this example
        services.AddSingleton<ICallStateManager<BaseActiveCallState>, ConcurrentInMemoryCallStateManager<BaseActiveCallState>>();
        services.AddSingleton<ICallHistoryManager<BaseActiveCallState, CallNotification>, ConcurrentInMemoryCallHistoryManager<BaseActiveCallState, CallNotification>>();

        return services.AddSingleton<IPstnCallingBot, RickrollPstnBot>();
    }
}
