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
    public class QuestionnaireChoiceDialog : CancelAndHelpDialog
    {
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
            //return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

            var questionnaireChoice = new List<Choice>() { new Choice("Big Five") };

            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = promptMessage, Choices = questionnaireChoice, RetryPrompt = retryPromptText }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            switch (((FoundChoice)stepContext.Result).Index)
            {
                case 0:
                    return await stepContext.BeginDialogAsync(nameof(TopFiveDialog), PersonalDetailsDialog.PersonalDetails, cancellationToken);

                default:
                    return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }
    }
}