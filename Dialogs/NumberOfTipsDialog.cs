using CoreBot;
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
    public class NumberOfTipsDialog : CancelAndHelpDialog
    {
        public static int NumberOfTips { get; set; }
        public static List<Tip> TipsUploadList { get; set;} = new List<Tip>();

        public NumberOfTipsDialog()
           : base(nameof(NumberOfTipsDialog))
        {
            AddDialog(new NumberPrompt<int>(nameof(NumberPrompt<int>)));
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
            var messageText = MainDialog.Response.EnterNumberOfTips();
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            var retryMessageText = MainDialog.Response.ReenterNumberOfTips();
            var retryPromptMessage = MessageFactory.Text(retryMessageText, retryMessageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(NumberPrompt<int>), new PromptOptions { Prompt = promptMessage, RetryPrompt = retryPromptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            NumberOfTips = (int)stepContext.Result;
            TipsUploadList = new List<Tip>();

            return await stepContext.BeginDialogAsync(nameof(UploadTipsDialog), NumberOfTips, cancellationToken);
        }
    }
}
