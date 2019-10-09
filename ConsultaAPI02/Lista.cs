using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ConsultaAPI02
{
    public partial class Pedidos
    {
        [JsonProperty("odata.metadata")]
        public Uri OdataMetadata { get; set; }

        [JsonProperty("odata.count")]
        public long OdataCount { get; set; }

        [JsonProperty("value")]
        public List<Value> Value { get; set; }
    }

    public partial class Value
    {
        [JsonProperty("pedidov")]
        public long Pedidov { get; set; }
    }

    public partial class Pedidos
    {
        public static Pedidos FromJson(string json) => JsonConvert.DeserializeObject<Pedidos>(json, ConsultaAPI02.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this Pedidos self) => JsonConvert.SerializeObject(self, ConsultaAPI02.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
                                {
                                    new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
                                },
        };
    }
}
