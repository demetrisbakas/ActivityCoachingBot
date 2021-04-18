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
                // Cluster rule
                .Query(() => tips, x => x.Match<Tip>(o => o.Cluster == personalDetails.Cluster || o.Cluster == null,
                    // Smoker rule
                    o => o.Smoker == personalDetails.Smoker || o.Smoker == null,
                    // Low sleep rule
                    o => (o.LowSleep == true && personalDetails.Sleep <= lowSleepThreshold) || o.LowSleep == null,
                    // Low physical activity rule
                    o => (o.LowPhysicalActivity == true && personalDetails.PhysicalActivity <= lowPhysicalActivityThreshold) || o.LowPhysicalActivity == null,
                    // Low water consumption rule
                    o => (o.LowWaterConsumption == true && personalDetails.WaterConsumption <= lowWaterConsumption) || o.LowWaterConsumption == null)
                    .Collect()
                    .Where(c => c.Any()));

            //When()
            //    .Match<PersonalDetails>(() => personalDetails)
            //    // Cluster rule
            //    .Query(() => tips, x => x.Match<Tip>(o => o.Cluster == personalDetails.Cluster || o.Cluster == null,
            //        // Smoker rule
            //        o => o.Smoker == personalDetails.Smoker && personalDetails.Smoker == true,
            //        // Low sleep rule
            //        o => o.LowSleep == true && personalDetails.Sleep <= lowSleepThreshold)
            //        .Collect()
            //        .Where(c => c.Any()));



            Then()
                .Do(ctx => AddTips(tips));
        } 

        private static void AddTips(IEnumerable<Tip> tips)
        {
            //foreach (var tip in tips)
            //{
            //    MainDialog.Response.TipList.Add(tip);
            //}

            MainDialog.Response.TipList.AddRange(tips.Except(MainDialog.Response.TipList));
        }
    }
}
