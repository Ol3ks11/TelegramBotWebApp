using Newtonsoft.Json;

namespace TelegramBotWebApp.Services.Resources
{
    public class Port
    {
        [JsonProperty("vessels")]
        public List<Ship> Vessels { get; set; }

        [JsonProperty("portGeoId")]
        public string GeoId { get; set; }

        [JsonProperty("port")]
        public string portName { get; set; }

        [JsonProperty("terminal")]
        public string terminal { get; set; }

        [JsonProperty("arrival")]
        public DateTime arrival { get; set; }

        [JsonProperty("departure")]
        public DateTime departure { get; set; }

        [JsonProperty("countryName")]
        public string countryName { get; set; }

        public string emoji { get; set; }

    }
}