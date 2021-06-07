using CoreBot;
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
    public class QuestionnaireChoiceDialog : CancelAndHelpDialog
    {
        public static List<QuestionTopFive> activeQuestionnaire { get; set; }

        public QuestionnaireChoiceDialog()
           : base(nameof(QuestionnaireChoiceDialog))
        {
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ChooseQuestionnaireStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ChooseQuestionnaireStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var choiceMsgText = MainDialog.Response.ChooseQuestionnaire();
            var promptMessage = MessageFactory.Text(choiceMsgText, choiceMsgText, InputHints.ExpectingInput);
            var retryText = $"Please choose one option.\n\n{choiceMsgText}";
            var retryPromptText = MessageFactory.Text(retryText, retryText, InputHints.ExpectingInput);

            var questionnaireChoice = new List<Choice>();
            var questionnairesNames = new List<string>();

            // Wait for the questionaires to be fetched from the database
            MainDialog.Response.Questionnaires = await MainDialog.Questionnaires;

            foreach (KeyValuePair<string, List<QuestionTopFive>> kvp in MainDialog.Response.Questionnaires)
            {
                questionnairesNames.Add(kvp.Key);
            }
            foreach (string name in questionnairesNames)
            {
                questionnaireChoice.Add(new Choice(name));
            }

            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = promptMessage, Choices = questionnaireChoice, RetryPrompt = retryPromptText, Style = ListStyle.HeroCard }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            activeQuestionnaire = (from kvp in MainDialog.Response.Questionnaires where kvp.Key == ((FoundChoice)stepContext.Result).Value select kvp.Value).FirstOrDefault();

            return await stepContext.BeginDialogAsync(nameof(TopFiveDialog), PersonalDetailsDialog.PersonalDetails, cancellationToken);
        }
    }
}
