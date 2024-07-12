using Microsoft.Graph.Models;

namespace CallingTestBot.FunctionApp.Engine;

/// <summary>
/// Tests for recording if test calls work.
/// </summary>
public interface IBotTestsLogger
{
    string TableName { get; }
    bool Initialised { get; }
    Task Initialise();
    Task<TestCallState?> GetTestCallState(string callId);
    Task LogCallConnectedSuccesfully(string callId);
    Task LogCallTerminated(string callId, ResultInfo resultInfo);
    Task LogNewCallEstablishing(string callId, string numberCalled);
}
