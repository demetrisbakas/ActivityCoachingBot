using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreBot
{
    public class SeedPrediction
    {
        [ColumnName("PredictedLabel")]
        public uint SelectedClusterId;
        [ColumnName("Score")]
        public float[] Distance;
    }
}
