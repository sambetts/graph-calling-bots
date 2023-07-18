namespace Bot.AdaptiveCards;

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
