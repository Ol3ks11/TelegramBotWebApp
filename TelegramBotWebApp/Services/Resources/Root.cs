using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBotWebApp.Services.Resources
{
    public class Root
    {
        [JsonProperty("vessels")]
        public List<Ship> Vessels { get; set; }

        [JsonProperty("ports")]
        public List<Port> Ports { get; set; }
    }

}
