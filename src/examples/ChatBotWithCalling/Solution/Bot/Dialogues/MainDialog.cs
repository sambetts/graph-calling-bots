﻿using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder;
using System.Threading.Tasks;
using System.Threading;
using Bot.Dialogues.Utils;
using Bot.AdaptiveCards;
using System;
using System.Collections.Generic;

namespace Bot.Dialogues;

/// <summary>
/// Entrypoint to all new conversations
/// </summary>
public class MainDialog : CancellableDialogue
{
    private readonly UserState _userState;

    public MainDialog(UserState userState) : base(nameof(MainDialog))
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
    }


    /// <summary>
    /// Main entry-point for bot new chat
    /// </summary>
    private async Task<DialogTurnResult> SendBotMenu(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // Get user state
        var userStateAccessors = _userState.CreateProperty<MeetingState>(nameof(MeetingState));
        var meetingState = await userStateAccessors.GetAsync(stepContext.Context, () => new MeetingState());

        var val = stepContext.Context.Activity.Value ?? string.Empty;

        // Form action
        var actionInfo = AdaptiveCardUtils.GetAdaptiveCardAction(val?.ToString(), stepContext.Context.Activity.From.AadObjectId) ?? new ActionResponse();

        if (actionInfo != null)
        {
            if (actionInfo.Action == CardConstants.CardActionValCreateMeeting)
            {
                if (!meetingState.IsMeetingCreated)
                {
                    // Create meeting
                    meetingState.Created = DateTime.Now;
                    meetingState.MeetingUrl = "123";

                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                            $"Meeting created. Anything else?"
                        ), cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                            $"Meeting already created! Anything else?"
                        ), cancellationToken);
                }
            }
            else if (actionInfo.Action == CardConstants.CardActionValAddNumber)
            {
                if (!meetingState.IsMeetingCreated)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                            $"Meeting not created, so I can't do that. Anything else?"
                        ), cancellationToken);
                }
                else
                {
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions()
                    {
                        Prompt = MessageFactory.Text("What number should we add to the meeting?"),
                        RetryPrompt = MessageFactory.Text("I need a phone number")
                    });
                }
            }
            else
            {
                // No idea what to do. Send this and end diag
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                        $"Hi, sorry, I didn't get that...use the menu buttons"
                    ), cancellationToken);
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
        var number = (string)stepContext.Result;

        // Get user state
        var userStateAccessors = _userState.CreateProperty<MeetingState>(nameof(MeetingState));
        var meetingState = await userStateAccessors.GetAsync(stepContext.Context, () => new MeetingState());

        if (NumberCallState.IsValidNumber(number))
        {

            // Add number
            meetingState.Numbers.Add(new NumberCallState()
            {
                Number = number
            });

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                                $"Number added"
                            ), cancellationToken);


            var introCardAttachment = new MenuCard(meetingState).GetCardAttachment();
            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(introCardAttachment));
        }
        else
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                                $"Invalid number"
                            ), cancellationToken);
        }

        return await stepContext.EndDialogAsync(meetingState, cancellationToken);

    }

    public class MeetingState
    {
        public DateTime Created { get; set; }
        public string MeetingUrl { get; set; } = string.Empty;

        public bool IsMeetingCreated => !string.IsNullOrEmpty(MeetingUrl);
        public List<NumberCallState> Numbers { get; set; } = new();
    }

    public class NumberCallState
    {
        public string Number { get; set; } = null!;

        internal static bool IsValidNumber(string number)
        {
            return !string.IsNullOrEmpty(number);
        }
    }
}