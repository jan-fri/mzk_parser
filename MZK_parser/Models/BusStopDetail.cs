﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MZK_parser.Models
{
    public class BusStopDetail
    {
        public string stopRef;
        public string name;
        public List<TimeTable> timeTable;
    }
}
