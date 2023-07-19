using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Threading.Tasks;
using System.Threading;

namespace GroupCallingChatBot.Web.Dialogues;

public abstract class CancellableDialogue : ComponentDialog
{
    private const string CancelMsgText = "Ok then; I was bored of this conversation anyway.";

    public CancellableDialogue(string dialogId) : base(dialogId)
    {
    }

    protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
    {
        // Is this user response a cancel request?
        var result = await InterruptAsync(innerDc, cancellationToken);
        if (result != null)
        {
            return result;
        }

        // Otherwise continue
        return await base.OnContinueDialogAsync(innerDc, cancellationToken);
    }

    private async Task<DialogTurnResult?> InterruptAsync(DialogContext innerDc, CancellationToken cancellationToken)
    {
        if (innerDc.Context.Activity.Type == ActivityTypes.Message)
        {
            var text = innerDc.Context.Activity.Text?.ToLowerInvariant();

            switch (text)
            {
                case "cancel":
                case "quit":
                    var cancelMessage = MessageFactory.Text(CancelMsgText, CancelMsgText, InputHints.IgnoringInput);
                    await innerDc.Context.SendActivityAsync(cancelMessage, cancellationToken);
                    return await innerDc.CancelAllDialogsAsync(cancellationToken);
            }
        }

        return null;
    }

}