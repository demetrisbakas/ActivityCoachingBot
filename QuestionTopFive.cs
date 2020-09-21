using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.BotBuilderSamples;
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
        public enum PersonalityTrait
        {
            Extraversion,
            Agreeableness,
            Conscientiousness,
            Neuroticism,
            Openness
        };
        public PersonalityTrait personalityTrait { get; set; }

        public bool reverseLogic = false;

        public QuestionTopFive()
        {
        }

        public QuestionTopFive(string question, PersonalityTrait personalityTrait)
        {
            Question = question;
            this.personalityTrait = personalityTrait;

            Answers.Add(new Choice("Not at all"));
            Answers.Add(new Choice("A little bit"));
            Answers.Add(new Choice("Neutral"));
            Answers.Add(new Choice("Much"));
            Answers.Add(new Choice("Very much"));
        }

        public QuestionTopFive(string question, PersonalityTrait personalityTrait, bool reverseLogic)
        {
            Question = question;
            this.personalityTrait = personalityTrait;
            this.reverseLogic = reverseLogic;

            Answers.Add(new Choice("Not at all"));
            Answers.Add(new Choice("A little bit"));
            Answers.Add(new Choice("Neutral"));
            Answers.Add(new Choice("Much"));
            Answers.Add(new Choice("Very much"));
        }

        public QuestionTopFive(string question, PersonalityTrait personalityTrait, string answer1, string answer2, string answer3, string answer4, string answer5)
        {
            Question = question;
            this.personalityTrait = personalityTrait;

            Answers.Add(new Choice(answer1));
            Answers.Add(new Choice(answer2));
            Answers.Add(new Choice(answer3));
            Answers.Add(new Choice(answer4));
            Answers.Add(new Choice(answer5));
        }

        public QuestionTopFive(string question, PersonalityTrait personalityTrait, string answer1, string answer2, string answer3, string answer4, string answer5, bool reverseLogic)
        {
            Question = question;
            this.personalityTrait = personalityTrait;
            this.reverseLogic = reverseLogic;

            Answers.Add(new Choice(answer1));
            Answers.Add(new Choice(answer2));
            Answers.Add(new Choice(answer3));
            Answers.Add(new Choice(answer4));
            Answers.Add(new Choice(answer5));
        }
    }
}
