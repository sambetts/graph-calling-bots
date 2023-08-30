using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallingTestBot.FunctionApp;

public class CallingTestManager
{
    private readonly TestConfig _testConfig;
    private readonly TestPstnBot _callingBot;

    public CallingTestManager(TestConfig testConfig, TestPstnBot callingBot) 
    {
        _testConfig = testConfig ?? throw new ArgumentNullException(nameof(testConfig));
        _callingBot = callingBot ?? throw new ArgumentNullException(nameof(callingBot));

    }

    public async Task StartTestPstnCall()
    {
        // Begin call. Will need to wait for callback to see what happens.
        await _callingBot.StartPTSNCall(_testConfig.Number);
    }

    public async Task HandleGraphCallback(CommsNotificationsPayload commsNotificationsPayload)
    {
        await _callingBot.HandleNotificationsAndUpdateCallStateAsync(commsNotificationsPayload);
    }
}


public class TestConfig
{
    public string Number { get; set; } = null!;
}
