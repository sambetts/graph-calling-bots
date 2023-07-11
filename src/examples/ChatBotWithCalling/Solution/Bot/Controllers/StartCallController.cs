using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using System.Threading.Tasks;
using System;
using Engine;

namespace Bot.Controllers;

[Route("[controller]")]
public class StartCallController : Controller
{
    private readonly CallAndRedirectBot _callingBot;

    public StartCallController(CallAndRedirectBot callingBot)
    {
        _callingBot = callingBot;
    }

    /// <summary>
    /// POST: StartCall
    /// </summary>
    [HttpPost()]
    public async Task<Call> StartCall([FromBody] StartCallData startCallData)
    {
        if (startCallData == null)
        {
            throw new ArgumentNullException(nameof(startCallData));
        }

        var call = await this._callingBot.StartGroupCall(startCallData.PhoneNumber).ConfigureAwait(false);

        return call;
    }
}
public class StartCallData
{
    public string PhoneNumber { get; set; } = null!;

}
