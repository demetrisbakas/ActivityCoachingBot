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
    public class ConnectionRecognizer : IRecognizer
    {
        // QnA Maker Credentials
        private string QnAKnowledgebaseId = "fa15132e-f5ee-4d77-b221-4612dc834c96";
        private string QnAEndpointKey = "e1f51738-0030-42fb-8acf-40ffcb8f48d1";
        private string QnAEndpointHostName = "https://activitycoachingbotqnaservice.azurewebsites.net/qnamaker";

        private readonly LuisRecognizer _recognizer;

        public ConnectionRecognizer(IConfiguration configuration)
        {
            var luisIsConfigured = !string.IsNullOrEmpty(configuration["LuisAppId"]) && !string.IsNullOrEmpty(configuration["LuisAPIKey"]) && !string.IsNullOrEmpty(configuration["LuisAPIHostName"]);
            if (luisIsConfigured)
            {
                var luisApplication = new LuisApplication(
                    configuration["LuisAppId"],
                    configuration["LuisAPIKey"],
                    "https://" + configuration["LuisAPIHostName"]);

                var recognizerOptions = new LuisRecognizerOptionsV3(luisApplication)
                {
                    PredictionOptions = new Bot.Builder.AI.LuisV3.LuisPredictionOptions
                    {
                        IncludeInstanceData = true,
                    }
                };

                _recognizer = new LuisRecognizer(recognizerOptions);
            }

            SampleQnA = new QnAMaker(new QnAMakerEndpoint
            {
                KnowledgeBaseId = QnAKnowledgebaseId,
                EndpointKey = QnAEndpointKey,
                Host = QnAEndpointHostName
            });
        }

        public QnAMaker SampleQnA { get; private set; }

        // Returns true if luis is configured in the appsettings.json and initialized.
        public virtual bool IsConfigured => _recognizer != null;

        public virtual async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
            => await _recognizer.RecognizeAsync(turnContext, cancellationToken);

        public virtual async Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
            => await _recognizer.RecognizeAsync<T>(turnContext, cancellationToken);
    }
}
