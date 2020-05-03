using Microsoft.Bot.Builder.Dialogs.Choices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreBot
{
    public class QuestionTopFive
    {
        public string Question { get; set; }
        public List<Choice> Answers { get; set; } = new List<Choice>();

        public QuestionTopFive()
        {
        }

        public QuestionTopFive(string question)
        {
            Question = question;

            Answers.Add(new Choice("Not at all"));
            Answers.Add(new Choice("A little bit"));
            Answers.Add(new Choice("Neutral"));
            Answers.Add(new Choice("Much"));
            Answers.Add(new Choice("Very much"));
        }

        public QuestionTopFive(string question, string answer1, string answer2, string answer3, string answer4, string answer5)
        {
            Question = question;

            Answers.Add(new Choice(answer1));
            Answers.Add(new Choice(answer2));
            Answers.Add(new Choice(answer3));
            Answers.Add(new Choice(answer4));
            Answers.Add(new Choice(answer5));
        }
    }
}
