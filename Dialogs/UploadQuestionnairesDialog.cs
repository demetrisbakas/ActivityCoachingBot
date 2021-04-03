using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bot.AdaptiveCard.Prompt;
using AdaptiveCardPromptSample.Welcome;
using CoreBot;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class UploadQuestionnairesDialog : CancelAndHelpDialog
    {
        static string AdaptivePromptId = "adaptive";

        public UploadQuestionnairesDialog()
           : base(nameof(UploadQuestionnairesDialog))
        {
            AddDialog(new AdaptiveCardPrompt(AdaptivePromptId));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ShowCardAsync,
                FinalStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ShowCardAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var cardJson = PrepareCard.ReadCard("question.json");

            var cardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(cardJson),
            };

            var opts = new PromptOptions
            {
                Prompt = new Activity
                {
                    Attachments = new List<Attachment>() { cardAttachment },
                    Type = ActivityTypes.Message
                }
            };

            return await stepContext.PromptAsync(AdaptivePromptId, opts, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var result = JsonConvert.DeserializeObject<QuestionJSON>(stepContext.Result.ToString());


            NameOfQuestionnaireDialog.QuestionnaireToUpload.Add(ConvertToQuestionTopFive(result));

            if (--NameOfQuestionnaireDialog.NumberOfQuestionsInQuestionnaire == 0)
            {
                // Send to database
                MainDialog.SendQuestionnairesToDB(NameOfQuestionnaireDialog.NameOfQuestionnaire, NameOfQuestionnaireDialog.QuestionnaireToUpload, stepContext, cancellationToken);

                // Show uploading message to the user
                var MsgText = MainDialog.Response.UploadingDataMessage();
                var promptMessage = MessageFactory.Text(MsgText, MsgText, InputHints.ExpectingInput);
                await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

                return await stepContext.EndDialogAsync(PersonalDetailsDialog.PersonalDetails, cancellationToken);
            }
            else
                return await stepContext.BeginDialogAsync(nameof(UploadQuestionnairesDialog), NameOfQuestionnaireDialog.NumberOfQuestionsInQuestionnaire, cancellationToken);
        }

        private QuestionTopFive ConvertToQuestionTopFive(QuestionJSON input)
        {
            QuestionTopFive output;
            var question = input.Question;
            var reverseLogic = input.ReverseLogic;
            QuestionTopFive.PersonalityTrait personalityTrait;

            switch (input.PersonalityTrait)
            {
                case "Extraversion":
                    personalityTrait = QuestionTopFive.PersonalityTrait.Extraversion;
                    break;
                case "Agreeableness":
                    personalityTrait = QuestionTopFive.PersonalityTrait.Agreeableness;
                    break;
                case "Conscientiousness":
                    personalityTrait = QuestionTopFive.PersonalityTrait.Conscientiousness;
                    break;
                case "Neuroticism":
                    personalityTrait = QuestionTopFive.PersonalityTrait.Neuroticism;
                    break;
                case "Openness":
                    personalityTrait = QuestionTopFive.PersonalityTrait.Openness;
                    break;
                default:
                    personalityTrait = QuestionTopFive.PersonalityTrait.Extraversion;
                    break;
            }

            if ((input.Answer1 == "") && (input.Answer2 == "") && (input.Answer3 == "") && (input.Answer4 == "") && (input.Answer5 == ""))
                output = new QuestionTopFive(question, personalityTrait, reverseLogic);
            else
                output = new QuestionTopFive(question, personalityTrait, input.Answer1, input.Answer2, input.Answer3, input.Answer4, input.Answer5 ,reverseLogic);

            return output;
        }
    }
}
