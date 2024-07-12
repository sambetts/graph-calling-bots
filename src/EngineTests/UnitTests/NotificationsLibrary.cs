using GraphCallingBots.Models;
using System.Text.Json;

namespace GraphCallingBots.UnitTests;

internal class NotificationsLibrary
{
    public static CommsNotificationsPayload P2PTest1CallEstablishingP2P => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.P2PTest1Event1CallEstablishing)!;
    public static CommsNotificationsPayload P2PTest1CallEstablishedP2P => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.P2PTest1Event2CallEstablished)!;
    public static CommsNotificationsPayload P2PTest1CallEstablishedWithAudioP2P => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.P2PTest1Event3CallEstablishedWithAudio)!;
    public static CommsNotificationsPayload P2PTest1HangUp => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.P2PTest1Event6HangUp)!;
    public static CommsNotificationsPayload P2PTest1PlayPromptFinish => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.P2PTest1Event5PlayPromptFinish)!;
    public static CommsNotificationsPayload P2PTest1TonePress => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.P2PTest1Event4TonePress)!;


    public static CommsNotificationsPayload P2PTest2Event1Establishing => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.P2PTest2Event1Establishing)!;
    public static CommsNotificationsPayload P2PTest2Event2Established => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.P2PTest2Event2Established)!;
    public static CommsNotificationsPayload P2PTest2Event3UpdatedWithMediaState => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.P2PTest2Event3UpdatedWithMediaState)!;
    public static CommsNotificationsPayload P2PTest2Event4UpdatedWithChatInfo => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.P2PTest2Event4UpdatedWithChatInfo)!;
    public static CommsNotificationsPayload P2PTest2Event5UpdatedWithRandomShit => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.P2PTest2Event5UpdatedWithRandomShit)!;
    public static CommsNotificationsPayload P2PTest2Event6UserJoin => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.P2PTest2Event6UserJoin)!;


    public static CommsNotificationsPayload GroupCallEstablished => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.GroupCallEstablished)!;
    public static CommsNotificationsPayload GroupCallEstablishing => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.GroupCallEstablishing)!;
    public static CommsNotificationsPayload GroupCallBotJoin => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.GroupCallBotJoin)!;
    public static CommsNotificationsPayload GroupCallUserJoin => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.GroupCallUserJoin)!;
    public static CommsNotificationsPayload GroupCallEnd => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.GroupCallEnd)!;



    public static CommsNotificationsPayload FailedCallEstablishingP2P => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.FailedCallEstablishing)!;
    public static CommsNotificationsPayload FailedCallDeleted => JsonSerializer.Deserialize<CommsNotificationsPayload>(Properties.Resources.FailedCallDeleted)!;
}
