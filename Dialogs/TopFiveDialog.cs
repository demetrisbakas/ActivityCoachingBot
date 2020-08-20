using CoreBot;
using Microsoft.Azure.Cosmos;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
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
            foreach (QuestionTopFive obj in QuestionnaireChoiceDialog.activeQuestionnaire /*MainDialog.Response.questionnaire*/)
            {
                if (!PersonalDetailsDialog.PersonalDetails.QuestionnaireAnswers.ContainsKey(obj.Question))
                {
                    activeQuestion = obj.Question;
                    var question = $"I see Myself as Someone Who\n\n{activeQuestion}";
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

                MainDialog.WriteToDB(stepContext, cancellationToken);

                // Show results
                var resultsText = $"Here are your results!\n\nExtraversion: {PersonalDetailsDialog.PersonalDetails.Extraversion}\n\nAgreeableness: {PersonalDetailsDialog.PersonalDetails.Agreeableness}\n\nConscientiousness: {PersonalDetailsDialog.PersonalDetails.Conscientiousness}\n\nNeuroticism: {PersonalDetailsDialog.PersonalDetails.Neuroticism}\n\nOpenness: {PersonalDetailsDialog.PersonalDetails.Openness}\n\nCLUSTER: {PersonalDetailsDialog.PersonalDetails.Cluster}";
                var resultsTextMessage = MessageFactory.Text(resultsText, resultsText, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(resultsTextMessage, cancellationToken);

                // Resseting the flag, in case new user comes
                finished = false;
                finishedBefore = true;
                return await stepContext.EndDialogAsync(PersonalDetailsDialog.PersonalDetails, cancellationToken);
            }
            else
            {
                //PersonalDetailsDialog.PersonalDetails.QuestionnaireAnswers.Add(new KeyValuePair<string, string>(activeQuestion, ((FoundChoice)stepContext.Result).Value));
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

            var questionnairesNames = MainDialog.Response.Questionnaires.Keys.ToList();
            foreach (string name in questionnairesNames)
            {
                List<QuestionTopFive> questionnaire = MainDialog.Response.Questionnaires[name];

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









            PersonalDetailsDialog.PersonalDetails.Cluster = await MainDialog.ClusteringAsync();
            //await MainDialog.ClusteringAsync();
        }
    }
}
