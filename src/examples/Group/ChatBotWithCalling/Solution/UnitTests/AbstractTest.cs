using GroupCallingBot.FunctionApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GroupCallingChatBot.UnitTests;


public abstract class AbstractTest
{
    protected FunctionsAppCallingBotConfig _config = null!;
    protected ILogger _tracer = null!;

    protected AbstractTest()
    {
        var builder = new ConfigurationBuilder()
            .AddUserSecrets<AbstractTest>();

        var config = builder.Build();

        builder = new ConfigurationBuilder()
            .AddUserSecrets<AbstractTest>();

        _tracer = LoggerFactory.Create(config =>
        {
            config.AddConsole();
            config.SetMinimumLevel(LogLevel.Debug);
        }).CreateLogger("Unit tests");

        config = builder.Build();
        _config = new FunctionsAppCallingBotConfig(config);


    }

    protected static Stream GetTestDataFileStream(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var embeddedName = $"{assembly.GetName().Name}.DataImport.TestData.{fileName}";

        var stream = assembly.GetManifestResourceStream(embeddedName);

        if (stream == null)
            throw new Exception($"Unable find embedded file at \"{embeddedName}\"");

        return stream;
    }

    protected ILogger<T> GetLogger<T>()
    {
        return LoggerFactory.Create(config =>
        {
            config.AddConsole();
            config.SetMinimumLevel(LogLevel.Debug);
        }).CreateLogger<T>();
    }
    protected ILogger GetLogger()
    {
        return LoggerFactory.Create(config =>
        {
            config.AddConsole();
            config.SetMinimumLevel(LogLevel.Debug);
        }).CreateLogger("Unit tests");
    }
}
