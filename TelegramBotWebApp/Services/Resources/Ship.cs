using Newtonsoft.Json;

namespace TelegramBotWebApp.Services.Resources
{
    public class Ship
    {
        [JsonProperty("vessel")]
        public string ShipName { get; set; }

        [JsonProperty("vesselCode")]
        public string ShipCode { get; set; }

        [JsonProperty("ports")]
        public List<Port> Ports { get; set; }

        [JsonProperty("arrival")]
        public DateTime Arrival { get; set; }

        [JsonProperty("departure")]
        public DateTime Departure { get; set; }
    }
}