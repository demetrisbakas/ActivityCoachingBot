// <auto-generated>
// Code generated by LUISGen .\FlightBooking.json -cs Luis.FlightBooking -o .
// Tool github: https://github.com/microsoft/botbuilder-tools
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;

namespace Microsoft.BotBuilderSamples
{
    public partial class FlightBooking: IRecognizerConvert
    {
        public string Text;
        public string AlteredText;
        public enum Intent {
            //BookFlight,
            Cancel,
            //GetWeather,
            Greet,
            None
        };
        public Dictionary<Intent, IntentScore> Intents;

        public class _Entities
        {

            // Built-in entities
            public DateTimeSpec[] datetime;
            public string[] personName;
            public Age[] age;

            // Lists
            //public string[][] Airport;

            // Composites
            //public class _InstanceFrom
            //{
            //    public InstanceData[] Airport;
            //}
            //public class FromClass
            //{
            //    public string[][] Airport;
            //    [JsonProperty("$instance")]
            //    public _InstanceFrom _instance;
            //}
            //public FromClass[] From;

            //public class _InstanceTo
            //{
            //    public InstanceData[] Airport;
            //}
            //public class ToClass
            //{
            //    public string[][] Airport;
            //    [JsonProperty("$instance")]
            //    public _InstanceTo _instance;
            //}
            //public ToClass[] To;

            // Instance
            public class _Instance
            {
                public InstanceData[] datetime;
                public InstanceData[] personName;
                public InstanceData[] Airport;
                public InstanceData[] From;
                public InstanceData[] To;
            }
            [JsonProperty("$instance")]
            public _Instance _instance;
        }
        public _Entities Entities;

        [JsonExtensionData(ReadData = true, WriteData = true)]
        public IDictionary<string, object> Properties {get; set; }

        public void Convert(dynamic result)
        {
            var app = JsonConvert.DeserializeObject<FlightBooking>(JsonConvert.SerializeObject(result, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            Text = app.Text;
            AlteredText = app.AlteredText;
            Intents = app.Intents;
            Entities = app.Entities;
            Properties = app.Properties;
        }

        public (Intent intent, double score) TopIntent()
        {
            Intent maxIntent = Intent.None;
            var max = 0.0;
            foreach (var entry in Intents)
            {
                if (entry.Value.Score > max)
                {
                    maxIntent = entry.Key;
                    max = entry.Value.Score.Value;
                }
            }
            return (maxIntent, max);
        }
    }
}
