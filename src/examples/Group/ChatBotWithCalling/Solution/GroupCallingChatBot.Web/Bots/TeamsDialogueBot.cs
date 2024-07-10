using GroupCallingChatBot.Web.AdaptiveCards;
using GroupCallingChatBot.Web.Models;
using GroupCalls.Common;
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
    private readonly TeamsChatbotBotConfig _config;

    public TeamsDialogueBot(TeamsChatbotBotConfig config, ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger)
        : base(conversationState, userState, dialog, logger)
    {
        _config = config;
    }

    protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        var userStateAccessors = _userState.CreateProperty<StartGroupCallData>(nameof(StartGroupCallData));
        var meetingState = await userStateAccessors.GetAsync(turnContext, () => GetDefaultStartGroupCallData(_config));

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

    public static StartGroupCallData GetDefaultStartGroupCallData(TeamsChatbotBotConfig botConfig)
    {
        return new StartGroupCallData { MessageInviteUrl = $"{botConfig.BotBaseUrl}/audio/rickroll.wav" };
    }
}

