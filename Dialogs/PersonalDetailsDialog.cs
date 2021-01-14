﻿using System;
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
using Microsoft.Bot.Connector.Authentication;
using Microsoft.ML;
using Microsoft.ML.Data;
//

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class PersonalDetailsDialog : CancelAndHelpDialog
    {
        public static PersonalDetails PersonalDetails { get; set; } = new PersonalDetails();
        //
        //static IConfiguration configuration;
        //private readonly FlightBookingRecognizer _luisRecognizer = new FlightBookingRecognizer(configuration);
        //
        private FlightBooking luisResult;
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
            AddDialog(new DateResolverDialog());
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)/*, ChoicePromptValidatorAsync*/));
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
            //PersonalDetails = (PersonalDetails)stepContext.Options;

            // Skip questions if already collected the data
            if (PersonalDetails.Name != null && PersonalDetails.Age != null && PersonalDetails.Sex != null)
                finished = true;

            if (PersonalDetails.Name == null)
            {
                var NameStepMsgText = MainDialog.Response.AskName();
                var promptMessage = MessageFactory.Text(NameStepMsgText, NameStepMsgText, InputHints.ExpectingInput);
                var retryText = MainDialog.Response.RetryName();
                var retryMessage = MessageFactory.Text(retryText, retryText, InputHints.ExpectingInput);

                //
                //var temp = _luisRecognizer.IsConfigured;
                //luisResult = await MainDialog._luisRecognizer.RecognizeAsync<FlightBooking>(stepContext.Context, cancellationToken);
                //string name = (luisResult.Entities.personName != null ? char.ToUpper(luisResult.Entities.personName[0][0]) + luisResult.Entities.personName[0].Substring(1) : null);
                //
                //PromptValidator<int> nameValidator = 5; 
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage, RetryPrompt = retryMessage, Validations = Validator.Name }, cancellationToken);
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

            // Sand to DB
            MainDialog.WriteToDB(stepContext, cancellationToken);

            // Test for questionnairs
            //MainDialog.WriteQuestionnairesTempAsync(stepContext, cancellationToken);
            //MainDialog.WriteQuestionnairesTemp2Async(stepContext, cancellationToken);

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
            //PersonalDetails= (PersonalDetails)stepContext.Options;

            //PersonalDetails.Age = Regex.Match((string)stepContext.Result, @"\d+").Value;
            //if (PersonalDetails.Age == null)
            //{
            //    luisResult = await MainDialog.Get_luisRecognizer().RecognizeAsync<FlightBooking>(stepContext.Context, cancellationToken);
            //    PersonalDetails.Age = (luisResult.Entities.age != null ? luisResult.Entities.age[0].Number.ToString() : Regex.Match((string)stepContext.Result, @"\d+").Value);
            //}

            //personalDetails.Age = (string)stepContext.Result;

            //if(PersonalDetails.Age == null)
            //{
            //    string errorMessage = "Didnt get that, lets try again.";
            //    var promptMessage = MessageFactory.Text(errorMessage, errorMessage, InputHints.ExpectingInput);
            //    await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);


            //    return await stepContext.ReplaceDialogAsync(nameof(PersonalDetailsDialog.AgeStepAsync), PersonalDetails, cancellationToken);
            //}

            // Sand to DB
            MainDialog.WriteToDB(stepContext, cancellationToken);

            if (PersonalDetails.Sex == null)
            {
                var SexStepMsgText = MainDialog.Response.AskSex();
                var promptMessage = MessageFactory.Text(SexStepMsgText, SexStepMsgText, InputHints.ExpectingInput);
                var retryText = $"Please choose one option.\n\n{SexStepMsgText}";
                var retryPromptText = MessageFactory.Text(retryText, retryText, InputHints.ExpectingInput);
                //return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

                var sexChoice = new List<Choice>() { new Choice("Male"), new Choice("Female") };

                return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = promptMessage, Choices = sexChoice, RetryPrompt = retryPromptText, Style = ListStyle.HeroCard }, cancellationToken);
            }

            return await stepContext.NextAsync(PersonalDetails.Sex, cancellationToken);
        }

        private async Task<DialogTurnResult> SmokerStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (PersonalDetails.Sex == null)
                PersonalDetails.Sex = ((FoundChoice)stepContext.Result).Value;

            // Sand to DB
            MainDialog.WriteToDB(stepContext, cancellationToken);

            if (PersonalDetails.Smoker == null)
            {
                var messageText = $"Are you a smoker?";
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

            // Sand to DB
            MainDialog.WriteToDB(stepContext, cancellationToken);

            if (PersonalDetails.WaterConsumption == null)
            {
                var messageText = $"How many cups of water do you drink every day?";
                var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                var retryText = $"Can you repeat the number of cups plrease?";
                var retryPromptText = MessageFactory.Text(retryText, retryText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(NumberPrompt<int>), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }
            else
                return await stepContext.NextAsync(PersonalDetails, cancellationToken);
        }

        private async Task<DialogTurnResult> SleepStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (PersonalDetails.WaterConsumption == null)
                PersonalDetails.WaterConsumption = (int)stepContext.Result;

            // Sand to DB
            MainDialog.WriteToDB(stepContext, cancellationToken);

            if (PersonalDetails.Sleep == null)
            {
                var messageText = $"How many hours of sleep do you get every day?";
                var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                var retryText = $"Can you repeat the number of hours you sleep plrease?";
                var retryPromptText = MessageFactory.Text(retryText, retryText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(NumberPrompt<int>), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }
            else
                return await stepContext.NextAsync(PersonalDetails, cancellationToken);
        }

        private async Task<DialogTurnResult> PhysicalActivityStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (PersonalDetails.Sleep == null)
                PersonalDetails.Sleep = (int)stepContext.Result;

            // Sand to DB
            MainDialog.WriteToDB(stepContext, cancellationToken);

            if (PersonalDetails.PhysicalActivity == null)
            {
                var messageText = $"How many hours of physical excersise do you get every week?";
                var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                var retryText = $"Can you repeat the number of hours you excersise plrease?";
                var retryPromptText = MessageFactory.Text(retryText, retryText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(NumberPrompt<int>), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }
            else
                return await stepContext.NextAsync(PersonalDetails, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (PersonalDetails.PhysicalActivity == null)
                PersonalDetails.PhysicalActivity = (int)stepContext.Result;

            //PersonalDetails = (PersonalDetails)stepContext.Options;

            //if(Regex.IsMatch((string)stepContext.Result, "female", RegexOptions.IgnoreCase))
            //    PersonalDetails.Sex = "Female";
            //else if (Regex.IsMatch((string)stepContext.Result, "male", RegexOptions.IgnoreCase))
            //    PersonalDetails.Sex = "Male";
            //else
            //    PersonalDetails.Sex = null;


            //if (PersonalDetails.Sex == null)
            //    PersonalDetails.Sex = ((FoundChoice)stepContext.Result).Value;

            //    //PersonalDetails.Sex = stepContext.Result.ToString();
            //    PersonalDetails.Sex = char.ToUpper(stepContext.Context.Activity.Text[0]) + stepContext.Context.Activity.Text.Substring(1).ToLower();

            //var choice = (FoundChoice)stepContext.Result;
            //PersonalDetails.Sex = choice.Value;

            //var userProfile = (UserProfile)stepContext.Values[UserInfo];
            //userProfile.CompaniesToReview = stepContext.Result as List<string> ?? new List<string>();

            // Sand to DB
            MainDialog.WriteToDB(stepContext, cancellationToken);

            var messageText = $"Please confirm, this is your personal info:\n\nName: {PersonalDetails.Name}\n\nAge: {PersonalDetails.Age}\n\nSex: {PersonalDetails.Sex}\n\nSmoker: {PersonalDetails.Smoker}\n\nWater Consumption: {PersonalDetails.WaterConsumption} per day\n\nSleep: {PersonalDetails.Sleep} hours per day\n\nPhysical Activity: {PersonalDetails.PhysicalActivity} hours per week\n\nIs this correct?";
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
                //var personalDetails = (PersonalDetails)stepContext.Options;

                // Upload to database

                // Wipe the personalDetails for testing purposes
                //PersonalDetails = new PersonalDetails();


                // Sand to DB
                MainDialog.WriteToDB(stepContext, cancellationToken);
                //var changes = new Dictionary<string, object>() { { PersonalDetails.UserID, PersonalDetails } };
                //try
                //{
                //    MainDialog.CosmosDBQuery.WriteAsync(changes, cancellationToken);
                //    finished = false;
                //}
                //catch (Exception e)
                //{
                //    await stepContext.Context.SendActivityAsync($"Error while connecting to database.\n\n{e}");
                //}

                //return await stepContext.EndDialogAsync(PersonalDetails, cancellationToken);
                return await stepContext.BeginDialogAsync(nameof(QuestionnaireChoiceDialog), PersonalDetails, cancellationToken);
            }
            else
            {
                //PersonalDetails = new PersonalDetails();
                //PersonalDetails.Name = PersonalDetails.Sex = null;
                //PersonalDetails.Age = null;
                ClearDetails();

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

                    if (PersonalDetails.Name == null)
                        return await Task.FromResult(false);
                    else
                        return await Task.FromResult(true);

                case Validator.Age:


                    luisResult = await MainDialog.Get_luisRecognizer().RecognizeAsync<FlightBooking>(promptContext.Context, cancellationToken);
                    //PersonalDetails.Age = (luisResult.Entities.age != null ? luisResult.Entities.age[0].Number.ToString() : Regex.Match(promptContext.Context.Activity.Text, @"\d+").Value);
                    if (luisResult.Entities.age != null)
                        PersonalDetails.Age = (int?)luisResult.Entities.age[0].Number;
                    else if (Regex.Match(promptContext.Context.Activity.Text, @"\d+").Value != "")
                        //PersonalDetails.Age = Int32.Parse(Regex.Match(promptContext.Context.Activity.Text, @"\d+").Value);
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

        //public async Task ClusteringAsync()
        //{
        //    var mlContext = new MLContext();


        //    DatabaseLoader loader = mlContext.Data.CreateDatabaseLoader<PersonalDetails>();




        //    var cosmosDbResults = await MainDialog.CosmosDBQuery.ReadAsync(new string[] { PersonalDetailsDialog.PersonalDetails.UserID });
        //    //var cosmosDbResults = await ReadFromDb;
        //    if (cosmosDbResults.Values.FirstOrDefault() != null)
        //        PersonalDetailsDialog.PersonalDetails = (PersonalDetails)cosmosDbResults.Values.FirstOrDefault();

        //    DatabaseSource dbSource = new DatabaseSource(SqlClientFactory.Instance, connectionString, sqlCommand);

        //    IDataView data = loader.Load();

        //    // read the iris flower data from a text file
        //    var trainingData = mlContext.Data.CreateDatabaseLoader();
        //}

        //public void Clustering()
        //{
        //    //Step 1. Create a ML Context
        //    var ctx = new MLContext();

        //    //Step 2. Read in the input data for model training
        //    IDataView dataReader = ctx.Data
        //        .LoadFromTextFile<MyInput>(dataPath, hasHeader: true);

        //    //Step 3. Build your estimator
        //    IEstimator<ITransformer> est = ctx.Transforms.Text
        //        .FeaturizeText("Features", nameof(SentimentIssue.Text))
        //        .Append(ctx.BinaryClassification.Trainers
        //            .LbfgsLogisticRegression("Label", "Features"));

        //    //Step 4. Train your Model
        //    ITransformer trainedModel = est.Fit(dataReader);

        //    //Step 5. Make predictions using your model
        //    var predictionEngine = ctx.Model
        //        .CreatePredictionEngine<MyInput, MyOutput>(trainedModel);

        //    var sampleStatement = new MyInput { Text = "This is a horrible movie" };

        //    var prediction = predictionEngine.Predict(sampleStatement);
        //}

        //private async Task<bool> ChoicePromptValidatorAsync(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        //{
        //    foreach (Choice obj in promptContext.Options.Choices)
        //    {
        //        if (obj.Value.ToLower() == promptContext.Context.Activity.Text.ToLower())
        //            return await Task.FromResult(true);
        //    }


        //    luisResult = await MainDialog.Get_luisRecognizer().RecognizeAsync<FlightBooking>(promptContext.Context, cancellationToken);
        //    //if (luisResult.TopIntent().intent == FlightBooking.Intent.Cancel)
        //        //    await stepContext1.EndDialogAsync(PersonalDetails, cancellationToken);


        //        //await stepContext.EndDialogAsync(PersonalDetailsDialog.PersonalDetails, cancellationToken);

        //        return await Task.FromResult(false);
        //}

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
