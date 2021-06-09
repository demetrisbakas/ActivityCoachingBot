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
using CoreBot;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class UploadTipsDialog : CancelAndHelpDialog
    {
        static string AdaptivePromptId = "adaptive";

        public UploadTipsDialog()
           : base(nameof(UploadTipsDialog))
        {
            AddDialog(new AdaptiveCardPrompt(AdaptivePromptId));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ShowCardAsync,
                FinalStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ShowCardAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var cardJson = PrepareCard.ReadCard("tipCard.json");

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
                    Type = ActivityTypes.Message
                }
            };

            return await stepContext.PromptAsync(AdaptivePromptId, opts, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var result = JsonConvert.DeserializeObject<Tip>(stepContext.Result.ToString());
            NumberOfTipsDialog.TipsUploadList.Add(NullifyFalseValues(result));

            if (--NumberOfTipsDialog.NumberOfTips == 0)
            {
                // Send to database
                MainDialog.SendTipsToDB(NumberOfTipsDialog.TipsUploadList, stepContext, cancellationToken);

                // Show uploading message to the user
                var MsgText = MainDialog.Response.UploadingDataMessage();
                var promptMessage = MessageFactory.Text(MsgText, MsgText, InputHints.ExpectingInput);
                await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

                return await stepContext.EndDialogAsync(PersonalDetailsDialog.PersonalDetails, cancellationToken);
            }
            else
                return await stepContext.BeginDialogAsync(nameof(UploadTipsDialog), NumberOfTipsDialog.NumberOfTips, cancellationToken);
        }

        // Converts flase values of a tip to null values for better processing with rules engine
        private Tip NullifyFalseValues(Tip input)
        {
            if (input.Smoker == false)
                input.Smoker = null;
            if (input.LowWaterConsumption == false)
                input.LowWaterConsumption = null;
            if (input.LowSleep == false)
                input.LowSleep = null;
            if (input.LowPhysicalActivity == false)
                input.LowPhysicalActivity = null;

            return input;
        }
    }
}
