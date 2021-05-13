// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CoreBot;
using Microsoft.AspNetCore.Mvc;
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
        //private static ConcurrentDictionary<string, ConversationReference> _conversationReferences;

        // Implemented a getter, so no other class can change the value of the recognizer exept this constructor
        public static ConnectionRecognizer Get_luisRecognizer()
        {
            return _luisRecognizer;
        }

        //private readonly FlightBookingRecognizer _luisRecognizer;
        protected readonly ILogger Logger;
        public static ResponseText Response { get; } = new ResponseText();

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(ConnectionRecognizer luisRecognizer, PersonalDetailsDialog personalDetailsDialog, TopFiveDialog topFiveDialog, QuestionnaireChoiceDialog questionnaireChoiceDialog, ReenterDetailsDialog reenterDetailsDialog, AuthenticationDialog authenticationDialog, UploadTipsOrQuestionnairesDialog uploadTipsOrQuestionnairesDialog, NumberOfTipsDialog numberOfTipsDialog, UploadTipsDialog uploadTipsDialog, NameOfQuestionnaireDialog nameOfQuestionnaireDialog, UploadQuestionnairesDialog uploadQuestionnairesDialog, ILogger<MainDialog> logger/*, ConcurrentDictionary<string, ConversationReference> conversationReferences*/)
            : base(nameof(MainDialog))
        {
            _luisRecognizer = luisRecognizer;
            Logger = logger;
            //_conversationReferences = conversationReferences;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(personalDetailsDialog);
            AddDialog(authenticationDialog);
            AddDialog(uploadTipsOrQuestionnairesDialog);
            AddDialog(topFiveDialog);
            AddDialog(questionnaireChoiceDialog);
            AddDialog(reenterDetailsDialog);
            AddDialog(numberOfTipsDialog);
            AddDialog(uploadTipsDialog);
            AddDialog(nameOfQuestionnaireDialog);
            AddDialog(uploadQuestionnairesDialog);
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
                case LuisModel.Intent.AddQuestionnairesOrTips:
                    return await stepContext.BeginDialogAsync(nameof(AuthenticationDialog), PersonalDetailsDialog.PersonalDetails, cancellationToken);

                case LuisModel.Intent.AnswerQuestionnaires:
                    return await stepContext.BeginDialogAsync(nameof(PersonalDetailsDialog), PersonalDetailsDialog.PersonalDetails, cancellationToken);

                case LuisModel.Intent.Greet:
                    if (PersonalDetailsDialog.PersonalDetails.Name == null)
                        PersonalDetailsDialog.PersonalDetails.Name = luisResult.Entities.personName != null ? char.ToUpper(luisResult.Entities.personName[0][0]) + luisResult.Entities.personName[0].Substring(1) : null;
                    if (PersonalDetailsDialog.PersonalDetails.Age == null)
                        PersonalDetailsDialog.PersonalDetails.Age = (int?)luisResult.Entities.age?[0].Number;

                    // Greeting message
                    var greetText = (PersonalDetailsDialog.PersonalDetails.Name == null ? Response.Greet() : Response.Greet(PersonalDetailsDialog.PersonalDetails.Name));
                    var greetTextMessage = MessageFactory.Text(greetText, greetText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(greetTextMessage, cancellationToken);

                    // Run the PersonalDetailsDialog giving it whatever details we have from the LUIS call, it will fill out the remainder.
                    return await stepContext.BeginDialogAsync(nameof(PersonalDetailsDialog), PersonalDetailsDialog.PersonalDetails, cancellationToken);

                case LuisModel.Intent.EnterPersonalDetails:
                    if (CheckDetails())
                        return await stepContext.BeginDialogAsync(nameof(ReenterDetailsDialog), PersonalDetailsDialog.PersonalDetails, cancellationToken);
                    else
                        return await stepContext.BeginDialogAsync(nameof(PersonalDetailsDialog), PersonalDetailsDialog.PersonalDetails, cancellationToken);

                default:
                    // Catch all for unhandled intents
                    // Try to find an answer on the knowledge base
                    var knowledgeBaseResult = await _luisRecognizer.SampleQnA.GetAnswersAsync(stepContext.Context);

                    if (Regex.IsMatch(stepContext.Context.Activity.Text, "help", RegexOptions.IgnoreCase))
                    {
                        // If intent is help, return help message
                        var helpMessageText = Response.HelpMessage();
                        return await stepContext.ReplaceDialogAsync(InitialDialogId, helpMessageText, cancellationToken);
                    }
                    else if (knowledgeBaseResult?.FirstOrDefault() != null)
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

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
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

        // Create Cosmos DB Storage.  
        private static readonly CosmosDbPartitionedStorage CosmosDBTipQuery = new CosmosDbPartitionedStorage(new CosmosDbPartitionedStorageOptions
        {
            AuthKey = cosmosDBKey,
            ContainerId = cosmosDBConteinerIdTips,
            CosmosDbEndpoint = cosmosServiceEndpoint,
            DatabaseId = cosmosDBDatabaseName,
        });

        // Create Cosmos DB Storage.  
        private static readonly CosmosDbPartitionedStorage CosmosDBQuestionnaireQuery = new CosmosDbPartitionedStorage(new CosmosDbPartitionedStorageOptions
        {
            AuthKey = cosmosDBKey,
            ContainerId = cosmosDBConteinerIdQuestionnaires,
            CosmosDbEndpoint = cosmosServiceEndpoint,
            DatabaseId = cosmosDBDatabaseName,
        });

        // Sends Personal Data to database
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

        // Sends Tips to database
        public static async void SendTipsToDB(List<Tip> input, WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Send to DB
            var changes = new Dictionary<string, object>();
            foreach (Tip obj in input)
            {
                changes.Add(obj.TipMessage, obj);
            }

            //var changes = new Dictionary<string, object>() { { PersonalDetailsDialog.PersonalDetails.UserID, PersonalDetailsDialog.PersonalDetails } };
            try
            {
                #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                CosmosDBTipQuery.WriteAsync(changes, cancellationToken);
                #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            catch (Exception e)
            {
                await stepContext.Context.SendActivityAsync($"Error while connecting to database.\n\n{e}");
            }
        }

        // Sends questionnaires to database
        public static async void SendQuestionnairesToDB(string name, List<QuestionTopFive> questions, WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Send to DB
            var pair = new KeyValuePair<string, List<QuestionTopFive>>( name, questions );
            var changes = new Dictionary<string, object>() { { name, pair } };

            try
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                CosmosDBQuestionnaireQuery.WriteAsync(changes, cancellationToken);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            catch (Exception e)
            {
                await stepContext.Context.SendActivityAsync($"Error while connecting to database.\n\n{e}");
            }
        }

        // Clusters all the users based on their personality traits
        public static async Task<uint> ClusteringAsync()
        {
            var dataList = await ClusteringData;

            var context = new MLContext();

            IDataView data = context.Data.LoadFromEnumerable(dataList);

            var trainTestData = context.Data.TrainTestSplit(data, testFraction: 0.2);

            var pipeline = context.Transforms.Concatenate("Features", "Extraversion", "Agreeableness", "Conscientiousness", "Neuroticism", "Openness").Append(context.Clustering.Trainers.KMeans(featureColumnName: "Features", numberOfClusters: 5));

            var model = pipeline.Fit(trainTestData.TrainSet);

            var predictionFunc = context.Model.CreatePredictionEngine<ClusterPersonalDetailsWithoutNull, SeedPrediction>(model);

            var prediction = predictionFunc.Predict(new ClusterPersonalDetailsWithoutNull
            {
                Extraversion = PersonalDetailsDialog.PersonalDetails.Extraversion != null ? (float)PersonalDetailsDialog.PersonalDetails.Extraversion : 0,
                Agreeableness = PersonalDetailsDialog.PersonalDetails.Agreeableness != null ? (float)PersonalDetailsDialog.PersonalDetails.Agreeableness : 0,
                Conscientiousness = PersonalDetailsDialog.PersonalDetails.Conscientiousness != null ? (float)PersonalDetailsDialog.PersonalDetails.Conscientiousness : 0,
                Neuroticism = PersonalDetailsDialog.PersonalDetails.Neuroticism != null ? (float)PersonalDetailsDialog.PersonalDetails.Neuroticism : 0,
                Openness = PersonalDetailsDialog.PersonalDetails.Openness != null ? (float)PersonalDetailsDialog.PersonalDetails.Openness : 0
            });

            return prediction.SelectedClusterId;
        }

        // Requests clustering data from the database
        public static async Task<List<ClusterPersonalDetailsWithoutNull>> QueryClusterDetailsAsync()
        {
            var sqlQueryText = "SELECT c.document.Extraversion, c.document.Agreeableness, c.document.Conscientiousness, c.document.Neuroticism, c.document.Openness FROM c";

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

            CosmosClient cosmosClient = new CosmosClient(cosmosServiceEndpoint, cosmosDBKey);
            Azure.Cosmos.Database database;
            database = await cosmosClient.CreateDatabaseIfNotExistsAsync(cosmosDBDatabaseName);
            Container container;
            container = await database.CreateContainerIfNotExistsAsync(cosmosDBConteinerId, "/id");

            FeedIterator<ClusterPersonalDetails> queryResultSetIterator = container.GetItemQueryIterator<ClusterPersonalDetails>(queryDefinition);

            List<ClusterPersonalDetails> detailsList = new List<ClusterPersonalDetails>();

            while (queryResultSetIterator.HasMoreResults)
            {
                Azure.Cosmos.FeedResponse<ClusterPersonalDetails> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (ClusterPersonalDetails details in currentResultSet)
                {
                    detailsList.Add(details);
                }
            }

            return RemoveNullValues(detailsList);
        }

        // Gets all the tips from the database
        public static async Task<List<Tip>> QueryTipsAsync()
        {
            var sqlQueryText = "SELECT c.document.TipMessage, c.document.Cluster, c.document.Smoker, c.document.LowWaterConsumption, c.document.LowSleep, c.document.LowPhysicalActivity FROM c";

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

            CosmosClient cosmosClient = new CosmosClient(cosmosServiceEndpoint, cosmosDBKey);
            Azure.Cosmos.Database database;
            database = await cosmosClient.CreateDatabaseIfNotExistsAsync(cosmosDBDatabaseName);
            Container container;
            container = await database.CreateContainerIfNotExistsAsync(cosmosDBConteinerIdTips, "/id");

            FeedIterator<Tip> queryResultSetIterator = container.GetItemQueryIterator<Tip>(queryDefinition);

            List<Tip> tipList = new List<Tip>();

            while (queryResultSetIterator.HasMoreResults)
            {
                var currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (Tip details in currentResultSet)
                {
                    tipList.Add(details);
                }
            }

            return tipList;
        }

        // Gets alla the questionnaires from the database
        public static async Task<List<KeyValuePair<string, List<QuestionTopFive>>>> QueryQuestionnairesAsync()
        {
            var sqlQueryText = "SELECT c.document.Key, c.document[\"Value\"] FROM c";

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

            CosmosClient cosmosClient = new CosmosClient(cosmosServiceEndpoint, cosmosDBKey);
            Azure.Cosmos.Database database;
            database = await cosmosClient.CreateDatabaseIfNotExistsAsync(cosmosDBDatabaseName);

            Container container;
            container = await database.CreateContainerIfNotExistsAsync(cosmosDBConteinerIdQuestionnaires, "/id");

            FeedIterator<KeyValuePair<string, List<QuestionTopFive>>> queryResultSetIterator = container.GetItemQueryIterator<KeyValuePair<string, List<QuestionTopFive>>>(queryDefinition);

            var questionnaireList = new List<KeyValuePair<string, List<QuestionTopFive>>>();

            while (queryResultSetIterator.HasMoreResults)
            {
                Azure.Cosmos.FeedResponse<KeyValuePair<string, List<QuestionTopFive>>> currentResultSet = await queryResultSetIterator.ReadNextAsync();

                foreach (KeyValuePair<string, List<QuestionTopFive>> questionnaire in currentResultSet)
                {
                    questionnaireList.Add(questionnaire);
                }
            }

            return ReplaceMissingQuestionValues(questionnaireList);
        }

        // Removes the null values from the clustering dataset
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

        // Puts the default answers if a question does not have any answers
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

        //public static void AddConversationReference(Activity activity)
        //{
        //    var conversationReference = activity.GetConversationReference();
        //    _conversationReferences.AddOrUpdate(conversationReference.User.Id, conversationReference, (key, newValue) => conversationReference);
        //}
    }
}
