using GraphCallingBots.Models;
using GraphCallingBots.StateManagement;

namespace GraphCallingBots.UnitTests.TestServices;

public class SlowInMemoryCallStateManager<T> : ConcurrentInMemoryCallStateManager<T> where T : BaseActiveCallState
{
    public override async Task AddCallStateOrUpdate(T callState)
    {
        await Delay();
        await base.AddCallStateOrUpdate(callState);
    }
    public override async Task<T?> GetStateByCallId(string callId)
    {
        await Delay();
        return await base.GetStateByCallId(callId);
    }
    public override async Task<bool> RemoveCurrentCall(string callId)
    {
        await Delay();
        return await base.RemoveCurrentCall(callId);
    }

    public override async Task UpdateCurrentCallState(T callState)
    {
        await Delay();

        await base.UpdateCurrentCallState(callState);
    }

    public override async Task<List<T>> GetActiveCalls()
    {
        await Delay();
        return await base.GetActiveCalls();
    }

    async Task Delay()
    {
        await Task.Delay(100);
    }
}
