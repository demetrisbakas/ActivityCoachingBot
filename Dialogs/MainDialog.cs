// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        // CosmosDB Initialization
        private const string cosmosServiceEndpoint = "https://activitycoachbot-cosmosdb-sql.documents.azure.com:443/";
        private const string cosmosDBKey = "62Flv5AdBkoKQed8WixdZeZEWp6yhn1rptznPnYQb1Yt5jI8UgYnJ0pOQuJTTOVLHr9le5sMzaWUEAKmbXTF1w==";
        private const string cosmosDBDatabaseName = "bot-cosmos-sql-db";
        private const string cosmosDBConteinerId = "bot-storage";
        private static FlightBookingRecognizer _luisRecognizer;

        // Implemented a getter, so no other class can change the value of the recognizer exept this constructor
        public static FlightBookingRecognizer Get_luisRecognizer()
        {
            return _luisRecognizer;
        }

        //private readonly FlightBookingRecognizer _luisRecognizer;
        protected readonly ILogger Logger;
        public static ResponseText Response { get; } = new ResponseText();

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(FlightBookingRecognizer luisRecognizer, BookingDialog bookingDialog, PersonalDetailsDialog personalDetailsDialog, TopFiveDialog topFiveDialog, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _luisRecognizer = luisRecognizer;
            Logger = logger;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(bookingDialog);
            AddDialog(personalDetailsDialog);
            AddDialog(topFiveDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);

                return await stepContext.NextAsync(null, cancellationToken);
            }

            // Fetch data from DB
            try
            {
                var cosmosDbResults = await CosmosDBQuery.ReadAsync(new string[] { PersonalDetailsDialog.PersonalDetails.UserID }, cancellationToken);
                if (cosmosDbResults.Values.FirstOrDefault() != null)
                    PersonalDetailsDialog.PersonalDetails = (PersonalDetails)cosmosDbResults.Values.FirstOrDefault();
                else
                    // Wiping user data since new user is detected
                    PersonalDetailsDialog.PersonalDetails = new PersonalDetails();
            }
            catch (Exception e)
            {
                await stepContext.Context.SendActivityAsync($"Error while connecting to database.\n\n{e}");
            }

            // Use the text provided in FinalStepAsync or the default if it is the first time.
            var messageText = stepContext.Options?.ToString() ?? "What can I help you with today?\nSay something like \"Book a flight from Paris to Berlin on March 22, 2020\"\n\nGreet the bot to enter the personal details dialog.";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                // LUIS is not configured, we just run the BookingDialog path with an empty BookingDetailsInstance.
                return await stepContext.BeginDialogAsync(nameof(BookingDialog), new BookingDetails(), cancellationToken);
            }

            // Call LUIS and gather any potential booking details. (Note the TurnContext has the response to the prompt.)
            var luisResult = await _luisRecognizer.RecognizeAsync<FlightBooking>(stepContext.Context, cancellationToken);
            switch (luisResult.TopIntent().intent)
            {
                case FlightBooking.Intent.BookFlight:
                    await ShowWarningForUnsupportedCities(stepContext.Context, luisResult, cancellationToken);

                    // Initialize BookingDetails with any entities we may have found in the response.
                    var bookingDetails = new BookingDetails()
                    {
                        // Get destination and origin from the composite entities arrays.
                        Destination = luisResult.ToEntities.Airport,
                        Origin = luisResult.FromEntities.Airport,
                        TravelDate = luisResult.TravelDate,
                    };

                    // Run the BookingDialog giving it whatever details we have from the LUIS call, it will fill out the remainder.
                    return await stepContext.BeginDialogAsync(nameof(BookingDialog), bookingDetails, cancellationToken);

                case FlightBooking.Intent.GetWeather:
                    // We haven't implemented the GetWeatherDialog so we just display a TODO message.
                    var getWeatherMessageText = "TODO: get weather flow here";
                    var getWeatherMessage = MessageFactory.Text(getWeatherMessageText, getWeatherMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(getWeatherMessage, cancellationToken);
                    break;

                case FlightBooking.Intent.Greet:
                    // Deatails of the user
                    // IMPORTANT Wipes the data of the user
                    //PersonalDetailsDialog.PersonalDetails = new PersonalDetails()
                    //{
                    //    // Get name and age from the composite entities arrays.
                    //    Name = luisResult.Entities.personName != null ? char.ToUpper(luisResult.Entities.personName[0][0]) + luisResult.Entities.personName[0].Substring(1) : null,
                    //    Age = (int?)luisResult.Entities.age?[0].Number,
                    //};

                    if (PersonalDetailsDialog.PersonalDetails.Name == null)
                        PersonalDetailsDialog.PersonalDetails.Name = luisResult.Entities.personName != null ? char.ToUpper(luisResult.Entities.personName[0][0]) + luisResult.Entities.personName[0].Substring(1) : null;
                    if (PersonalDetailsDialog.PersonalDetails.Age == null)
                        PersonalDetailsDialog.PersonalDetails.Age = (int?)luisResult.Entities.age?[0].Number;

                    // Greeting message
                    var greetText = (PersonalDetailsDialog.PersonalDetails.Name == null ? Response.Greet() : Response.Greet(PersonalDetailsDialog.PersonalDetails.Name));
                    var greetTextMessage = MessageFactory.Text(greetText, greetText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(greetTextMessage, cancellationToken);







                    //await ShowWarningForUnsupportedCities(stepContext.Context, luisResult, cancellationToken);

                    // Initialize BookingDetails with any entities we may have found in the response.
                    //var personalDetails = new PersonalDetails()
                    //{
                    //    // Get destination and origin from the composite entities arrays.
                    //    //Name = luisResult.Entities.datetime.ToString(),
                    //    Name = luisResult.ToEntities.Airport,
                    //    Age = luisResult.FromEntities.Airport,
                    //    Sex = luisResult.TravelDate,
                    //};

                    // Run the BookingDialog giving it whatever details we have from the LUIS call, it will fill out the remainder.
                    return await stepContext.BeginDialogAsync(nameof(PersonalDetailsDialog), PersonalDetailsDialog.PersonalDetails, cancellationToken);
                    //return await stepContext.BeginDialogAsync(nameof(TopFiveDialog), PersonalDetailsDialog.PersonalDetails.QuestionnaireAnswers, cancellationToken);



                default:
                    // Catch all for unhandled intents
                    var didntUnderstandMessageText = $"Sorry, I didn't get that. Please try asking in a different way (intent was {luisResult.TopIntent().intent})";
                    var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                    break;
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        // Shows a warning if the requested From or To cities are recognized as entities but they are not in the Airport entity list.
        // In some cases LUIS will recognize the From and To composite entities as a valid cities but the From and To Airport values
        // will be empty if those entity values can't be mapped to a canonical item in the Airport.
        private static async Task ShowWarningForUnsupportedCities(ITurnContext context, FlightBooking luisResult, CancellationToken cancellationToken)
        {
            var unsupportedCities = new List<string>();

            var fromEntities = luisResult.FromEntities;
            if (!string.IsNullOrEmpty(fromEntities.From) && string.IsNullOrEmpty(fromEntities.Airport))
            {
                unsupportedCities.Add(fromEntities.From);
            }

            var toEntities = luisResult.ToEntities;
            if (!string.IsNullOrEmpty(toEntities.To) && string.IsNullOrEmpty(toEntities.Airport))
            {
                unsupportedCities.Add(toEntities.To);
            }

            if (unsupportedCities.Any())
            {
                var messageText = $"Sorry but the following airports are not supported: {string.Join(',', unsupportedCities)}";
                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                await context.SendActivityAsync(message, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // If the child dialog ("BookingDialog") was cancelled, the user failed to confirm or if the intent wasn't BookFlight
            // the Result here will be null.
            if (stepContext.Result is BookingDetails result)
            {
                // Now we have all the booking details call the booking service.

                // If the call to the booking service was successful tell the user.

                var timeProperty = new TimexProperty(result.TravelDate);
                var travelDateMsg = timeProperty.ToNaturalLanguage(DateTime.Now);
                var messageText = $"I have you booked to {result.Destination} from {result.Origin} on {travelDateMsg}";
                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(message, cancellationToken);
            }

            // Restart the main dialog with a different message the second time around
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }

        // Create Cosmos DB Storage.  
        public static readonly CosmosDbPartitionedStorage CosmosDBQuery = new CosmosDbPartitionedStorage(new CosmosDbPartitionedStorageOptions
        {
            AuthKey = cosmosDBKey,
            ContainerId = cosmosDBConteinerId,
            CosmosDbEndpoint = cosmosServiceEndpoint,
            DatabaseId = cosmosDBDatabaseName,
        });
    }
}
