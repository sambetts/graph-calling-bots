﻿using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;
using GraphCallingBots.UnitTests.TestServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GraphCallingBots.UnitTests;

public class BaseTests
{
    protected SlowInMemoryCallStateManager<BaseActiveCallState> _callStateManager = new SlowInMemoryCallStateManager<BaseActiveCallState>();
    protected ConcurrentInMemoryCallHistoryManager<BaseActiveCallState> _historyManager = new ConcurrentInMemoryCallHistoryManager<BaseActiveCallState>();
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
