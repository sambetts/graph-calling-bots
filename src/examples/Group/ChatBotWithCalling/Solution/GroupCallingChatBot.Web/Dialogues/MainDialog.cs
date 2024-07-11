using CommonUtils;
using GroupCallingChatBot.Web.AdaptiveCards;
using GroupCallingChatBot.Web.Bots;
using GroupCallingChatBot.Web.Dialogues.Utils;
using GroupCallingChatBot.Web.Models;
using GroupCalls.Common;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attachment = Microsoft.Bot.Schema.Attachment;

namespace GroupCallingChatBot.Web.Dialogues;

/// <summary>
/// Entrypoint to all new conversations
/// </summary>
public class MainDialog : CancellableDialogue
{
    private readonly UserState _userState;
    private readonly TeamsChatbotBotConfig _config;
    private readonly GraphServiceClient _graphServiceClient;
    private readonly CallOrchestrator _callOrchestrator;

    public MainDialog(UserState userState, TeamsChatbotBotConfig config, GroupCallBot groupCallBot, GraphServiceClient graphServiceClient, CallOrchestrator callOrchestrator) : base(nameof(MainDialog))
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
        _graphServiceClient = graphServiceClient;
        _callOrchestrator = callOrchestrator;
    }


    /// <summary>
    /// Main entry-point for bot new chat
    /// </summary>
    private async Task<DialogTurnResult> SendBotMenu(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var user = stepContext.Context.Activity.From;
        var botUser = user.ParseBotUserInfo();

        // Get user state
        var userStateAccessors = _userState.CreateProperty<StartGroupCallData>(nameof(StartGroupCallData));
        var meetingState = await userStateAccessors.GetAsync(stepContext.Context,
            () => TeamsDialogueBot<MainDialog>.GetDefaultStartGroupCallData(_config, botUser.IsAzureAdUserId ? botUser.UserId : null));

        // Set organiser id
        if (meetingState.OrganizerUserId == null)
        {
            if (botUser.IsAzureAdUserId)
            {
                meetingState.OrganizerUserId = botUser.UserId;
            }
            else
            {
                if (meetingState.Attendees.Count > 0)
                {
                    meetingState.OrganizerUserId = meetingState.Attendees.First().Id;
                    await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text($"I'm sorry, I can't seem to find your Azure AD user ID. I'll use the first attendee's ID instead."),
                        cancellationToken);
                }
            }
        }

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
                if (meetingState.Attendees.Count > 0)
                {
                    // Start configured meeting
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Starting call..."), cancellationToken);

                    try
                    {
                        var createdCall = await _callOrchestrator.StartGroupCall(meetingState);
                        if (createdCall != null)
                        {
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Your group call ID is {createdCall.Id}. Each attendee will be called seperately and invited to join. Have a nice meeting!"), cancellationToken);
                        }
                        else
                        {
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Sorry, I couldn't start the call. Please try again."), cancellationToken);
                        }
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Sorry, I couldn't start the call. Check WAV config and try again."), cancellationToken);
                    }

                    return await stepContext.EndDialogAsync(meetingState, cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"The meeting details I've got remembered has nobody in it, for some reason. Try adding members and starting the meeting again."), cancellationToken);
                    return await stepContext.EndDialogAsync(meetingState, cancellationToken);
                }
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
        var userStateAccessors = _userState.CreateProperty<StartGroupCallData?>(nameof(StartGroupCallData));
        var meetingState = await userStateAccessors.GetAsync(stepContext.Context, () => null);
        if (meetingState == null)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Oops, looks like I forgot the meeting details...try again?"), cancellationToken);
            return await stepContext.EndDialogAsync(meetingState, cancellationToken);
        }

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
                    // Lookup object ID in Azure AD for email address
                    string? userId = null;
                    try
                    {
                        var user = await _graphServiceClient.Users[addContactActionInfo.ContactId].GetAsync();
                        userId = user?.Id;
                    }
                    catch (ODataError ex) when (ex.ResponseStatusCode == 404)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Teams user not found"), cancellationToken);
                        validInput = false;
                    }
                    catch (ODataError ex)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(ex.Message), cancellationToken);
                        validInput = false;
                    }

                    // Have we got a user id?   
                    if (validInput && userId != null)
                    {
                        meetingState.Attendees.Add(new AttendeeCallInfo
                        {
                            Id = userId,
                            DisplayName = addContactActionInfo.ContactId,
                            Type = GroupMeetingAttendeeType.Teams
                        });
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
                    meetingState.Attendees.Add(new AttendeeCallInfo
                    {
                        Id = addContactActionInfo.ContactId,
                        Type = GroupMeetingAttendeeType.Phone,
                        DisplayName = addContactActionInfo.ContactId
                    });
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
