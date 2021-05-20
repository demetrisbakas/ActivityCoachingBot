﻿using CoreBot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class NameOfQuestionnaireDialog : CancelAndHelpDialog
    {
        public static string NameOfQuestionnaire { get; set; }
        public static int NumberOfQuestionsInQuestionnaire { get; set; }
        public static List<QuestionTopFive> QuestionnaireToUpload { get; set; } = new List<QuestionTopFive>();
    //public static KeyValuePair<string, List<QuestionTopFive>> QuestionnaireToUpload { get; set; } = new KeyValuePair<string, List<QuestionTopFive>>();

    public NameOfQuestionnaireDialog()
           : base(nameof(NameOfQuestionnaireDialog))
        {
            AddDialog(new NumberPrompt<int>(nameof(NumberPrompt<int>), PositiveNumberValidatorAsync));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                AskNameStepAsync,
                AskNumberOfQuestionsStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> AskNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Empties any previous questions
            QuestionnaireToUpload = new List<QuestionTopFive>();

            var messageText = MainDialog.Response.EnterNameOfQuestionnaire();
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> AskNumberOfQuestionsStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            NameOfQuestionnaire = stepContext.Result.ToString();

            var messageText = MainDialog.Response.EnterNumberOfQuestions();
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            var retryMessageText = MainDialog.Response.ReenterNumberOfQuestions();
            var retryPromptMessage = MessageFactory.Text(retryMessageText, retryMessageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(NumberPrompt<int>), new PromptOptions { Prompt = promptMessage, RetryPrompt = retryPromptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            NumberOfQuestionsInQuestionnaire = (int)stepContext.Result;

            return await stepContext.BeginDialogAsync(nameof(UploadQuestionnairesDialog), NumberOfQuestionsInQuestionnaire, cancellationToken);
        }

        private async Task<bool> PositiveNumberValidatorAsync(PromptValidatorContext<int> promptContext, CancellationToken cancellationToken)
        {
            if (Regex.Match(promptContext.Context.Activity.Text, @"\d+").Value != "")
            {
                var result = Int32.TryParse(Regex.Match(promptContext.Context.Activity.Text, @"\d+").Value, out var tempVal) ? tempVal : (int?)null;
                if (result > 0)
                {
                    NumberOfQuestionsInQuestionnaire = (int)result;
                    return await Task.FromResult(true);
                }
            }

            return await Task.FromResult(false);
        }
    }
}
