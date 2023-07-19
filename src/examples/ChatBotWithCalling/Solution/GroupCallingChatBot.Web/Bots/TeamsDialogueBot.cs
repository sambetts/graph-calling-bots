using GroupCallingChatBot.Web.AdaptiveCards;
using GroupCallingChatBot.Web.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GroupCallingChatBot.Web.Bots;

public class TeamsDialogueBot<T> : DialogBot<T> where T : Dialog
{
    public TeamsDialogueBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger)
        : base(conversationState, userState, dialog, logger)
    {
    }

    protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        var userStateAccessors = _userState.CreateProperty<MeetingRequest>(nameof(MeetingRequest));
        var meetingState = await userStateAccessors.GetAsync(turnContext, () => new MeetingRequest());

        var welcomeText = "Hello and welcome!";
        foreach (var member in membersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                // Say hi
                await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);

                var introCardAttachment = new MenuCard(meetingState).GetCardAttachment();
                await turnContext.SendActivityAsync(MessageFactory.Attachment(introCardAttachment));
            }
        }
    }
}
