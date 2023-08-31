using Microsoft.Graph;

namespace CallingTestBot.FunctionApp.Engine
{
    public interface IBotTestsLogger
    {
        string TableName { get; }

        Task<TestCallState?> GetTestCallState(string callId);
        Task LogCallConnectedSuccesfully(string callId);
        Task LogCallTerminated(string callId, ResultInfo resultInfo);
        Task LogNewCallEstablishing(string callId);
    }
}