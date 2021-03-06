﻿using AdaptiveCards;
using CoreBot;
using Microsoft.Azure.Cosmos;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static CoreBot.QuestionTopFive.PersonalityTrait;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class TopFiveDialog : CancelAndHelpDialog
    {
        private bool finished = false, finishedBefore = true;
        private string activeQuestion;

        public TopFiveDialog()
           : base(nameof(TopFiveDialog))
        {
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                AskQuestionStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> AskQuestionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            foreach (QuestionTopFive obj in QuestionnaireChoiceDialog.activeQuestionnaire)
            {
                if (!PersonalDetailsDialog.PersonalDetails.QuestionnaireAnswers.ContainsKey(obj.Question))
                {
                    activeQuestion = obj.Question;
                    var question = $"I see Myself as Someone Who \n\n{activeQuestion}";
                    var promptMessage = MessageFactory.Text(question, question, InputHints.ExpectingInput);
                    var retryText = $"Please choose one option.\n\n{question}";
                    var retryPromptText = MessageFactory.Text(retryText, retryText, InputHints.ExpectingInput);
                    var answerChoice = obj.Answers;
                    finishedBefore = false;

                    return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = promptMessage, Choices = answerChoice, RetryPrompt = retryPromptText, Style = ListStyle.HeroCard }, cancellationToken);
                }
            }

            finished = true;
            return await stepContext.NextAsync(PersonalDetailsDialog.PersonalDetails, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Send a message the this questionnaire has already been finished
            if (finishedBefore)
            {
                var finishedText = MainDialog.Response.FinishedQuestionnaire();
                var finishedTextMessage = MessageFactory.Text(finishedText, finishedText, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(finishedTextMessage, cancellationToken);
            }

            if (finished)
            {
                await CalculatePersonalityTraitsAsync();
                string tip = await MainDialog.Response.TipMessageAsync();

                MainDialog.WriteToDB(stepContext, cancellationToken);

                var resultCard = CreateAdaptiveCardAttachment();
                var response = MessageFactory.Attachment(resultCard/*, ssml: "Here are your results!"*/);
                await stepContext.Context.SendActivityAsync(response, cancellationToken);

                // Resseting the flag, in case new user comes
                finished = false;
                finishedBefore = true;
                return await stepContext.EndDialogAsync(PersonalDetailsDialog.PersonalDetails, cancellationToken);
            }
            else
            {
                // Adding 1 to the answers index because it starts from 0
                PersonalDetailsDialog.PersonalDetails.QuestionnaireAnswers.Add(activeQuestion, ++((FoundChoice)stepContext.Result).Index);
                MainDialog.WriteToDB(stepContext, cancellationToken);

                return await stepContext.BeginDialogAsync(nameof(TopFiveDialog), PersonalDetailsDialog.PersonalDetails, cancellationToken);
            }
        }

        private async Task CalculatePersonalityTraitsAsync()
        {
            List<int> extraversionList = new List<int>(), agreeablenessList = new List<int>(), conscientiousnessList = new List<int>(), neuroticismList = new List<int>(), opennessList = new List<int>();
            int score;

            var questionnairesNames = new List<string>();
            foreach (KeyValuePair<string, List<QuestionTopFive>> kvp in MainDialog.Response.Questionnaires)
            {
                questionnairesNames.Add(kvp.Key);
            }

            foreach (string name in questionnairesNames)
            {
                List<QuestionTopFive> questionnaire = (from kvp in MainDialog.Response.Questionnaires where kvp.Key == name select kvp.Value).FirstOrDefault();

                foreach (QuestionTopFive obj in questionnaire)
                {
                    if (PersonalDetailsDialog.PersonalDetails.QuestionnaireAnswers.ContainsKey(obj.Question))
                    {
                        if (obj.reverseLogic)
                            // Adding 1 because Count() returns 5 and not 6
                            score = obj.Answers.Count() + 1 - PersonalDetailsDialog.PersonalDetails.QuestionnaireAnswers[obj.Question];
                        else
                            score = PersonalDetailsDialog.PersonalDetails.QuestionnaireAnswers[obj.Question];

                        switch (obj.personalityTrait)
                        {
                            case Extraversion:
                                extraversionList.Add(score);
                                break;
                            case Agreeableness:
                                agreeablenessList.Add(score);
                                break;
                            case Conscientiousness:
                                conscientiousnessList.Add(score);
                                break;
                            case Neuroticism:
                                neuroticismList.Add(score);
                                break;
                            case Openness:
                                opennessList.Add(score);
                                break;
                        }
                    }
                }
            }

            if (extraversionList.Count() > 0)
                PersonalDetailsDialog.PersonalDetails.Extraversion = (float)extraversionList.Average();
            if (agreeablenessList.Count() > 0)
                PersonalDetailsDialog.PersonalDetails.Agreeableness = (float)agreeablenessList.Average();
            if (conscientiousnessList.Count() > 0)
                PersonalDetailsDialog.PersonalDetails.Conscientiousness = (float)conscientiousnessList.Average();
            if (neuroticismList.Count() > 0)
                PersonalDetailsDialog.PersonalDetails.Neuroticism = (float)neuroticismList.Average();
            if (opennessList.Count() > 0)
                PersonalDetailsDialog.PersonalDetails.Openness = (float)opennessList.Average();

            // Start the clustering procedure
            PersonalDetailsDialog.PersonalDetails.Cluster = await MainDialog.ClusteringAsync();
        }

        private Attachment CreateAdaptiveCardAttachment()
        {
            AdaptiveCard card = new AdaptiveCard("1.0");

            // Specify speech for the card.  
            card.Speak = "Here are your results!";

            // Body content  
            // Add text to the card.  
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = "Here are your results!",
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder
            });

            // Add text to the card.  
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = $"Extraversion: { PersonalDetailsDialog.PersonalDetails.Extraversion}"
            });
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = $"Agreeableness: {PersonalDetailsDialog.PersonalDetails.Agreeableness}"
            });
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = $"Conscientiousness: {PersonalDetailsDialog.PersonalDetails.Conscientiousness}"
            });
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = $"Neuroticism: {PersonalDetailsDialog.PersonalDetails.Neuroticism}"
            });
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = $"Openness: {PersonalDetailsDialog.PersonalDetails.Openness}"
            });

            // Create the attachment with adapative card.  
            Attachment attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };
            return attachment;
        }
    }
}
