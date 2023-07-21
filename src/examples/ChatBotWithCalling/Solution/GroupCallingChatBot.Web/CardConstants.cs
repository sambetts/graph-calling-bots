namespace GroupCallingChatBot.Web;

public class CardConstants
{
    public const string CardActionPropName = "action";
    public const string CardActionValStartAddAttendee = "StartAddAttendee";
    public const string CardActionValAddAttendee = "AddAttendee";
    public const string CardActionValStartMeeting = "StartMeeting";

    public const string CardContentVarBotMenu = "/*meeting numbers and status*/";
    public const string CardContentActions = "/*actions*/";
    public const string CardContentMeetingInfo = "${MeetingInfo}";

    public const string CardFileNameBotMenu = "GroupCallingChatBot.Web.AdaptiveCards.Templates.BotMenu.json";
    public const string CardFileNameAddContact = "GroupCallingChatBot.Web.AdaptiveCards.Templates.AddContact.json";

    public const string CardActionValContactTypeTeams = "Teams";
    public const string CardActionValContactTypePhone = "Phone";
}
