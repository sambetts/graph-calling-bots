using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration.UserSecrets;
using System.Threading.Tasks;
using System.Threading;
using Bot.Dialogues.Utils;
using Bot.AdaptiveCards;

namespace Bot.Dialogues;


/// <summary>
/// Entrypoint to all new conversations
/// </summary>
public class MainDialog : CancellableDialogue
{
    public MainDialog() : base(nameof(MainDialog))
    {
        AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
        {
            Act
        }));
        InitialDialogId = nameof(WaterfallDialog);
    }

    /// <summary>
    /// Main entry-point for bot new chat
    /// </summary>
    private async Task<DialogTurnResult> Act(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var inputText = stepContext.Context.Activity.Text ?? string.Empty;
        var val = stepContext.Context.Activity.Value ?? string.Empty;

        // Text response or adaptive-card action?
        if (val != null && !string.IsNullOrEmpty(val.ToString()))
        {
            return await HandleCardResponse(stepContext, val.ToString()!, cancellationToken);
        }
        else
        {
            var command = inputText.ToLower();

            // No idea what to do. Send this
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                    $"Hi, sorry, I didn't get that..."
                ), cancellationToken);

            var introCardAttachment = new MenuCard().GetCardAttachment();
            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(introCardAttachment));

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }


    /// <summary>
    /// Someone replied via an Adaptive card form
    /// </summary>
    public static async Task<DialogTurnResult> HandleCardResponse(WaterfallStepContext stepContext, string submitJson, CancellationToken cancellationToken)
    {
        // Form action
        var action = AdaptiveCardUtils.GetAdaptiveCardAction(submitJson, stepContext.Context.Activity.From.AadObjectId);

        // Figure out what was done

        // Something else
        return await ReplyWithNoIdeaAndEndDiag(stepContext, cancellationToken);

    }


    public static async Task<DialogTurnResult> ReplyWithNoIdeaAndEndDiag(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {

        await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                $"You sent me something but I can't work out what, sorry! Try again?."
                ), cancellationToken);
        return await stepContext.EndDialogAsync(null);
    }
}
