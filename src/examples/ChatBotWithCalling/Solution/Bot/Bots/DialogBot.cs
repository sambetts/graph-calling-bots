using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Bot.Builder.Dialogs;

namespace GroupCallingChatBot.Bots;

public abstract class DialogBot<T> : TeamsActivityHandler where T : Dialog
{
    protected readonly BotState _conversationState;
    protected readonly Dialog _dialog;
    protected readonly ILogger _logger;
    protected readonly BotState _userState;

    public DialogBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger)
    {
        _conversationState = conversationState;
        _userState = userState;
        _dialog = dialog;
        _logger = logger;
    }

    public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(turnContext.Activity.Text) && turnContext.Activity.Value != null)
        {
            turnContext.Activity.Text = Newtonsoft.Json.JsonConvert.SerializeObject(turnContext.Activity.Value);
        }

        await base.OnTurnAsync(turnContext, cancellationToken);

        // Save any state changes that might have occurred during the turn.
        await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running dialog with Message Activity.");

        // Run the Dialog with the new message Activity.
        await _dialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
    }

    protected override async Task OnTeamsSigninVerifyStateAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running dialog with signin/verifystate from an Invoke Activity.");

        // The OAuth Prompt needs to see the Invoke Activity in order to complete the login process.

        // Run the Dialog with the new Invoke Activity.
        await _dialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
    }
}
