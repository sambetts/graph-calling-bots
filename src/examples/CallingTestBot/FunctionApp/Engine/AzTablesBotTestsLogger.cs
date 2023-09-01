﻿using Microsoft.Graph;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace CallingTestBot.FunctionApp.Engine;

/// <summary>
/// Log test call results to Azure Tables
/// </summary>
public class AzTablesBotTestsLogger : AbstractAzTablesStorageManager, IBotTestsLogger
{
    public AzTablesBotTestsLogger(CallingTestBotConfig callingTestBotConfig) : base(callingTestBotConfig.Storage)
    {
    }
    public override string TableName => "TestCallState";

    public async Task LogNewCallEstablishing(string callId, string numberCalled)
    {
        InitCheck(_tableClient);

        var newCallTestLog = new TestCallState { CallId = callId, CallConnectedOk = false, Timestamp = DateTime.UtcNow, NumberCalled = numberCalled };
        await _tableClient!.UpsertEntityAsync(newCallTestLog);
    }

    public async Task LogCallConnectedSuccesfully(string callId)
    {
        InitCheck(_tableClient);

        var existingCallTestLog = await GetTestCallStateOrThrowIfNone(callId);
        existingCallTestLog.CallConnectedOk = true;
        await _tableClient!.UpsertEntityAsync(existingCallTestLog);
    }

    public async Task LogCallTerminated(string callId, ResultInfo resultInfo)
    {
        InitCheck(_tableClient);

        var existingCallTestLog = await GetTestCallStateOrThrowIfNone(callId);

        existingCallTestLog.CallTerminateCode = resultInfo.Code;
        existingCallTestLog.CallTerminateMessage = resultInfo.Message;
        await _tableClient!.UpsertEntityAsync(existingCallTestLog);
    }

    public async Task<TestCallState?> GetTestCallState(string callId)
    {
        InitCheck(_tableClient);

        var results = _tableClient!.QueryAsync<TestCallState>(f => f.RowKey == callId);
        await foreach (var result in results)
        {
            return result;
        }

        return null;
    }

    async Task<TestCallState> GetTestCallStateOrThrowIfNone(string callId)
    {
        var existingCallTestLog = await GetTestCallState(callId);
        if (existingCallTestLog == null)
        {
            throw new Exception($"Call {callId} not found in {TableName}");
        }
        return existingCallTestLog;
    }
}
