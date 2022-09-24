using Newtonsoft.Json;

namespace TelegramBotWebApp.Services.Resources
{
    public class Port
    {
        [JsonProperty("port")]
        public string port { get; set; }

        [JsonProperty("terminal")]
        public string terminal { get; set; }

        [JsonProperty("arrival")]
        public DateTime arrival { get; set; }

        [JsonProperty("departure")]
        public DateTime departure { get; set; }
    }
}