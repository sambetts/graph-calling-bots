namespace Bot;

public class CardConstants
{
    public const string CardActionPropName = "action";
    public const string CardActionValCreateMeeting = "CreateMeeting";
    public const string CardActionValAddNumber = "AddExternalNumber";

    public const string CardContentVarBotMenu = "/*meeting numbers and status*/";
    public const string CardContentActions = "/*actions*/";
    public const string CardContentMeetingInfo = "${MeetingInfo}";

    public static string CardFileNameBotMenu => "Bot.AdaptiveCards.Templates.BotMenu.json";

}
