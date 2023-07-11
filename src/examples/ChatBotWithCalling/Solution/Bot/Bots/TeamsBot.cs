using Bot.AdaptiveCards;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static Bot.Dialogues.MainDialog;

namespace Bot.Bots;

public class TeamsBot<T> : DialogBot<T> where T : Dialog
{
    public TeamsBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger)
        : base(conversationState, userState, dialog, logger)
    {
    }

    protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        var userStateAccessors = _userState.CreateProperty<MeetingState>(nameof(MeetingState));
        var meetingState = await userStateAccessors.GetAsync(turnContext, () => new MeetingState());

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
