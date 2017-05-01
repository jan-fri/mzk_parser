using HtmlAgilityPack;
using MZK_parser.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MZK_parser.Interface
{
    public interface ITimeTableExtractor
    {
        void GetTimeTable(List<BusStopLink> busStopLink, List<BusLink> busLinkList, HtmlWeb htmlWeb, List<string> processedLinks);
    }
}
