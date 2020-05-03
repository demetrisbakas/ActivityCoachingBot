﻿using CoreBot;
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
    public class TopFiveDialog : CancelAndHelpDialog
    {
        private bool finished = false;
        private string activeQuestion;

        public TopFiveDialog()
           : base(nameof(TopFiveDialog))
        {
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                AskQuestionStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> AskQuestionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //foreach (QuestionTopFive obj in MainDialog.Response.questionnaire)
            //{
            //    if (!PersonalDetailsDialog.PersonalDetails.QuestionnaireAnswers.ContainsKey(obj))

            foreach (QuestionTopFive obj in MainDialog.Response.questionnaire)
            {
                if (!PersonalDetailsDialog.PersonalDetails.QuestionnaireAnswers.Any(l => l.Key == obj.Question))
                {
                    activeQuestion = obj.Question;
                    var question = $"I see Myself as Someone Who\n\n{activeQuestion}";
                    var promptMessage = MessageFactory.Text(question, question, InputHints.ExpectingInput);
                    var retryText = $"Please choose one option.\n\n{question}";
                    var retryPromptText = MessageFactory.Text(retryText, retryText, InputHints.ExpectingInput);
                    var answerChoice = obj.Answers;

                    return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = promptMessage, Choices = answerChoice, RetryPrompt = retryPromptText }, cancellationToken);
                }
            }

            finished = true;
            return await stepContext.NextAsync(PersonalDetailsDialog.PersonalDetails.QuestionnaireAnswers, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (finished)
            {
                // Sand to DB
                var changes = new Dictionary<string, object>();
                {
                    changes.Add(PersonalDetailsDialog.PersonalDetails.UserID, PersonalDetailsDialog.PersonalDetails);
                }
                await MainDialog.CosmosDBQuery.WriteAsync(changes, cancellationToken);
                return await stepContext.EndDialogAsync(PersonalDetailsDialog.PersonalDetails.QuestionnaireAnswers, cancellationToken);
            }
            else
            {
                PersonalDetailsDialog.PersonalDetails.QuestionnaireAnswers.Add(new KeyValuePair<string, string>(activeQuestion, ((FoundChoice)stepContext.Result).Value));
                return await stepContext.BeginDialogAsync(nameof(TopFiveDialog), PersonalDetailsDialog.PersonalDetails.QuestionnaireAnswers, cancellationToken);
            }
        }
    }
}
