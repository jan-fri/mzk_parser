using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MZK_parser.Models
{
    public class BusStopLink
    {
        public string stopName;
        public string stopRef;
        public string stopLink;
        public List<BusNo2Link> timeTableLink;
    }
}
