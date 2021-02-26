using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class AuthenticationDialog : CancelAndHelpDialog
    {
        private const string password = "mypassword";

        public AuthenticationDialog()
           : base(nameof(AuthenticationDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                AuthenticateAsync,
                FinalStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> AuthenticateAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var NameStepMsgText = MainDialog.Response.AskPassword();
            var promptMessage = MessageFactory.Text(NameStepMsgText, NameStepMsgText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (password == stepContext.Result.ToString())
            {
                return await stepContext.BeginDialogAsync(nameof(UploadTipsOrQuestionnairesDialog), PersonalDetailsDialog.PersonalDetails, cancellationToken);
            }
            else
            {
                var messageText = MainDialog.Response.WrongPassword();
                var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
                return await stepContext.EndDialogAsync(PersonalDetailsDialog.PersonalDetails, cancellationToken);
            }
        }
    }
}
