using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using PstnBot.Shared;
using ServiceHostedMediaCallingBot.Engine.CallingBots;
using ServiceHostedMediaCallingBot.Engine.Models;

namespace PstnBot.Web.Controllers;

[Route("[controller]")]
public class StartCallController : Controller
{
    private readonly IPstnCallingBot _callingBot;
    private readonly SingleWavFileBotConfig _botConfig;

    public StartCallController(IPstnCallingBot callingBot, SingleWavFileBotConfig botConfig)
    {
        _callingBot = callingBot;
        _botConfig = botConfig;
    }

    /// <summary>
    /// POST: StartCall
    /// </summary>
    [HttpPost()]
    public async Task<Call?> StartCall([FromBody] StartCallData startCallData)
    {
        if (startCallData == null)
        {
            throw new ArgumentNullException(nameof(startCallData));
        }

        var call = await _callingBot.StartPTSNCall(startCallData.PhoneNumber, _botConfig.WavCallbackUrl).ConfigureAwait(false);

        return call;
    }
}
