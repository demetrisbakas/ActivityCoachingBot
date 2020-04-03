using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples
{
    public class ResponseText
    {
        readonly Random rnd = new Random();

        // Response Lists
        private readonly List<string> greetList = new List<string>() { "Hello!", "Hi!", "Hey!" };
        private readonly List<string> nameQuestionList = new List<string>() { "What is your name?", "How can I call you?" };
        private readonly List<string> ageQuestionList = new List<string>() { "How old are you?", "What is your age?" };
        private readonly List<string> sexQuestionList = new List<string>() { "What is your sex?", "Are you a male or a female?" };


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

        public string AskAge()
        {
            return RandomiseList(ageQuestionList);
        }

        public string AskSex()
        {
            return RandomiseList(sexQuestionList);
        }

    }
}
