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
        List<string> greetList = new List<string>() { "Hello!", "Hi!", "Hey!" };
        List<string> nameQuestionList = new List<string>() { "What is your name?", "How can I call you?" };



        public string Greet()
        {
            return greetList[rnd.Next(greetList.Count)];
        }

        public string Greet(string name)
        {
            return $"{Regex.Replace(Greet(), @"[^\w\s]", "")}, {name}!";
        }

        public string AskName()
        {
            return nameQuestionList[rnd.Next(nameQuestionList.Count)];
        }
    }
}
