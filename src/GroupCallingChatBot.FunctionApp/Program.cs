using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using GroupCallingChatBot.Web.Extensions;
using GroupCallingChatBot.Web;

var host = new HostBuilder()
    .ConfigureServices((context, services) =>
    {
        // Create the Bot Framework Adapter with error handling enabled.
        services.AddChatBot();
    })
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();
