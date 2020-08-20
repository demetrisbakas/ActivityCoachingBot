using CoreBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples
{
    public class PersonalDetails
    {
        public string UserID { get; set; }

        public string Name { get; set; }

        public int? Age { get; set; }

        public string Sex { get; set; }

        public float? Extraversion { get; set; } 
        public float? Agreeableness { get; set; }
        public float? Conscientiousness { get; set; }
        public float? Neuroticism { get; set; }
        public float? Openness { get; set; }

        public uint? Cluster { get; set; }

        public IDictionary<string, int> QuestionnaireAnswers { get; set; } = new Dictionary<string, int>();
        //public List<KeyValuePair<string, string>> QuestionnaireAnswers { get; set; } = new List<KeyValuePair<string, string>>();
    }
}
