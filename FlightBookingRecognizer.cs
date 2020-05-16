// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Extensions.Configuration;
using Microsoft.Bot.Builder.AI.QnA;

namespace Microsoft.BotBuilderSamples
{
    public class FlightBookingRecognizer : IRecognizer
    {
        // QnA stuff
        string QnAKnowledgebaseId = "fa15132e-f5ee-4d77-b221-4612dc834c96";
        string QnAEndpointKey = "e1f51738-0030-42fb-8acf-40ffcb8f48d1";
        string QnAEndpointHostName = "https://activitycoachingbotqnaservice.azurewebsites.net/qnamaker";



        private readonly LuisRecognizer _recognizer;

        public FlightBookingRecognizer(IConfiguration configuration)
        {
            var luisIsConfigured = !string.IsNullOrEmpty(configuration["LuisAppId"]) && !string.IsNullOrEmpty(configuration["LuisAPIKey"]) && !string.IsNullOrEmpty(configuration["LuisAPIHostName"]);
            if (luisIsConfigured)
            {
                var luisApplication = new LuisApplication(
                    configuration["LuisAppId"],
                    configuration["LuisAPIKey"],
                    "https://" + configuration["LuisAPIHostName"]);

                _recognizer = new LuisRecognizer(luisApplication);
            }





            // TEST
            SampleQnA = new QnAMaker(new QnAMakerEndpoint
            {
                KnowledgeBaseId = QnAKnowledgebaseId, //configuration[QnAKnowledgebaseId],
                EndpointKey = QnAEndpointKey, //configuration[QnAEndpointKey],
                Host = QnAEndpointHostName //configuration[QnAEndpointHostName]
            });
            //
        }

        // Returns true if luis is configured in the appsettings.json and initialized.
        public virtual bool IsConfigured => _recognizer != null;

        public virtual async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
            => await _recognizer.RecognizeAsync(turnContext, cancellationToken);

        public virtual async Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
            => await _recognizer.RecognizeAsync<T>(turnContext, cancellationToken);








        public QnAMaker SampleQnA { get; private set; }
    }
}
