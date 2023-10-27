using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;
using ServiceHostedMediaCallingBot.UnitTests.TestServices;

namespace ServiceHostedMediaCallingBot.UnitTests;

public class BaseTests
{
    protected SlowInMemoryCallStateManager<BaseActiveCallState> _callStateManager = new SlowInMemoryCallStateManager<BaseActiveCallState>();
    protected ConcurrentInMemoryCallHistoryManager<BaseActiveCallState, CallNotification> _historyManager = new ConcurrentInMemoryCallHistoryManager<BaseActiveCallState, CallNotification>();
    protected ILogger _logger;
    protected UnitTestConfig _config;

    public BaseTests()
    {
        _logger = LoggerFactory.Create(config =>
        {
            config.AddTraceSource(new System.Diagnostics.SourceSwitch("SourceSwitch"));
            config.AddConsole();
        }).CreateLogger("Unit tests");

        var builder = new ConfigurationBuilder()
            .AddUserSecrets<BaseTests>();

        var config = builder.Build();

        builder = new ConfigurationBuilder()
            .AddUserSecrets<BaseTests>();


        _config = new UnitTestConfig(config);
    }

    protected ILogger<T> GetLogger<T>()
    {
        return
        LoggerFactory.Create(config =>
        {
            config.AddConsole();
        }).CreateLogger<T>(); ;
    }
}
