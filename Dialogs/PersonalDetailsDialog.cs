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

//
using Microsoft.Extensions.Configuration;
//

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class PersonalDetailsDialog : CancelAndHelpDialog
    {
        //
        //static IConfiguration configuration;
        //private readonly FlightBookingRecognizer _luisRecognizer = new FlightBookingRecognizer(configuration);
        //
        FlightBooking luisResult;

        private string NameStepMsgText;
        private string AgeStepMsgText;
        private string SexStepMsgText;

        private enum Validator
        {
            Name,
            Age
        };

        public static PersonalDetails PersonalDetails { get; set; } = new PersonalDetails();

        public PersonalDetailsDialog()
            : base(nameof(PersonalDetailsDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt), TextPromptValidatorAsync));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
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
            //PersonalDetails = (PersonalDetails)stepContext.Options;

            if (PersonalDetails.Name == null)
            {
                NameStepMsgText = MainDialog.Response.AskName();
                var promptMessage = MessageFactory.Text(NameStepMsgText, NameStepMsgText, InputHints.ExpectingInput);

                //
                //var temp = _luisRecognizer.IsConfigured;
                //luisResult = await MainDialog._luisRecognizer.RecognizeAsync<FlightBooking>(stepContext.Context, cancellationToken);
                //string name = (luisResult.Entities.personName != null ? char.ToUpper(luisResult.Entities.personName[0][0]) + luisResult.Entities.personName[0].Substring(1) : null);
                //
                //PromptValidator<int> nameValidator = 5; 
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage, RetryPrompt = MessageFactory.Text("Retriyng... Name"), Validations = Validator.Name }, cancellationToken);
            }

            return await stepContext.NextAsync(PersonalDetails.Name, cancellationToken);
        }

        private async Task<DialogTurnResult> AgeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //PersonalDetails = (PersonalDetails)stepContext.Options;

            //if (PersonalDetails.Name == null)
            //{
            //    luisResult = await MainDialog.Get_luisRecognizer().RecognizeAsync<FlightBooking>(stepContext.Context, cancellationToken);
            //    PersonalDetails.Name = (luisResult.Entities.personName != null ? char.ToUpper(luisResult.Entities.personName[0][0]) + luisResult.Entities.personName[0].Substring(1) : null);
            //    //PersonalDetails.Name = (string)stepContext.Result;
            //}

            // Need to make int work as null
            if (PersonalDetails.Age == null)
            {
                AgeStepMsgText = MainDialog.Response.AskAge();
                var promptMessage = MessageFactory.Text(AgeStepMsgText, AgeStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage, RetryPrompt = MessageFactory.Text("Retriyng... Age") }, cancellationToken);
            }

            return await stepContext.NextAsync(PersonalDetails.Age, cancellationToken);
        }

        private async Task<DialogTurnResult> SexStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //PersonalDetails= (PersonalDetails)stepContext.Options;

            //PersonalDetails.Age = Regex.Match((string)stepContext.Result, @"\d+").Value;
            if (PersonalDetails.Age == null)
            {
                luisResult = await MainDialog.Get_luisRecognizer().RecognizeAsync<FlightBooking>(stepContext.Context, cancellationToken);
                PersonalDetails.Age = (luisResult.Entities.age != null ? luisResult.Entities.age[0].Number.ToString() : Regex.Match((string)stepContext.Result, @"\d+").Value);
            }

            //personalDetails.Age = (string)stepContext.Result;

            //if(PersonalDetails.Age == null)
            //{
            //    string errorMessage = "Didnt get that, lets try again.";
            //    var promptMessage = MessageFactory.Text(errorMessage, errorMessage, InputHints.ExpectingInput);
            //    await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);


            //    return await stepContext.ReplaceDialogAsync(nameof(PersonalDetailsDialog.AgeStepAsync), PersonalDetails, cancellationToken);
            //}

            // Need to find a more suitable type
            if (PersonalDetails.Sex == null)
            {
                SexStepMsgText = MainDialog.Response.AskSex();
                var promptMessage = MessageFactory.Text(SexStepMsgText, SexStepMsgText, InputHints.ExpectingInput);
                var retryPromptText = MessageFactory.Text($"Please choose one option.\n\n{SexStepMsgText}");
                //return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

                var sexChoice = new List<Choice>() { new Choice("Male"), new Choice("Female") };

                return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = promptMessage, Choices = sexChoice, RetryPrompt = retryPromptText }, cancellationToken);
            }

            return await stepContext.NextAsync(PersonalDetails.Sex, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //PersonalDetails = (PersonalDetails)stepContext.Options;

            //if(Regex.IsMatch((string)stepContext.Result, "female", RegexOptions.IgnoreCase))
            //    PersonalDetails.Sex = "Female";
            //else if (Regex.IsMatch((string)stepContext.Result, "male", RegexOptions.IgnoreCase))
            //    PersonalDetails.Sex = "Male";
            //else
            //    PersonalDetails.Sex = null;


            if (PersonalDetails.Sex == null)
                //PersonalDetails.Sex = stepContext.Result.ToString();
                PersonalDetails.Sex = char.ToUpper(stepContext.Context.Activity.Text[0]) + stepContext.Context.Activity.Text.Substring(1).ToLower();


            var messageText = $"Please confirm, this is your personal info:\n\nName: {PersonalDetails.Name}\n\nAge: {PersonalDetails.Age}\n\nSex: {PersonalDetails.Sex}\n\nIs this correct?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                //var personalDetails = (PersonalDetails)stepContext.Options;

                // Upload to database

                // Wipe the personalDetails for testing purposes
                //PersonalDetails = new PersonalDetails();

                return await stepContext.EndDialogAsync(PersonalDetails, cancellationToken);
            }
            else
            {
                PersonalDetails = new PersonalDetails();

                return await stepContext.BeginDialogAsync(nameof(PersonalDetailsDialog), PersonalDetails, cancellationToken);
            }

            //return await stepContext.EndDialogAsync(null, cancellationToken);
        }





        private async Task<bool> TextPromptValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            //throw new NotImplementedException();

            switch (promptContext.Options.Validations != null ? (Validator)promptContext.Options.Validations : (Validator)(-1))
            {
                case Validator.Name:
                    luisResult = await MainDialog.Get_luisRecognizer().RecognizeAsync<FlightBooking>(promptContext.Context, cancellationToken);
                    PersonalDetails.Name = (luisResult.Entities.personName != null ? char.ToUpper(luisResult.Entities.personName[0][0]) + luisResult.Entities.personName[0].Substring(1) : null);

                    if (luisResult.Entities.personName == null)
                        return await Task.FromResult(false);
                    else
                        return await Task.FromResult(true);

                case Validator.Age:
                    return await Task.FromResult(true);

                default:
                    return await Task.FromResult(true);
            }
        }



        //private static bool IsAmbiguous(string timex)
        //{
        //    var timexProperty = new TimexProperty(timex);
        //    return !timexProperty.Types.Contains(Constants.TimexTypes.Definite);
        //}
    }
}
