using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CallingTestBot.FunctionApp
{
    public class TimerFunction
    {
        private readonly ILogger _logger;

        public TimerFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TimerFunction>();
        }

        [Function("TimerFunction")]
        public void Run([TimerTrigger("0 */5 * * * *")] TimerExecutionInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
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
}
