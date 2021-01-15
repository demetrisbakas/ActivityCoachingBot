using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreBot
{
    public class Tip
    {
        public string TipMessage { get; set; }
        public int Cluster { get; set; }

        public bool? Smoker { get; set; }
        public bool? LowWaterConsumption { get; set; }
        public bool? LowSleep { get; set; }
        public bool? LowPhysicalActivity { get; set; }
    }
}
