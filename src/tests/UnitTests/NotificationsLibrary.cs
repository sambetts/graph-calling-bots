using SimpleCallingBotEngine.Models;
using System.Text.Json;

namespace UnitTests;

internal class NotificationsLibrary
{
    public static CommsNotificationsPayload CallEstablishing => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.CallEstablishing)!;
    public static CommsNotificationsPayload CallEstablished => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.CallEstablished)!;

    public static CommsNotificationsPayload CallEstablishedWithAudio => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.CallEstablishedWithAudio)!;
    public static CommsNotificationsPayload HangUp => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.HangUp)!;
    public static CommsNotificationsPayload PlayPromptFinish => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.PlayPromptFinish)!;
    public static CommsNotificationsPayload TonePress => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.TonePress)!;
}
