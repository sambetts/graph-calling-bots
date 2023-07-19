using RickrollP2PPstnBot;
using ServiceHostedMediaCallingBot.Engine.Models;

namespace RickrollP2PPstnBot.Extensions;

public static class BotBuilderExtensions
{
    public static IServiceCollection AddCallingBot(this IServiceCollection services)
        => services.AddCallingBot(_ => { });

    public static IServiceCollection AddCallingBot(this IServiceCollection services, Action<RemoteMediaCallingBotConfiguration> botOptionsAction)
    {
        var options = new RemoteMediaCallingBotConfiguration();
        botOptionsAction(options);
        options.CallingEndpoint = options.BotBaseUrl + HttpRouteConstants.OnIncomingRequestRoute;
        services.AddSingleton(options);

        return services.AddSingleton<RickrollPstnBot>();
    }
}
