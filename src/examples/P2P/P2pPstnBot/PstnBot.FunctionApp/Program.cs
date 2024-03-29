using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using PstnBot.FunctionApp;
using PstnBot.FunctionApp.Extensions;

var host = new HostBuilder()
    .ConfigureAppConfiguration(c =>
    {
        c.AddEnvironmentVariables()
            .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddCommandLine(args)
            .Build();
    })
    .ConfigureServices(configureDelegate: (hostContext, services) =>
    {
        var config = new FunctionsAppCallingBotConfig(hostContext.Configuration);
        services.AddCallingBot(config);

    })
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();
