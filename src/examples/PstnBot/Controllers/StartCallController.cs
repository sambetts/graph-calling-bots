using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;

namespace PstnBot;

[Route("[controller]")]
public class StartCallController : Controller
{
    private readonly PstnCallingBot _callingBot;

    public StartCallController(PstnCallingBot callingBot)
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

        var call = await this._callingBot.StartP2PCall(startCallData.PhoneNumber).ConfigureAwait(false);

        return call;
    }
}
