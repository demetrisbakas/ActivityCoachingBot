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

                // Sand to DB
                var changes = new Dictionary<string, object>() { { PersonalDetailsDialog.PersonalDetails.UserID, PersonalDetailsDialog.PersonalDetails } };
                try
                {
                    await MainDialog.CosmosDBQuery.WriteAsync(changes, cancellationToken);
                }
                catch (Exception e)
                {
                    await stepContext.Context.SendActivityAsync($"Error while connecting to database.\n\n{e}");
                }

                // Resseting the flag, in case new user comes
                finished = false;
                return await stepContext.EndDialogAsync(PersonalDetailsDialog.PersonalDetails, cancellationToken);
            }
            else
            {
                //PersonalDetailsDialog.PersonalDetails.QuestionnaireAnswers.Add(new KeyValuePair<string, string>(activeQuestion, ((FoundChoice)stepContext.Result).Value));
                // Adding 1 to the answers index because it starts from 0
                PersonalDetailsDialog.PersonalDetails.QuestionnaireAnswers.Add(activeQuestion, ++((FoundChoice)stepContext.Result).Index);
                return await stepContext.BeginDialogAsync(nameof(TopFiveDialog), PersonalDetailsDialog.PersonalDetails, cancellationToken);
            }
        }

        private void CalculatePersonalityTraits(List<QuestionTopFive> questionnaire, IDictionary<string, int> QuestionnaireAnswers)
        {
            List<int> extraversion = new List<int>(), agreeableness = new List<int>(), conscientiousness = new List<int>(), neuroticism = new List<int>(), openness = new List<int>();

            foreach (QuestionTopFive obj in questionnaire)
            {
                if (QuestionnaireAnswers.ContainsKey(obj.Question))
                {
                    switch (obj.personalityTrait)
                    {
                        case QuestionTopFive.PersonalityTrait.Extraversion:
                            extraversion.Add(QuestionnaireAnswers[obj.Question]);
                            break;
                        case QuestionTopFive.PersonalityTrait.Agreeableness:
                            agreeableness.Add(QuestionnaireAnswers[obj.Question]);
                            break;
                        case QuestionTopFive.PersonalityTrait.Conscientiousness:
                            conscientiousness.Add(QuestionnaireAnswers[obj.Question]);
                            break;
                        case QuestionTopFive.PersonalityTrait.Neuroticism:
                            neuroticism.Add(QuestionnaireAnswers[obj.Question]);
                            break;
                        case QuestionTopFive.PersonalityTrait.Openness:
                            openness.Add(QuestionnaireAnswers[obj.Question]);
                            break;
                    }
                }
            }

            if (extraversion.Count() > 0)
                PersonalDetailsDialog.PersonalDetails.extraversion = extraversion.Sum() / extraversion.Count();
            if (agreeableness.Count() > 0)
                PersonalDetailsDialog.PersonalDetails.agreeableness = agreeableness.Sum() / agreeableness.Count();
            if (conscientiousness.Count() > 0)
                PersonalDetailsDialog.PersonalDetails.conscientiousness = conscientiousness.Sum() / conscientiousness.Count();
            if (neuroticism.Count() > 0)
                PersonalDetailsDialog.PersonalDetails.neuroticism = neuroticism.Sum() / neuroticism.Count();
            if (openness.Count() > 0)
                PersonalDetailsDialog.PersonalDetails.openness = openness.Sum() / openness.Count();
        }
    }
}
