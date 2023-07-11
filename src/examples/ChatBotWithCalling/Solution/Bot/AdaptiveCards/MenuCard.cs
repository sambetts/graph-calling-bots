using AdaptiveCards;
using Bot.Dialogues;
using Engine;
using System.Collections.Generic;

namespace Bot.AdaptiveCards;

public class MenuCard : BaseAdaptiveCard
{
    private readonly MeetingState _meetingState;

    public MenuCard(MeetingState meetingState)
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
        foreach (var item in _meetingState.Numbers)
        {
            numbers.Add(new AdaptiveTextBlock
            {
                Text = item.Number,
                Size = AdaptiveTextSize.Small,
                Weight = AdaptiveTextWeight.Bolder
            });

            states.Add(new AdaptiveTextBlock
            {
                Text = "Calling",
                Size = AdaptiveTextSize.Small,
                Weight = AdaptiveTextWeight.Bolder
            });
        }

        AdaptiveAction? action = null;
        var meetingInfoDesc = "No meeting created";
        if (!_meetingState.IsMeetingCreated)
        {
            action = new AdaptiveSubmitAction
            {
                Title = "Create Meeting",
                Data = new ActionResponse
                {
                    Action = CardConstants.CardActionValCreateMeeting
                }
            };
        }
        else
        {
            meetingInfoDesc = $"Meeting created at {_meetingState.Created} with url {_meetingState.MeetingUrl}";
            action = new AdaptiveSubmitAction
            {
                Title = "Add Number",
                Data = new ActionResponse
                {
                    Action = CardConstants.CardActionValAddNumber
                }
            };
        }

        json = ReplaceVal(json, CardConstants.CardContentVarBotMenu, Newtonsoft.Json.JsonConvert.SerializeObject(cols));
        json = ReplaceVal(json, CardConstants.CardContentActions, Newtonsoft.Json.JsonConvert.SerializeObject(action));
        json = ReplaceVal(json, CardConstants.CardContentMeetingInfo, meetingInfoDesc);

        return json;
    }
}
