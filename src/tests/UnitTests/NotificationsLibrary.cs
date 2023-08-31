using ServiceHostedMediaCallingBot.Engine.Models;
using System.Text.Json;

namespace ServiceHostedMediaCallingBot.UnitTests;

internal class NotificationsLibrary
{
    public static CommsNotificationsPayload CallEstablishingP2P => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.CallEstablishing)!;
    public static CommsNotificationsPayload CallEstablishedP2P => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.CallEstablished)!;
    public static CommsNotificationsPayload CallEstablishedWithAudioP2P => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.CallEstablishedWithAudio)!;
    public static CommsNotificationsPayload HangUp => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.HangUp)!;
    public static CommsNotificationsPayload PlayPromptFinish => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.PlayPromptFinish)!;
    public static CommsNotificationsPayload TonePress => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.TonePress)!;

    public static CommsNotificationsPayload GroupCallEstablished => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.GroupCallEstablished)!;
    public static CommsNotificationsPayload GroupCallEstablishing => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.GroupCallEstablishing)!;
    public static CommsNotificationsPayload GroupCallBotJoin => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.GroupCallBotJoin)!;
    public static CommsNotificationsPayload GroupCallUserJoin => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.GroupCallUserJoin)!;
    public static CommsNotificationsPayload GroupCallEnd => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.GroupCallEnd)!;



    public static CommsNotificationsPayload FailedCallEstablishingP2P => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.FailedCallEstablishing)!;
    public static CommsNotificationsPayload FailedCallDeleted => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.FailedCallDeleted)!;
}
