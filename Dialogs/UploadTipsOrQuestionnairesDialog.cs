using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class UploadTipsOrQuestionnairesDialog : CancelAndHelpDialog
    {
        private List<Choice> uploadChoice = new List<Choice>() { new Choice("Tips"), new Choice("Questionnaires") };
        private string answer;

        public UploadTipsOrQuestionnairesDialog()
           : base(nameof(UploadTipsOrQuestionnairesDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ChooseUploadAsync,
                ChoseTips,
                ChoseQuestionnaires
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ChooseUploadAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var MsgText = MainDialog.Response.UploadTipsOrQuestionnaires();
            var promptMessage = MessageFactory.Text(MsgText, MsgText, InputHints.ExpectingInput);
            var retryText = $"Please choose one option.\n\n{MsgText}";
            var retryPromptText = MessageFactory.Text(retryText, retryText, InputHints.ExpectingInput);
            //return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = promptMessage, Choices = uploadChoice, RetryPrompt = retryPromptText, Style = ListStyle.HeroCard }, cancellationToken);
        }

        private async Task<DialogTurnResult> ChoseTips(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            answer = ((FoundChoice)stepContext.Result).Value;
            if (answer == uploadChoice?[0]?.Value)
            {
                // Change dialog
                return await stepContext.BeginDialogAsync(nameof(NumberOfTipsDialog), PersonalDetailsDialog.PersonalDetails, cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(PersonalDetailsDialog.PersonalDetails, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> ChoseQuestionnaires(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (answer == uploadChoice?[1]?.Value)
            {
                // Change dialog
                return await stepContext.BeginDialogAsync(nameof(QuestionnaireChoiceDialog), PersonalDetailsDialog.PersonalDetails, cancellationToken);
            }
            else
            {
                return await stepContext.EndDialogAsync(PersonalDetailsDialog.PersonalDetails, cancellationToken);
            }
        }
    }
}
