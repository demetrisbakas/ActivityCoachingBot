﻿using CoreBot;
using Microsoft.BotBuilderSamples.Dialogs;
using NRules;
using NRules.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
        private readonly List<string> welcomeMessageList = new List<string>() { "Hey I'm an Activity Coach bot!\n\nI can help you by providing daily tips to improve your everyday life, but you need to answer some questionnaires first.\n\nGreet me to enter the personal details dialog, or ask whatever you like.\n\nIf you need any help, ask away!" };
        private readonly List<string> smokerMessageList = new List<string>() { "Are you a smoker?" };
        private readonly List<string> waterConsumptionMessageList = new List<string>() { "How many cups of water do you drink every day?" };
        private readonly List<string> waterConsumptionRetryList = new List<string>() { "Can you repeat the number of cups please?" };
        private readonly List<string> sleepMessageList = new List<string>() { "How many hours of sleep do you get every day?" };
        private readonly List<string> sleepRetryList = new List<string>() { "Can you repeat the number of hours you sleep please?" };
        private readonly List<string> physicalActivityMessageList = new List<string>() { "How many hours of physical excersise do you get every week?" };
        private readonly List<string> physicalActivityRetryList = new List<string>() { "Can you repeat the number of hours you excersise please?" };
        private readonly List<string> reenterDetailsMessageList = new List<string>() { "You have already entered all of your personal details, would you like to change them?" };
        private readonly List<string> askPasswordList = new List<string>() { "Please input your passphrase" };
        private readonly List<string> wrongPasswordList = new List<string>() { "Wrong Passphrase" , "Your passphrase appears to be wrong" };
        private readonly List<string> uploadTipsOrQuestionnairesList = new List<string>() { "What do you wish to upload?", "What would you like to upload?" };
        private readonly List<string> enterNumberOfTipsList = new List<string>() { "How many tips would you like to upload?", "Enter the number of tips you would like to upload?" };
        private readonly List<string> reenterNumberOfTipsList = new List<string>() { "Please enter a number bigger than 0"};
        private readonly List<string> uploadingDataMessageList = new List<string>() { "Uploading your data..." };
        private readonly List<string> enterNameOfQuestionnaireList = new List<string>() { "What is the title of your questionnaire?" };
        private readonly List<string> enterNumberOfQuestionsList = new List<string>() { "How many questions does your questionnaire contain?" };
        private readonly List<string> reenterNumberOfQuestionsList = new List<string>() { "Can you repeat the number of questions please?" };
        private readonly List<string> helpMessageList = new List<string>() { "This is the help message.\n\nBy greeting me you will enter the regular flow of the dialog.\n\nOther functionality includes:\n\nAsking to change your personal details\n\nAsking to answer a questionnaire\n\nAsking to upload data like tips and questionnaires, provided you have the credentials\n\nYou can tell me to exit or quit each dialog at any time" };

        public List<Tip> TipList { get; set; } = new List<Tip>();
        public List<KeyValuePair<string, List<QuestionTopFive>>> Questionnaires { get; set; } = new List<KeyValuePair<string, List<QuestionTopFive>>>();

        public ResponseText()
        {
        }

        private string RandomiseList(List<string> inputList)
        {
            return inputList[rnd.Next(inputList.Count)];
        }

        public string Answer1()
        {
            return "Disagree strongly";
        }

        public string Answer2()
        {
            return "Disagree a little";
        }

        public string Answer3()
        {
            return "Neither agree nor disagree";
        }

        public string Answer4()
        {
            return "Agree a little";
        }

        public string Answer5()
        {
            return "Agree Strongly";
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

        public string AskSmoker()
        {
            return RandomiseList(smokerMessageList);
        }

        public string AskWater()
        {
            return RandomiseList(waterConsumptionMessageList);
        }

        public string RetryWater()
        {
            return RandomiseList(waterConsumptionRetryList);
        }
        public string AskSleep()
        {
            return RandomiseList(sleepMessageList);
        }

        public string RetrySleep()
        {
            return RandomiseList(sleepRetryList);
        }

        public string AskPhysicalActivity()
        {
            return RandomiseList(physicalActivityMessageList);
        }
        public string RetryPhysycalActivity()
        {
            return RandomiseList(physicalActivityRetryList);
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

        public string ReenterDetailsMessage()
        {
            return RandomiseList(reenterDetailsMessageList);
        }

        public string AskPassword()
        {
            return RandomiseList(askPasswordList);
        }

        public string WrongPassword()
        {
            return RandomiseList(wrongPasswordList);
        }

        public string UploadTipsOrQuestionnaires()
        {
            return RandomiseList(uploadTipsOrQuestionnairesList);
        }

        public string EnterNumberOfTips()
        {
            return RandomiseList(enterNumberOfTipsList);
        }

        public string ReenterNumberOfTips()
        {
            return RandomiseList(reenterNumberOfTipsList);
        }

        public string UploadingDataMessage()
        {
            return RandomiseList(uploadingDataMessageList);
        }

        public string EnterNameOfQuestionnaire()
        {
            return RandomiseList(enterNameOfQuestionnaireList);
        }

        public string EnterNumberOfQuestions()
        {
            return RandomiseList(enterNumberOfQuestionsList);
        }

        public string ReenterNumberOfQuestions()
        {
            return RandomiseList(reenterNumberOfQuestionsList);
        }

        public string HelpMessage()
        {
            return RandomiseList(helpMessageList);
        }

        public async Task<string> TipMessageAsync()
        {
            TipList = new List<Tip>();
            var tipList = await MainDialog.QueryTipsAsync();

            // Rules engine here
            // Load rules
            var repository = new RuleRepository();
            repository.Load(x => x.From(typeof(TipChooseRule).Assembly));

            // Compile rules
            var factory = repository.Compile();

            // Create a working session
            var session = factory.CreateSession();

            // Insert facts into rules engine's memory
            session.Insert(PersonalDetailsDialog.PersonalDetails);
            foreach (Tip obj in tipList)
            {
                session.Insert(obj);
            }

            // Start match/resolve/act cycle
            session.Fire();

            return RandomiseList(TipList.Select(l => l.TipMessage).ToList());
        }
    }
}
