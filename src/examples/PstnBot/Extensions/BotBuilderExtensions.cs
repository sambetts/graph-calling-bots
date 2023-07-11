using SimpleCallingBotEngine.Models;

namespace PstnBot;

public static class BotBuilderExtensions
{
    public static IServiceCollection AddCallingBot(this IServiceCollection services)
        => services.AddCallingBot(_ => { });

    public static IServiceCollection AddCallingBot(this IServiceCollection services, Action<RemoteMediaCallingBotConfiguration> botOptionsAction) 
    {
        var options = new RemoteMediaCallingBotConfiguration();
        botOptionsAction(options);
        services.AddSingleton(options);

        return services.AddSingleton<RickrollPstnBot>();
    }
}
