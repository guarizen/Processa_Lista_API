using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FaturamentoAutomatico
{
    public partial class ListaPrefaturamentos
    {
        [JsonProperty("odata.metadata")]
        public Uri OdataMetadata { get; set; }

        [JsonProperty("odata.count")]
        public long OdataCount { get; set; }

        [JsonProperty("value")]
        public Value[] Value { get; set; }
    }

    public partial class Value
    {
        [JsonProperty("numero")]
        public string Numero { get; set; }
    }

    public partial class ListaPrefaturamentos
    {
        public static ListaPrefaturamentos FromJson(string json) => JsonConvert.DeserializeObject<ListaPrefaturamentos>(json, FaturamentoAutomatico.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this ListaPrefaturamentos self) => JsonConvert.SerializeObject(self, FaturamentoAutomatico.Converter.Settings);
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
