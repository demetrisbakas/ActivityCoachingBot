// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreBot;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Newtonsoft.Json;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        // CosmosDB Initialization
        private const string cosmosServiceEndpoint = "https://activitycoachbot-cosmosdb-sql.documents.azure.com:443/";
        private const string cosmosDBKey = "62Flv5AdBkoKQed8WixdZeZEWp6yhn1rptznPnYQb1Yt5jI8UgYnJ0pOQuJTTOVLHr9le5sMzaWUEAKmbXTF1w==";
        private const string cosmosDBDatabaseName = "bot-cosmos-sql-db";
        private const string cosmosDBConteinerId = "bot-storage";
        private const string cosmosDBConteinerIdTips = "tips";
        private const string cosmosDBConteinerIdQuestionnaires = "questionnaires";

        private static ConnectionRecognizer _luisRecognizer;
        public static Task<IDictionary<string, object>> ReadFromDb;
        public static Task<List<ClusterPersonalDetailsWithoutNull>> ClusteringData;
        public static Task<List<KeyValuePair<string, List<QuestionTopFive>>>> Questionnaires;
        private static ConcurrentDictionary<string, ConversationReference> _conversationReferences;

        // Implemented a getter, so no other class can change the value of the recognizer exept this constructor
        public static ConnectionRecognizer Get_luisRecognizer()
        {
            return _luisRecognizer;
        }

        //private readonly FlightBookingRecognizer _luisRecognizer;
        protected readonly ILogger Logger;
        public static ResponseText Response { get; } = new ResponseText();

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(ConnectionRecognizer luisRecognizer, PersonalDetailsDialog personalDetailsDialog, TopFiveDialog topFiveDialog, QuestionnaireChoiceDialog questionnaireChoiceDialog, ReenterDetailsDialog reenterDetailsDialog, AuthenticationDialog authenticationDialog, ILogger<MainDialog> logger, ConcurrentDictionary<string, ConversationReference> conversationReferences)
            : base(nameof(MainDialog))
        {
            _luisRecognizer = luisRecognizer;
            Logger = logger;
            _conversationReferences = conversationReferences;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(personalDetailsDialog);
            AddDialog(authenticationDialog);
            AddDialog(topFiveDialog);
            AddDialog(questionnaireChoiceDialog);
            AddDialog(reenterDetailsDialog);
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

            // Use the text provided in FinalStepAsync or the default if it is the first time.
            var messageText = stepContext.Options?.ToString() ?? Response.WelcomeMessage()/*"What can I help you with today?\nSay something like \"Book a flight from Paris to Berlin on March 22, 2020\"\n\nGreet the bot to enter the personal details dialog."*/;
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Fetch data from DB
            try
            {
                //var cosmosDbResults = await CosmosDBQuery.ReadAsync(new string[] { PersonalDetailsDialog.PersonalDetails.UserID }, cancellationToken);
                var cosmosDbResults = await ReadFromDb;
                if (cosmosDbResults.Values.FirstOrDefault() != null)
                    PersonalDetailsDialog.PersonalDetails = (PersonalDetails)cosmosDbResults.Values.FirstOrDefault();
                //else
                //    // Wiping user data since new user is detected
                //    PersonalDetailsDialog.PersonalDetails = new PersonalDetails();
            }
            catch (Exception e)
            {
                await stepContext.Context.SendActivityAsync($"Error while connecting to database.\n\n{e}");
            }

            if (!_luisRecognizer.IsConfigured)
            {
                // LUIS is not configured, we just run the PersonalDetailsDialog path with an empty BookingDetailsInstance.
                return await stepContext.BeginDialogAsync(nameof(PersonalDetailsDialog), new BookingDetails(), cancellationToken);
            }

            // Call LUIS and gather any potential booking details. (Note the TurnContext has the response to the prompt.)
            var luisResult = await _luisRecognizer.RecognizeAsync<LuisModel>(stepContext.Context, cancellationToken);
            switch (luisResult.TopIntent().intent)
            {
                //case FlightBooking.Intent.BookFlight:
                //    await ShowWarningForUnsupportedCities(stepContext.Context, luisResult, cancellationToken);

                //    // Initialize BookingDetails with any entities we may have found in the response.
                //    var bookingDetails = new BookingDetails()
                //    {
                //        // Get destination and origin from the composite entities arrays.
                //        Destination = luisResult.ToEntities.Airport,
                //        Origin = luisResult.FromEntities.Airport,
                //        TravelDate = luisResult.TravelDate,
                //    };

                //    // Run the BookingDialog giving it whatever details we have from the LUIS call, it will fill out the remainder.
                //    return await stepContext.BeginDialogAsync(nameof(BookingDialog), bookingDetails, cancellationToken);

                //case FlightBooking.Intent.GetWeather:
                //    // We haven't implemented the GetWeatherDialog so we just display a TODO message.
                //    var getWeatherMessageText = "TODO: get weather flow here";
                //    var getWeatherMessage = MessageFactory.Text(getWeatherMessageText, getWeatherMessageText, InputHints.IgnoringInput);
                //    await stepContext.Context.SendActivityAsync(getWeatherMessage, cancellationToken);
                //    break;

                case LuisModel.Intent.AddQuestionnairesOrTips:
                    return await stepContext.BeginDialogAsync(nameof(AuthenticationDialog), PersonalDetailsDialog.PersonalDetails, cancellationToken);

                case LuisModel.Intent.AnswerQuestionnaires:
                    return await stepContext.BeginDialogAsync(nameof(PersonalDetailsDialog), PersonalDetailsDialog.PersonalDetails, cancellationToken);

                case LuisModel.Intent.Greet:
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

                    // Run the PersonalDetailsDialog giving it whatever details we have from the LUIS call, it will fill out the remainder.
                    return await stepContext.BeginDialogAsync(nameof(PersonalDetailsDialog), PersonalDetailsDialog.PersonalDetails, cancellationToken);
                //return await stepContext.BeginDialogAsync(nameof(TopFiveDialog), PersonalDetailsDialog.PersonalDetails.QuestionnaireAnswers, cancellationToken);

                case LuisModel.Intent.EnterPersonalDetails:
                    if (CheckDetails())
                        return await stepContext.BeginDialogAsync(nameof(ReenterDetailsDialog), PersonalDetailsDialog.PersonalDetails, cancellationToken);
                    else
                        return await stepContext.BeginDialogAsync(nameof(PersonalDetailsDialog), PersonalDetailsDialog.PersonalDetails, cancellationToken);

                default:
                    // Catch all for unhandled intents
                    // Try to find an answer on the knowledge base
                    var knowledgeBaseResult = await _luisRecognizer.SampleQnA.GetAnswersAsync(stepContext.Context);

                    if (knowledgeBaseResult?.FirstOrDefault() != null)
                        return await stepContext.ReplaceDialogAsync(InitialDialogId, knowledgeBaseResult[0].Answer, cancellationToken);
                    else
                    {
                        // If it's not on the knowledge base, return error message
                        var didntUnderstandMessageText = $"Sorry, I didn't get that. Please try asking in a different way (intent was {luisResult.TopIntent().intent})";
                        var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                        await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                    }
                    break;
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        // Shows a warning if the requested From or To cities are recognized as entities but they are not in the Airport entity list.
        // In some cases LUIS will recognize the From and To composite entities as a valid cities but the From and To Airport values
        // will be empty if those entity values can't be mapped to a canonical item in the Airport.
        //private static async Task ShowWarningForUnsupportedCities(ITurnContext context, FlightBooking luisResult, CancellationToken cancellationToken)
        //{
        //    var unsupportedCities = new List<string>();

        //    var fromEntities = luisResult.FromEntities;
        //    if (!string.IsNullOrEmpty(fromEntities.From) && string.IsNullOrEmpty(fromEntities.Airport))
        //    {
        //        unsupportedCities.Add(fromEntities.From);
        //    }

        //    var toEntities = luisResult.ToEntities;
        //    if (!string.IsNullOrEmpty(toEntities.To) && string.IsNullOrEmpty(toEntities.Airport))
        //    {
        //        unsupportedCities.Add(toEntities.To);
        //    }

        //    if (unsupportedCities.Any())
        //    {
        //        var messageText = $"Sorry but the following airports are not supported: {string.Join(',', unsupportedCities)}";
        //        var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
        //        await context.SendActivityAsync(message, cancellationToken);
        //    }
        //}

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // If the child dialog ("BookingDialog") was cancelled, the user failed to confirm or if the intent wasn't BookFlight
            // the Result here will be null.
            //if (stepContext.Result is BookingDetails result)
            //{
            //    // Now we have all the booking details call the booking service.

            //    // If the call to the booking service was successful tell the user.

            //    var timeProperty = new TimexProperty(result.TravelDate);
            //    var travelDateMsg = timeProperty.ToNaturalLanguage(DateTime.Now);
            //    var messageText = $"I have you booked to {result.Destination} from {result.Origin} on {travelDateMsg}";
            //    var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
            //    await stepContext.Context.SendActivityAsync(message, cancellationToken);
            //}

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

        public static async void WriteToDB(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Send to DB
            var changes = new Dictionary<string, object>() { { PersonalDetailsDialog.PersonalDetails.UserID, PersonalDetailsDialog.PersonalDetails } };
            try
            {
                #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                CosmosDBQuery.WriteAsync(changes, cancellationToken);
                #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            catch (Exception e)
            {
                await stepContext.Context.SendActivityAsync($"Error while connecting to database.\n\n{e}");
            }
        }

        //public static async void WriteQuestionnairesTempAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        //{
        //    CosmosDbPartitionedStorage CosmosDBQueryQuestionnaires = new CosmosDbPartitionedStorage(new CosmosDbPartitionedStorageOptions
        //    {
        //        AuthKey = cosmosDBKey,
        //        ContainerId = "test",
        //        CosmosDbEndpoint = cosmosServiceEndpoint,
        //        DatabaseId = cosmosDBDatabaseName,
        //    });

        //    // Sand to DB
        //    var changes = new Dictionary<string, object>() { { Response.Questionnaires.FirstOrDefault().Key, Response.Questionnaires.FirstOrDefault() } };
        //    try
        //    {
        //        await CosmosDBQueryQuestionnaires.WriteAsync(changes, cancellationToken);
        //    }
        //    catch (Exception e)
        //    {
        //        await stepContext.Context.SendActivityAsync($"Error while connecting to database.\n\n{e}");
        //    }
        //}

        public static async Task<uint> ClusteringAsync()
        {
            //var dataLocation = "./Seed_Data.csv";


            //IQueryable<PersonalDetails> linqQuery = table.CreateQuery<PersonalDetails>()
            //.Select(x => new PersonalDetails());


            var dataList = await ClusteringData;



            var context = new MLContext();

            //var textLoader = context.Data.CreateTextLoader(new[]
            //{
            //    new TextLoader.Column("Extraversion", DataKind.Single, 0),
            //    new TextLoader.Column("Agreeableness", DataKind.Single, 1),
            //    new TextLoader.Column("Conscientiousness", DataKind.Single, 2),
            //    new TextLoader.Column("Neuroticism", DataKind.Single, 3),
            //    new TextLoader.Column("Openness", DataKind.Single, 4),
            //},
            //hasHeader: true,
            //separatorChar: ',');

            IDataView data = context.Data.LoadFromEnumerable(dataList);


            //IDataView data = textLoader.Load(dataLocation);

            var trainTestData = context.Data.TrainTestSplit(data, testFraction: 0.2);

            var pipeline = context.Transforms.Concatenate("Features", "Extraversion", "Agreeableness", "Conscientiousness", "Neuroticism", "Openness").Append(context.Clustering.Trainers.KMeans(featureColumnName: "Features", numberOfClusters: 5));

            //var preview = trainTestData.TrainSet.Preview();

            var model = pipeline.Fit(trainTestData.TrainSet);

            //var predictions = model.Transform(trainTestData.TestSet);

            //var metrics = context.Clustering.Evaluate(predictions, scoreColumnName: "Score", featureColumnName: "Features");

            //Console.WriteLine($"Average minimum score: {metrics.AverageDistance}");

            //var predictionFunc = model.CreatePredictionEngine<PersonalDetails, SeedPrediction>(context);

            var predictionFunc = context.Model.CreatePredictionEngine<ClusterPersonalDetailsWithoutNull, SeedPrediction>(model);

            var prediction = predictionFunc.Predict(new ClusterPersonalDetailsWithoutNull
            {
                Extraversion = PersonalDetailsDialog.PersonalDetails.Extraversion != null ? (float)PersonalDetailsDialog.PersonalDetails.Extraversion : 0,
                Agreeableness = PersonalDetailsDialog.PersonalDetails.Agreeableness != null ? (float)PersonalDetailsDialog.PersonalDetails.Agreeableness : 0,
                Conscientiousness = PersonalDetailsDialog.PersonalDetails.Conscientiousness != null ? (float)PersonalDetailsDialog.PersonalDetails.Conscientiousness : 0,
                Neuroticism = PersonalDetailsDialog.PersonalDetails.Neuroticism != null ? (float)PersonalDetailsDialog.PersonalDetails.Neuroticism : 0,
                Openness = PersonalDetailsDialog.PersonalDetails.Openness != null ? (float)PersonalDetailsDialog.PersonalDetails.Openness : 0
            });

            //Console.WriteLine($"Prediction - {prediction.SelectedClusterId}");
            //Console.ReadLine();

            return prediction.SelectedClusterId;
        }


        public static async Task<List<ClusterPersonalDetailsWithoutNull>> QueryClusterDetailsAsync()
        {
            var sqlQueryText = "SELECT c.document.Extraversion, c.document.Agreeableness, c.document.Conscientiousness, c.document.Neuroticism, c.document.Openness FROM c";

            //Console.WriteLine("Running query: {0}\n", sqlQueryText);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

            CosmosClient cosmosClient = new CosmosClient(cosmosServiceEndpoint, cosmosDBKey);
            Azure.Cosmos.Database database;
            database = await cosmosClient.CreateDatabaseIfNotExistsAsync(cosmosDBDatabaseName);
            //Console.WriteLine("Created Database: {0}\n", this.database.Id);
            Container container;
            container = await database.CreateContainerIfNotExistsAsync(cosmosDBConteinerId, "/id");
            //Console.WriteLine("Created Container: {0}\n", this.container.Id);

            FeedIterator<ClusterPersonalDetails> queryResultSetIterator = container.GetItemQueryIterator<ClusterPersonalDetails>(queryDefinition);

            List<ClusterPersonalDetails> detailsList = new List<ClusterPersonalDetails>();

            while (queryResultSetIterator.HasMoreResults)
            {
                Azure.Cosmos.FeedResponse<ClusterPersonalDetails> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (ClusterPersonalDetails details in currentResultSet)
                {
                    detailsList.Add(details);
                    //Console.WriteLine("\tRead {0}\n", family);
                }
            }

            return RemoveNullValues(detailsList);
        }

        public static async Task<List<Tip>> QueryTipsAsync()
        {
            var sqlQueryText = "SELECT * FROM c";

                //Console.WriteLine("Running query: {0}\n", sqlQueryText);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

            CosmosClient cosmosClient = new CosmosClient(cosmosServiceEndpoint, cosmosDBKey);
            Azure.Cosmos.Database database;
            database = await cosmosClient.CreateDatabaseIfNotExistsAsync(cosmosDBDatabaseName);
            //Console.WriteLine("Created Database: {0}\n", this.database.Id);
            Container container;
            container = await database.CreateContainerIfNotExistsAsync(cosmosDBConteinerIdTips, "/id");
            //Console.WriteLine("Created Container: {0}\n", this.container.Id);

            FeedIterator<Tip> queryResultSetIterator = container.GetItemQueryIterator<Tip>(queryDefinition);

            List<Tip> tipList = new List<Tip>();

            while (queryResultSetIterator.HasMoreResults)
            {
                Azure.Cosmos.FeedResponse<Tip> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (Tip details in currentResultSet)
                {
                    tipList.Add(details);
                    //Console.WriteLine("\tRead {0}\n", family);
                }
            }

            return tipList;
        }

        public static async Task<List<KeyValuePair<string, List<QuestionTopFive>>>> QueryQuestionnairesAsync()
        {
            // test
            //using (StreamReader r = new StreamReader(@"C:\SourceTree Repos\ActivityCoachingBot\test.json"))
            //{
            //    string json = r.ReadToEnd();
            //    //var items = JsonConvert.DeserializeObject<KeyValuePair<string, List<QuestionTopFive>>>(json);
            //    var items = JsonConvert.DeserializeObject<KeyValuePair<string, List<QuestionTopFive>>>(json);
            //}
            //

            var sqlQueryText = "SELECT c.document.Key, c.document[\"Value\"] FROM c";

            //Console.WriteLine("Running query: {0}\n", sqlQueryText);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

            CosmosClient cosmosClient = new CosmosClient(cosmosServiceEndpoint, cosmosDBKey);
            Azure.Cosmos.Database database;
            database = await cosmosClient.CreateDatabaseIfNotExistsAsync(cosmosDBDatabaseName);
            //Console.WriteLine("Created Database: {0}\n", this.database.Id);
            Container container;
            container = await database.CreateContainerIfNotExistsAsync(cosmosDBConteinerIdQuestionnaires, "/id");
            //Console.WriteLine("Created Container: {0}\n", this.container.Id);

            FeedIterator<KeyValuePair<string, List<QuestionTopFive>>> queryResultSetIterator = container.GetItemQueryIterator<KeyValuePair<string, List<QuestionTopFive>>>(queryDefinition);

            var questionnaireList = new List<KeyValuePair<string, List<QuestionTopFive>>>();

            while (queryResultSetIterator.HasMoreResults)
            {
                Azure.Cosmos.FeedResponse<KeyValuePair<string, List<QuestionTopFive>>> currentResultSet = await queryResultSetIterator.ReadNextAsync();

                // Test
                //using (StreamReader r = new StreamReader(@"C:\SourceTree Repos\ActivityCoachingBot\test2.json"))
                //{
                //    string json = r.ReadToEnd();
                //    //var items = JsonConvert.DeserializeObject<KeyValuePair<string, List<QuestionTopFive>>>(json);
                //    var items = JsonConvert.DeserializeObject<List<string>>(json);
                //}

                foreach (KeyValuePair<string, List<QuestionTopFive>> questionnaire in currentResultSet)
                {
                    questionnaireList.Add(questionnaire);
                    //Console.WriteLine("\tRead {0}\n", family);
                }
            }

            return ReplaceMissingQuestionValues(questionnaireList);
        }


        private static List<ClusterPersonalDetailsWithoutNull> RemoveNullValues(List<ClusterPersonalDetails> detailsList)
        {
            var outputList = new List<ClusterPersonalDetailsWithoutNull>();

            foreach (ClusterPersonalDetails details in detailsList)
            {
                if (details.Extraversion == null)
                    details.Extraversion = 0;
                if (details.Agreeableness == null)
                    details.Agreeableness = 0;
                if (details.Conscientiousness == null)
                    details.Conscientiousness = 0;
                if (details.Neuroticism == null)
                    details.Neuroticism = 0;
                if (details.Openness == null)
                    details.Openness = 0;

                outputList.Add(new ClusterPersonalDetailsWithoutNull() { Extraversion = (float)details.Extraversion, Agreeableness = (float)details.Agreeableness, Conscientiousness = (float)details.Conscientiousness, Neuroticism = (float)details.Neuroticism, Openness = (float)details.Openness });
            }

            return outputList;
        }


        private static List<KeyValuePair<string, List<QuestionTopFive>>> ReplaceMissingQuestionValues(List<KeyValuePair<string, List<QuestionTopFive>>> questionnaireList)
        {
            foreach(var questionnaire in questionnaireList)
            {
                foreach(var question in questionnaire.Value)
                {
                    if (question.Answers.Count() == 0)
                        question.Answers = QuestionTopFive.DefaultAnswers();
                }
            }

            return questionnaireList;
        }

        // Checks if personal details are already completed by the user
        private bool CheckDetails() 
        {
            if (PersonalDetailsDialog.PersonalDetails.Name != null && PersonalDetailsDialog.PersonalDetails.Age != null && PersonalDetailsDialog.PersonalDetails.Sex != null && PersonalDetailsDialog.PersonalDetails.Smoker != null && PersonalDetailsDialog.PersonalDetails.WaterConsumption != null && PersonalDetailsDialog.PersonalDetails.Sleep != null && PersonalDetailsDialog.PersonalDetails.PhysicalActivity != null)
                return true;
            else
                return false;
        }


        //
        //public static void TestSQL()
        //{
        //    var querySpec = new SqlQuerySpec
        //    {
        //        QueryText = "select * from c",
        //        Parameters = new SqlParameterCollection
        //        {
        //            new SqlParameter
        //            {
        //                Name = "@id",
        //                //Value = userId
        //            }
        //        }
        //    };

        //    var documentClient = new DocumentClient(new Uri(cosmosServiceEndpoint), cosmosDBKey);
        //    var database = documentClient.CreateDatabaseQuery().FirstOrDefault(d => d.Id == cosmosDBDatabaseName);
        //    var collection = documentClient.CreateDocumentCollectionQuery(new Uri(cosmosServiceEndpoint), "select * from c").FirstOrDefault();
        //    var queryResult = documentClient.CreateDatabaseQuery(collection.DocumentsLink);












        //    FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

        //    IQueryable<dynamic> familyQueryInSql = client.CreateDocumentQuery<dynamic>(UriFactory.CreateDocumentCollectionUri(cosmosDBDatabaseName, cosmosDBConteinerId), "SELECT * FROM c", queryOptions);
        //}
        //





















        public static void AddConversationReference(Activity activity)
        {
            var conversationReference = activity.GetConversationReference();
            _conversationReferences.AddOrUpdate(conversationReference.User.Id, conversationReference, (key, newValue) => conversationReference);
        }

        //protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        //{
        //    AddConversationReference(turnContext.Activity as Activity);

        //    // Echo back what the user said
        //    await turnContext.SendActivityAsync(MessageFactory.Text($"You sent '{turnContext.Activity.Text}'"), cancellationToken);
        //}
    }
}
