using AdaptiveCards;
using GroupCallingChatBot.Models;
using System.Collections.Generic;

namespace GroupCallingChatBot.AdaptiveCards;

public class MenuCard : BaseAdaptiveCard
{
    private readonly MeetingRequest _meetingState;

    public MenuCard(MeetingRequest meetingState)
    {
        _meetingState = meetingState;
    }

    public override string GetCardContent()
    {
        var json = ReadResource(CardConstants.CardFileNameBotMenu);

        var numbers = new List<AdaptiveElement>();
        var states = new List<AdaptiveElement>();
        var cols = new AdaptiveColumnSet
        {
            Columns = new List<AdaptiveColumn>
            {
                new AdaptiveColumn{ Items = numbers},
                new AdaptiveColumn{ Items = states }
            }
        };

        // Add attendees to card
        foreach (var item in _meetingState.Attendees)
        {
            numbers.Add(new AdaptiveTextBlock
            {
                Text = item.DisplayId,
                Size = AdaptiveTextSize.Small,
                Weight = AdaptiveTextWeight.Bolder
            });

            states.Add(new AdaptiveTextBlock
            {
                Text = item.Type == MeetingAttendeeType.Phone ? "Calling" : "Teams",
                Size = AdaptiveTextSize.Small,
                Weight = AdaptiveTextWeight.Bolder
            });
        }

        var actions = new List<AdaptiveSubmitAction> {
            new AdaptiveSubmitAction
            {
                Title = "Add Contact",
                Data = new AdaptiveCardActionResponse
                {
                    Action = CardConstants.CardActionValStartAddAttendee
                }
            }
        };
        if (_meetingState.Attendees.Count > 0)
        {
            actions.Add(new AdaptiveSubmitAction
            {
                Title = "Start Meeting",
                Data = new AdaptiveCardActionResponse
                {
                    Action = CardConstants.CardActionValStartMeeting
                }
            });
        }

        json = ReplaceVal(json, CardConstants.CardContentVarBotMenu, Newtonsoft.Json.JsonConvert.SerializeObject(cols));
        json = ReplaceVal(json, CardConstants.CardContentActions, Newtonsoft.Json.JsonConvert.SerializeObject(actions));

        return json;
    }
}
