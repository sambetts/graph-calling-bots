using Microsoft.Extensions.Logging;
using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;
using ServiceHostedMediaCallingBot.UnitTests.TestServices;

namespace ServiceHostedMediaCallingBot.UnitTests;

public class BaseTests
{
    protected SlowInMemoryCallStateManager<BaseActiveCallState> _callStateManager = new SlowInMemoryCallStateManager<BaseActiveCallState>();
    protected ConcurrentInMemoryCallHistoryManager<BaseActiveCallState> _historyManager = new ConcurrentInMemoryCallHistoryManager<BaseActiveCallState>();
    protected ILogger _logger;

    public BaseTests()
    {
        _logger = LoggerFactory.Create(config =>
        {
            config.AddTraceSource(new System.Diagnostics.SourceSwitch("SourceSwitch"));
            config.AddConsole();
        }).CreateLogger("Unit tests");
    }
}
