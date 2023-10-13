using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;

namespace CallingTestBot.FunctionApp;

public class TimerFunction
{
    private readonly ILogger _logger;

    private readonly IPstnCallingBot _callingBot;
    private readonly CallingTestBotConfig _callingTestBotConfig;
    private readonly SingleWavFileBotConfig _botConfig;

    public TimerFunction(ILoggerFactory loggerFactory, IPstnCallingBot callingBot, CallingTestBotConfig callingTestBotConfig, SingleWavFileBotConfig botConfig)
    {
        _logger = loggerFactory.CreateLogger<TimerFunction>();

        _callingBot = callingBot;
        _callingTestBotConfig = callingTestBotConfig;
        _botConfig = botConfig;
    }

    [Function(nameof(TestCallTimer))]
    public async Task TestCallTimer([TimerTrigger("0 */5 * * * *")] TimerExecutionInfo info)
    {
        _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        _logger.LogInformation($"Starting new call to number {_callingTestBotConfig.TestNumber} (timer call)");
        try
        {
            await _callingBot.StartPTSNCall(_callingTestBotConfig.TestNumber, _botConfig.WavCallbackUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting call");
        }

        _logger.LogInformation($"Next timer schedule at: {info.ScheduleStatus.Next}");
    }
}

public class TimerExecutionInfo
{
    public MyScheduleStatus ScheduleStatus { get; set; } = null!;

    public bool IsPastDue { get; set; }
}

public class MyScheduleStatus
{
    public DateTime Last { get; set; }

    public DateTime Next { get; set; }

    public DateTime LastUpdated { get; set; }
}
