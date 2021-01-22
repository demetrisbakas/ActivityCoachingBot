using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.BotBuilderSamples;
using Microsoft.BotBuilderSamples.Dialogs;
using NRules.Fluent.Dsl;

namespace CoreBot
{
    public class TipChooseRule : Rule
    {
        // Values are inclusive
        private readonly int lowSleepThreshold = 5;
        private readonly int lowPhysicalActivityThreshold = 4;
        private readonly int lowWaterConsumption = 4;

        public override void Define()
        {
            //List<Tip> tipList = new List<Tip>();
            PersonalDetails personalDetails = default;
            IEnumerable<Tip> tips = default;

            When()
                .Match<PersonalDetails>(() => personalDetails)
                .Query(() => tips, x => x.Match<Tip>(o => o.Cluster == personalDetails.Cluster)
                    .Collect()
                    .Where(c => c.Any()));

            Then()
                .Do(ctx => AddTips(tips));
        } 

        private static void AddTips(IEnumerable<Tip> tips)
        {
            //MainDialog.Response.TipList.Add(tip);

            //foreach (var tip in tips)
            //{
            //    MainDialog.Response.TipList.Add(tip);
            //}

            MainDialog.Response.TipList.AddRange(tips.Except(MainDialog.Response.TipList));
        }
    }
}
