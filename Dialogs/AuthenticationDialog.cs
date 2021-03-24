using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bot.AdaptiveCard.Prompt;
using AdaptiveCardPromptSample.Welcome;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class AuthenticationDialog : CancelAndHelpDialog
    {
        private const string password = "mypassword";
        static string AdaptivePromptId = "adaptive";

        public AuthenticationDialog()
           : base(nameof(AuthenticationDialog))
        {
            AddDialog(new AdaptiveCardPrompt(AdaptivePromptId));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new AttachmentPrompt(nameof(AttachmentPrompt)));
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
            var cardJson = PrepareCard.ReadCard("passphrase.json");

            var cardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(cardJson),
            };

            var opts = new PromptOptions
            {
                Prompt = new Activity
                {
                    Attachments = new List<Attachment>() { cardAttachment },
                    Type = ActivityTypes.Message,
                    //Text = "Enter your passphrase",
                }
            };

            return await stepContext.PromptAsync(AdaptivePromptId, opts, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var result = JsonConvert.DeserializeObject<PassJson>(stepContext.Result.ToString());

            if (password == result.Pass)
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
