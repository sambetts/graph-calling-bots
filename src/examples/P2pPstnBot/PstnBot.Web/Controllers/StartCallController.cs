using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using PstnBot.Shared;
using PstnBot.Web;
using ServiceHostedMediaCallingBot.Engine.CallingBots;

namespace PstnBot.Web.Controllers;

[Route("[controller]")]
public class StartCallController : Controller
{
    private readonly IPstnCallingBot _callingBot;

    public StartCallController(IPstnCallingBot callingBot)
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

        var call = await _callingBot.StartPTSNCall(startCallData.PhoneNumber).ConfigureAwait(false);

        return call;
    }
}
