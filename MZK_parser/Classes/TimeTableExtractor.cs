using MZK_parser.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MZK_parser.Models;

namespace MZK_parser
{
    public class TimeTableExtractor 
    {
        ITimeTableExtractor _timeTableExtractor = null;
        public TimeTableExtractor(ITimeTableExtractor timeTableExtractor, List<BusStopLink> busStopLink, List<BusLink> busLinkList, HtmlWeb htmlWeb, List<string> processedLinks)
        {
            _timeTableExtractor = timeTableExtractor;
            _timeTableExtractor.GetTimeTable(busStopLink, busLinkList, htmlWeb, processedLinks);
        }
    }
}
