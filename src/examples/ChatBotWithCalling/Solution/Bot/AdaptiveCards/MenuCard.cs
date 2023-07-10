namespace Bot.AdaptiveCards
{
    public class MenuCard : BaseAdaptiveCard
    {
        public MenuCard()
        {
        }

        public override string GetCardContent()
        {
            var json = ReadResource(CardConstants.CardFileNameBotMenu);

            //json = base.ReplaceVal(json, CardConstants.FIELD_NAME_BOT_NAME, this.BotName);

            return json;
        }
    }
}
