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
    public class BusLinksExtractor
    {
        ILinksExtractor _linksExtractor = null;
        public BusLinksExtractor(ILinksExtractor linksExtractor, IEnumerable<HtmlNode> table, List<BusLink> busLinkList)
        {
            _linksExtractor = linksExtractor;
            _linksExtractor.MainPageExtract(table, busLinkList);
        }
    }
}
