using Microsoft.Extensions.Hosting;
using PstnBot.FunctionApp;
using PstnBot.FunctionApp.Extensions;

var host = new HostBuilder()
    .ConfigureServices(configureDelegate: (hostContext, services) =>
    {
        var config = new CallingBotConfig(hostContext.Configuration);
        services.AddCallingBot(config);

    })
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();
