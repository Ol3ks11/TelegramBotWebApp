using Newtonsoft.Json;

namespace TelegramBotWebApp.Services.Resources
{
    public class Port
    {
        [JsonProperty("countryName")]
        public string countryName { get; set; }

        [JsonProperty("locationName")]
        public string locationName { get; set; }

        [JsonProperty("geoId")]
        public string GeoId { get; set; }

        [JsonProperty("port")]
        public string portName { get; set; }

        [JsonProperty("terminal")]
        public string terminal { get; set; }

        [JsonProperty("arrival")]
        public DateTime arrival { get; set; }

        [JsonProperty("departure")]
        public DateTime departure { get; set; }
    }
}