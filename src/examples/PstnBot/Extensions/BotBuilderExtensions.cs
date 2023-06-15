using SimpleCallingBot.Models;

namespace PstnBot;

public static class BotBuilderExtensions
{
    public static IServiceCollection AddCallingBot(this IServiceCollection services)
        => services.AddCallingBot(_ => { });

    public static IServiceCollection AddCallingBot(this IServiceCollection services, Action<BotOptions> botOptionsAction) 
    {
        var options = new BotOptions();
        botOptionsAction(options);
        services.AddSingleton(options);

        return services.AddSingleton<PstnCallingBot>();
    }
}
