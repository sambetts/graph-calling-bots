using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder;
using System.Threading.Tasks;
using System.Threading;
using Bot.Dialogues.Utils;
using Bot.AdaptiveCards;
using System;
using Engine;
using CommonUtils;
using Microsoft.Bot.Schema;
using System.Collections.Generic;

namespace Bot.Dialogues;

/// <summary>
/// Entrypoint to all new conversations
/// </summary>
public class MainDialog : CancellableDialogue
{
    private readonly UserState _userState;
    private readonly Config _config;
    private readonly GroupCallBot _groupCallBot;
    private readonly ITeamsChatbotManager _teamsChatbotManager;

    public MainDialog(UserState userState, Config config, GroupCallBot groupCallBot, ITeamsChatbotManager teamsChatbotManager) : base(nameof(MainDialog))
    {
        AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
        {
            SendBotMenu,
            ProcessNumberAsync,
        }));
        AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
        AddDialog(new TextPrompt(nameof(TextPrompt)));
        InitialDialogId = nameof(WaterfallDialog);
        _userState = userState;
        _config = config;
        _groupCallBot = groupCallBot;
        _teamsChatbotManager = teamsChatbotManager;
    }


    /// <summary>
    /// Main entry-point for bot new chat
    /// </summary>
    private async Task<DialogTurnResult> SendBotMenu(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // Get user state
        var userStateAccessors = _userState.CreateProperty<MeetingRequest>(nameof(MeetingRequest));
        var meetingState = await userStateAccessors.GetAsync(stepContext.Context, () => new MeetingRequest());

        var val = stepContext.Context.Activity.Value ?? string.Empty;

        // Form action
        var actionInfo = AdaptiveCardUtils.GetAdaptiveCardAction(val?.ToString()) ?? new AdaptiveCardActionResponse();

        if (actionInfo != null)
        {
            if (actionInfo.Action == CardConstants.CardActionValStartAddAttendee)
            {
                // Send "add contact" card
                var opts = new PromptOptions
                {
                    Prompt = new Activity
                    {
                        Attachments = new List<Attachment>() { new AddContact().GetCardAttachment() },
                        Type = ActivityTypes.Message,
                        Text = "Please fill out all the fields below",
                    }
                };
                return await stepContext.PromptAsync(nameof(TextPrompt), opts);
            }
            else if (actionInfo.Action == CardConstants.CardActionValAddAttendee)
            {
                // Is this an action from the add contact card? Validation errors from previous step will arrive here
                var opts = new PromptOptions { Prompt = new Activity { Type = ActivityTypes.CommandResult } };
                return await stepContext.PromptAsync(nameof(TextPrompt), opts);
            }
            else if (actionInfo.Action == CardConstants.CardActionValStartMeeting)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Starting call. Have a nice meeting!"), cancellationToken);
                await _groupCallBot.StartGroupCall(meetingState);
                return await stepContext.EndDialogAsync(meetingState, cancellationToken);
            }
            else
            {
                // No idea what to do. Send this and end diag
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Hi, sorry, I didn't get that...use the menu buttons"), cancellationToken);
            }
        }
        else
        {
            // No idea what to do. Send this and end diag
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                    $"Hi, sorry, I can't seem to find the meeting I had in memory..."
                ), cancellationToken);
        }

        // Send menu
        var introCardAttachment = new MenuCard(meetingState).GetCardAttachment();
        await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(introCardAttachment));
        return await stepContext.EndDialogAsync(meetingState, cancellationToken);
    }

    private async Task<DialogTurnResult> ProcessNumberAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // Get user state
        var userStateAccessors = _userState.CreateProperty<MeetingRequest>(nameof(MeetingRequest));
        var meetingState = await userStateAccessors.GetAsync(stepContext.Context, () => new MeetingRequest());

        // Form action
        var validInput = false;
        var actionInfo = AdaptiveCardUtils.GetAdaptiveCardAction(stepContext.Context.Activity.Value?.ToString() ?? string.Empty) ?? new AddContactAdaptiveCardActionResponse();
        if (actionInfo != null && actionInfo is AddContactAdaptiveCardActionResponse)
        {
            var addContactActionInfo = (AddContactAdaptiveCardActionResponse)actionInfo;
            if (addContactActionInfo.ContactType == CardConstants.CardActionValContactTypeTeams)
            {
                validInput = DataValidation.IsValidEmail(addContactActionInfo.ContactId);
                if (validInput)
                {
                    // Lookup ID
                    var userId = string.Empty;
                    try
                    {
                        userId = await _teamsChatbotManager.GetUserIdByEmailAsync(addContactActionInfo.ContactId);
                    }
                    catch (Microsoft.Graph.ServiceException ex)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(ex.Message), cancellationToken);
                        validInput = false;
                    }

                    // Have we got a user id?   
                    if (validInput)
                    {
                        meetingState.Attendees.Add(new AttendeeCallInfo { Id = addContactActionInfo.ContactId, DisplayId = addContactActionInfo.ContactId, Type = AttendeeType.Teams });
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Teams user added"), cancellationToken);
                    }
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Invalid email"), cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(MainDialog), null, cancellationToken);
                }
            }
            else if (addContactActionInfo.ContactType == CardConstants.CardActionValContactTypePhone)
            {
                if (!validInput)
                {
                    validInput = DataValidation.IsValidNumber(addContactActionInfo.ContactId);
                    meetingState.Attendees.Add(new AttendeeCallInfo { Id = addContactActionInfo.ContactId, Type = AttendeeType.Phone });
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Number added"), cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Invalid phone number"), cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(SendBotMenu), cancellationToken);
                }
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Invalid contact type"), cancellationToken);
            }
        }

        if (validInput)
        {
            // Send updated menu
            var meetingCardAttachment = new MenuCard(meetingState).GetCardAttachment();
            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(meetingCardAttachment));
        }

        return await stepContext.EndDialogAsync(meetingState, cancellationToken);
    }
}
