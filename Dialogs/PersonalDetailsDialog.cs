using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class PersonalDetailsDialog : CancelAndHelpDialog
    {
        private readonly string NameStepMsgText = MainDialog.Response.AskName();
        private readonly string AgeStepMsgText = MainDialog.Response.AskAge();
        private readonly string SexStepMsgText = MainDialog.Response.AskSex();

        public PersonalDetailsDialog()
            : base(nameof(PersonalDetailsDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                NameStepAsync,
                AgeStepAsync,
                SexStepAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var personalDetails = (PersonalDetails)stepContext.Options;

            if (personalDetails.Name == null)
            {
                var promptMessage = MessageFactory.Text(NameStepMsgText, NameStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(personalDetails.Name, cancellationToken);
        }

        private async Task<DialogTurnResult> AgeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var personalDetails = (PersonalDetails)stepContext.Options;

            personalDetails.Name = (string)stepContext.Result;

            // Need to make int work as null
            if (personalDetails.Age == null)
            {
                var promptMessage = MessageFactory.Text(AgeStepMsgText, AgeStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(personalDetails.Age, cancellationToken);
        }

        private async Task<DialogTurnResult> SexStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var personalDetails= (PersonalDetails)stepContext.Options;

            personalDetails.Age = (string)stepContext.Result;

            // Need to find a more suitable type
            if (personalDetails.Sex == null)
            {
                var promptMessage = MessageFactory.Text(SexStepMsgText, SexStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(personalDetails.Sex, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var personalDetails = (PersonalDetails)stepContext.Options;

            personalDetails.Sex = (string)stepContext.Result;

            var messageText = $"Please confirm, this is yourpersonal info: Name: {personalDetails.Name} Age: {personalDetails.Age} Sex: {personalDetails.Sex}. Is this correct?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                var personalDetails = (PersonalDetails)stepContext.Options;

                return await stepContext.EndDialogAsync(personalDetails, cancellationToken);
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        //private static bool IsAmbiguous(string timex)
        //{
        //    var timexProperty = new TimexProperty(timex);
        //    return !timexProperty.Types.Contains(Constants.TimexTypes.Definite);
        //}
    }
}
