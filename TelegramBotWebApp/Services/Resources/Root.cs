using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaerskScheduleBot.Resources
{
    public class Root
    {
        [JsonProperty("vesselSchedules")]
        public List<VesselSchedule> vesselSchedules { get; set; }
    }
}
