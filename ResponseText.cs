using CoreBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static CoreBot.QuestionTopFive.PersonalityTrait;

namespace Microsoft.BotBuilderSamples
{
    public class ResponseText
    {
        private readonly Random rnd = new Random();

        // Response Lists
        private readonly List<string> greetList = new List<string>() { "Hello!", "Hi!", "Hey!" };
        private readonly List<string> nameQuestionList = new List<string>() { "What is your name?", "How can I call you?" };
        private readonly List<string> nameRetryList = new List<string>() { "Can you please tell me your name again?", "I didn't got your name, can you try again?" };
        private readonly List<string> ageQuestionList = new List<string>() { "How old are you?", "What is your age?" };
        private readonly List<string> ageRetryList = new List<string>() { "Can you please tell me your age again?", "I didn't get you age, can you try again?" };
        private readonly List<string> sexQuestionList = new List<string>() { "What is your sex?", "Are you a male or a female?" };
        private readonly List<string> questionnaireQuestionList = new List<string>() { "Please choose a questionnaire", "Which questionnaire would you like?" };
        private readonly List<string> finishedQuestionnaireList = new List<string>() { "This questionnaire has already been finished" };
        private readonly List<string> welcomeMessageList = new List<string>() { "What can I help you with today?\n\nGreet me to enter the personal details dialog, or ask whatever you like." };


        public IDictionary<string, List<QuestionTopFive>> Questionnaires { get; set; } = new Dictionary<string, List<QuestionTopFive>>();
        public List<QuestionTopFive> questionnaire = new List<QuestionTopFive>();


        public ResponseText()
        {
            // Populate questionnaire
            questionnaire.Add(new QuestionTopFive("Is talkative", Extraversion));
            questionnaire.Add(new QuestionTopFive("Does a thorough job", Agreeableness, true));
            questionnaire.Add(new QuestionTopFive("Is depressed, blue", Conscientiousness));
            questionnaire.Add(new QuestionTopFive("Is original, comes up with new ideas", Neuroticism));
            questionnaire.Add(new QuestionTopFive("Is reserved", Openness));

            Questionnaires.Add("Big Five", questionnaire);
        }

        private string RandomiseList(List<string> inputList)
        {
            return inputList[rnd.Next(inputList.Count)];
        }

        public string Greet()
        {
            return RandomiseList(greetList);
        }

        public string Greet(string name)
        {
            return $"{Regex.Replace(Greet(), @"[^\w\s]", "")}, {name}!";
        }

        public string AskName()
        {
            return RandomiseList(nameQuestionList);
        }

        public string RetryName()
        {
            return RandomiseList(nameRetryList);
        }

        public string AskAge()
        {
            return RandomiseList(ageQuestionList);
        }

        public string RetryAge()
        {
            return RandomiseList(ageRetryList);
        }

        public string AskSex()
        {
            return RandomiseList(sexQuestionList);
        }

        public string ChooseQuestionnaire()
        {
            return RandomiseList(questionnaireQuestionList);
        }

        public string FinishedQuestionnaire()
        {
            return RandomiseList(finishedQuestionnaireList);
        }

        public string WelcomeMessage()
        {
            return RandomiseList(welcomeMessageList);
        }
    }
}
