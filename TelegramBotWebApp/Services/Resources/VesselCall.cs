using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBotWebApp.Services.Resources
{
    public class VesselCall
    {
        public Facility facility { get; set; }
        public List<CallSchedule> callSchedules { get; set; }

        //public Transport transport { get; set; }

    }
}
