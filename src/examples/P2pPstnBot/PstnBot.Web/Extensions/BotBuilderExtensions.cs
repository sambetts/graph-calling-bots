using PstnBot.Shared;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;

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

        return services.AddSingleton<IPstnCallingBot, RickrollPstnBot>();
    }
}
