using Newtonsoft.Json;

namespace TelegramBotWebApp.Services.Resources
{
    public class Ship
    {
        [JsonProperty("name")]
        public string ShipName { get; set; }

        [JsonProperty("code")]
        public string ShipCode { get; set; }

        [JsonProperty("ports")]
        public List<Port> Ports { get; set; }
    }
}