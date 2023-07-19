using GroupCallingChatBot.Web;

namespace GroupCallingChatBot.Web.AdaptiveCards;

public class AddContact : BaseAdaptiveCard
{

    public AddContact()
    {
    }

    public override string GetCardContent()
    {
        var json = ReadResource(CardConstants.CardFileNameAddContact);

        return json;
    }
}
