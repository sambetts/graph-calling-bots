using Microsoft.Graph;
using System.Text.Json.Serialization;

namespace ServiceHostedMediaCallingBot.Engine.Models;

internal class InviteInfo : EmptyModelWithClientContext
{
    [JsonPropertyName("participants")]
    public List<InvitationParticipantInfo> Participants { get; set; } = new List<InvitationParticipantInfo>();
}
