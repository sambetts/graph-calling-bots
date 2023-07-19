using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using System.Threading.Tasks;
using System;
using Engine;

namespace Bot.Controllers;

[Route("[controller]")]
public class StartCallController : Controller
{
    private readonly GroupCallBot _callingBot;

    public StartCallController(GroupCallBot callingBot)
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

        var req = new MeetingRequest();

        req.Attendees.Add(new AttendeeCallInfo
        {
            DisplayId = startCallData.PhoneNumber,
            Id = startCallData.PhoneNumber,
            Type = Engine.MeetingAttendeeType.Phone
        });

        req.Attendees.Add(new AttendeeCallInfo
        {
            DisplayId = "Sam Teams",
            Id = "3b10aa94-739a-472c-a68a-c2e3d480ed6b",
            Type = Engine.MeetingAttendeeType.Teams
        });

        //req.Attendees.Add(new AttendeeCallInfo
        //{
        //    DisplayId = "Sam2",
        //    Id = "6c9ebfbf-1ea8-4469-a54a-2b952b4bceb9",
        //    Type = Engine.AttendeeType.Teams
        //});

        var call = await this._callingBot.StartGroupCall(req).ConfigureAwait(false);

        return call;
    }
}
public class StartCallData
{
    public string PhoneNumber { get; set; } = null!;

}
