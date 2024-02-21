using ServiceHostedMediaCallingBot.Engine.Models;
using ServiceHostedMediaCallingBot.Engine.StateManagement;

namespace ServiceHostedMediaCallingBot.UnitTests.TestServices;

public class SlowInMemoryCallStateManager<T> : ConcurrentInMemoryCallStateManager<T> where T : BaseActiveCallState
{
    public override async Task AddCallStateOrUpdate(T callState)
    {
        await Delay();
        await base.AddCallStateOrUpdate(callState);
    }
    public override async Task<T?> GetByNotificationResourceUrl(string resourceUrl)
    {
        await Delay();
        return await base.GetByNotificationResourceUrl(resourceUrl);
    }
    public override async Task<bool> RemoveCurrentCall(string resourceUrl)
    {
        await Delay();
        return await base.RemoveCurrentCall(resourceUrl);
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
