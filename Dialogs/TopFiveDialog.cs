using CoreBot;
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
        private bool finished = false;
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
            foreach (QuestionTopFive obj in MainDialog.Response.questionnaire)
            {
                if (!PersonalDetailsDialog.PersonalDetails.QuestionnaireAnswers.ContainsKey(obj.Question))
                {
                    activeQuestion = obj.Question;
                    var question = $"I see Myself as Someone Who\n\n{activeQuestion}";
                    var promptMessage = MessageFactory.Text(question, question, InputHints.ExpectingInput);
                    var retryText = $"Please choose one option.\n\n{question}";
                    var retryPromptText = MessageFactory.Text(retryText, retryText, InputHints.ExpectingInput);
                    var answerChoice = obj.Answers;

                    return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = promptMessage, Choices = answerChoice, RetryPrompt = retryPromptText }, cancellationToken);
                }
            }

            finished = true;
            return await stepContext.NextAsync(PersonalDetailsDialog.PersonalDetails, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (finished)
            {
                CalculatePersonalityTraits(MainDialog.Response.questionnaire, PersonalDetailsDialog.PersonalDetails.QuestionnaireAnswers);

                WriteToDB(stepContext, cancellationToken);

                // Resseting the flag, in case new user comes
                finished = false;
                return await stepContext.EndDialogAsync(PersonalDetailsDialog.PersonalDetails, cancellationToken);
            }
            else
            {
                //PersonalDetailsDialog.PersonalDetails.QuestionnaireAnswers.Add(new KeyValuePair<string, string>(activeQuestion, ((FoundChoice)stepContext.Result).Value));
                // Adding 1 to the answers index because it starts from 0
                PersonalDetailsDialog.PersonalDetails.QuestionnaireAnswers.Add(activeQuestion, ++((FoundChoice)stepContext.Result).Index);
                WriteToDB(stepContext, cancellationToken);

                return await stepContext.BeginDialogAsync(nameof(TopFiveDialog), PersonalDetailsDialog.PersonalDetails, cancellationToken);
            }
        }

        private void CalculatePersonalityTraits(List<QuestionTopFive> questionnaire, IDictionary<string, int> QuestionnaireAnswers)
        {
            List<int> extraversionList = new List<int>(), agreeablenessList = new List<int>(), conscientiousnessList = new List<int>(), neuroticismList = new List<int>(), opennessList = new List<int>();
            int score;

            foreach (QuestionTopFive obj in questionnaire)
            {
                if (QuestionnaireAnswers.ContainsKey(obj.Question))
                {
                    if (obj.reverseLogic)
                        // Adding 1 because Count() returns 5 and not 6
                        score = obj.Answers.Count() + 1 - QuestionnaireAnswers[obj.Question];
                    else
                        score = QuestionnaireAnswers[obj.Question];

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

            if (extraversionList.Count() > 0)
                PersonalDetailsDialog.PersonalDetails.Extraversion = extraversionList.Average();
            if (agreeablenessList.Count() > 0)
                PersonalDetailsDialog.PersonalDetails.Agreeableness = agreeablenessList.Average();
            if (conscientiousnessList.Count() > 0)
                PersonalDetailsDialog.PersonalDetails.Conscientiousness = conscientiousnessList.Average();
            if (neuroticismList.Count() > 0)
                PersonalDetailsDialog.PersonalDetails.Neuroticism = neuroticismList.Average();
            if (opennessList.Count() > 0)
                PersonalDetailsDialog.PersonalDetails.Openness = opennessList.Average();
        }

        private async void WriteToDB(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Sand to DB
            var changes = new Dictionary<string, object>() { { PersonalDetailsDialog.PersonalDetails.UserID, PersonalDetailsDialog.PersonalDetails } };
            try
            {
                MainDialog.CosmosDBQuery.WriteAsync(changes, cancellationToken);
            }
            catch (Exception e)
            {
                await stepContext.Context.SendActivityAsync($"Error while connecting to database.\n\n{e}");
            }
        }
    }
}
