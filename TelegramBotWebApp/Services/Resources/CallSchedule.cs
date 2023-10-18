using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


    public class CallSchedule
    {
        public string transportEventTypeCode { get; set; } // ARRI or DEPA
        public string eventClassifierCode { get; set; } // PLN for planned, ACT for actual, EST for estimated
        public DateTime classifierDateTime { get; set; } // example: 2020-03-20T10:00:00

    }

