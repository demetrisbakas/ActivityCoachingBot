using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System.Text.RegularExpressions;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class PersonalDetailsDialog : CancelAndHelpDialog
    {
        public static PersonalDetails PersonalDetails { get; set; } = new PersonalDetails();
        private LuisModel luisResult;
        private bool finished = false;

        private enum Validator
        {
            Name,
            Age
        };

        public PersonalDetailsDialog()
            : base(nameof(PersonalDetailsDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt), TextPromptValidatorAsync));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new NumberPrompt<int>(nameof(NumberPrompt<int>)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                NameStepAsync,
                AgeStepAsync,
                SexStepAsync,
                SmokerStepAsync,
                WaterStepAsync,
                SleepStepAsync,
                PhysicalActivityStepAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }


        private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Skip questions if already collected the data
            if (PersonalDetails.Name != null && PersonalDetails.Age != null && PersonalDetails.Sex != null && PersonalDetails.Smoker != null && PersonalDetails.WaterConsumption != null && PersonalDetails.Sleep != null && PersonalDetails.PhysicalActivity != null)
                finished = true;

            if (PersonalDetails.Name == null)
            {
                var NameStepMsgText = MainDialog.Response.AskName();
                var promptMessage = MessageFactory.Text(NameStepMsgText, NameStepMsgText, InputHints.ExpectingInput);
                var retryText = MainDialog.Response.RetryName();
                var retryMessage = MessageFactory.Text(retryText, retryText, InputHints.ExpectingInput);

                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage, RetryPrompt = retryMessage, Validations = Validator.Name }, cancellationToken);
            }

            return await stepContext.NextAsync(PersonalDetails.Name, cancellationToken);
        }

        private async Task<DialogTurnResult> AgeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Send to DB
            MainDialog.WriteToDB(stepContext, cancellationToken);

            if (PersonalDetails.Age == null)
            {
                var AgeStepMsgText = MainDialog.Response.AskAge();
                var promptMessage = MessageFactory.Text(AgeStepMsgText, AgeStepMsgText, InputHints.ExpectingInput);
                var retryText = MainDialog.Response.RetryAge();
                var retryMessage = MessageFactory.Text(retryText, retryText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage, RetryPrompt = retryMessage, Validations = Validator.Age }, cancellationToken);
            }

            return await stepContext.NextAsync(PersonalDetails.Age, cancellationToken);
        }

        private async Task<DialogTurnResult> SexStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Send to DB
            MainDialog.WriteToDB(stepContext, cancellationToken);

            if (PersonalDetails.Sex == null)
            {
                var SexStepMsgText = MainDialog.Response.AskSex();
                var promptMessage = MessageFactory.Text(SexStepMsgText, SexStepMsgText, InputHints.ExpectingInput);
                var retryText = $"Please choose one option.\n\n{SexStepMsgText}";
                var retryPromptText = MessageFactory.Text(retryText, retryText, InputHints.ExpectingInput);

                var sexChoice = new List<Choice>() { new Choice("Male"), new Choice("Female") };

                return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = promptMessage, Choices = sexChoice, RetryPrompt = retryPromptText, Style = ListStyle.HeroCard }, cancellationToken);
            }

            return await stepContext.NextAsync(PersonalDetails.Sex, cancellationToken);
        }

        private async Task<DialogTurnResult> SmokerStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (PersonalDetails.Sex == null)
                PersonalDetails.Sex = ((FoundChoice)stepContext.Result).Value;

            // Send to DB
            MainDialog.WriteToDB(stepContext, cancellationToken);

            if (PersonalDetails.Smoker == null)
            {
                var messageText = MainDialog.Response.AskSmoker();
                var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }
            else
                return await stepContext.NextAsync(PersonalDetails, cancellationToken);
        }

        private async Task<DialogTurnResult> WaterStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (PersonalDetails.Smoker == null)
                PersonalDetails.Smoker = (bool)stepContext.Result;

            // Send to DB
            MainDialog.WriteToDB(stepContext, cancellationToken);

            if (PersonalDetails.WaterConsumption == null)
            {
                var messageText = MainDialog.Response.AskWater();
                var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                var retryText = MainDialog.Response.RetryWater();
                var retryPromptText = MessageFactory.Text(retryText, retryText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(NumberPrompt<int>), new PromptOptions { Prompt = promptMessage, RetryPrompt = retryPromptText }, cancellationToken);
            }
            else
                return await stepContext.NextAsync(PersonalDetails, cancellationToken);
        }

        private async Task<DialogTurnResult> SleepStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (PersonalDetails.WaterConsumption == null)
                PersonalDetails.WaterConsumption = (int)stepContext.Result;

            // Send to DB
            MainDialog.WriteToDB(stepContext, cancellationToken);

            if (PersonalDetails.Sleep == null)
            {
                var messageText = MainDialog.Response.AskSleep();
                var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                var retryText = MainDialog.Response.RetrySleep();
                var retryPromptText = MessageFactory.Text(retryText, retryText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(NumberPrompt<int>), new PromptOptions { Prompt = promptMessage, RetryPrompt = retryPromptText }, cancellationToken);
            }
            else
                return await stepContext.NextAsync(PersonalDetails, cancellationToken);
        }

        private async Task<DialogTurnResult> PhysicalActivityStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (PersonalDetails.Sleep == null)
                PersonalDetails.Sleep = (int)stepContext.Result;

            // Send to DB
            MainDialog.WriteToDB(stepContext, cancellationToken);

            if (PersonalDetails.PhysicalActivity == null)
            {
                var messageText = MainDialog.Response.AskPhysicalActivity();
                var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                var retryText = MainDialog.Response.RetryPhysycalActivity();
                var retryPromptText = MessageFactory.Text(retryText, retryText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(NumberPrompt<int>), new PromptOptions { Prompt = promptMessage, RetryPrompt = retryPromptText }, cancellationToken);
            }
            else
                return await stepContext.NextAsync(PersonalDetails, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (PersonalDetails.PhysicalActivity == null)
                PersonalDetails.PhysicalActivity = (int)stepContext.Result;

            // Send to DB
            MainDialog.WriteToDB(stepContext, cancellationToken);

            var messageText = $"Please confirm, this is your personal info:\n\nName: {PersonalDetails.Name}\n\nAge: {PersonalDetails.Age}\n\nSex: {PersonalDetails.Sex}\n\nSmoker: {PersonalDetails.Smoker}\n\nWater Consumption: {PersonalDetails.WaterConsumption} cups per day\n\nSleep: {PersonalDetails.Sleep} hours per day\n\nPhysical Activity: {PersonalDetails.PhysicalActivity} hours per week\n\nIs this correct?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);

            if (!finished)
                return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            else
            {
                return await stepContext.NextAsync(PersonalDetails, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (finished)
            {
                finished = false;
                return await stepContext.BeginDialogAsync(nameof(QuestionnaireChoiceDialog), PersonalDetails, cancellationToken);
            }
            else if ((bool)stepContext.Result)
            {
                // Send to DB
                MainDialog.WriteToDB(stepContext, cancellationToken);

                return await stepContext.BeginDialogAsync(nameof(QuestionnaireChoiceDialog), PersonalDetails, cancellationToken);
            }
            else
            {
                ClearDetails();

                return await stepContext.BeginDialogAsync(nameof(PersonalDetailsDialog), PersonalDetails, cancellationToken);
            }
        }

        private async Task<bool> TextPromptValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            switch (promptContext.Options.Validations != null ? (Validator)promptContext.Options.Validations : (Validator)(-1))
            {
                case Validator.Name:
                    luisResult = await MainDialog.Get_luisRecognizer().RecognizeAsync<LuisModel>(promptContext.Context, cancellationToken);
                    PersonalDetails.Name = (luisResult.Entities.personName != null ? char.ToUpper(luisResult.Entities.personName[0][0]) + luisResult.Entities.personName[0].Substring(1) : null);

                    if (PersonalDetails.Name == null)
                        return await Task.FromResult(false);
                    else
                        return await Task.FromResult(true);

                case Validator.Age:


                    luisResult = await MainDialog.Get_luisRecognizer().RecognizeAsync<LuisModel>(promptContext.Context, cancellationToken);
                    if (luisResult.Entities.age != null)
                        PersonalDetails.Age = (int?)luisResult.Entities.age[0].Number;
                    else if (Regex.Match(promptContext.Context.Activity.Text, @"\d+").Value != "")
                        // Second check, just in case. Returns null if parse fails
                        PersonalDetails.Age = Int32.TryParse(Regex.Match(promptContext.Context.Activity.Text, @"\d+").Value, out var tempVal) ? tempVal : (int?)null;

                    if (PersonalDetails.Age == null)
                        return await Task.FromResult(false);
                    else
                        return await Task.FromResult(true);

                default:
                    return await Task.FromResult(true);
            }
        }

        // Clears personal details
        public static void ClearDetails()
        {
            PersonalDetails.Name = null;
            PersonalDetails.Age = null;
            PersonalDetails.Sex = null;
            PersonalDetails.Smoker = null;
            PersonalDetails.WaterConsumption = null;
            PersonalDetails.Sleep = null;
            PersonalDetails.PhysicalActivity = null;
        }
    }
}
