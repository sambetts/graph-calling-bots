using GroupCallingChatBot.Web.AdaptiveCards;
using GroupCallingChatBot.Web.Dialogues;
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

        var welcomeText = "Hello and welcome!";
        foreach (var member in membersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                var botUser = member.ParseBotUserInfo();

                // Is this an Azure AD user?
                if (!botUser.IsAzureAdUserId)
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Hi, anonynous user. I only work with Azure AD users in Teams normally..."));


                var meetingState = await userStateAccessors.GetAsync(turnContext,
                    () => TeamsDialogueBot<MainDialog>.GetDefaultStartGroupCallData(_config, botUser.IsAzureAdUserId ? botUser.UserId : null));
                // Say hi
                await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);

                var introCardAttachment = new MenuCard(meetingState).GetCardAttachment();
                await turnContext.SendActivityAsync(MessageFactory.Attachment(introCardAttachment));
            }
        }
    }

    public static StartGroupCallData GetDefaultStartGroupCallData(TeamsChatbotBotConfig botConfig, string? organizerUserId)
    {
        return new StartGroupCallData
        {
            MessageInviteUrl = $"{botConfig.BotBaseUrl}/audio/invite.wav",
            MessageTransferingUrl = $"{botConfig.BotBaseUrl}/audio/transfering.wav",
            OrganizerUserId = organizerUserId 
        };
    }
}

