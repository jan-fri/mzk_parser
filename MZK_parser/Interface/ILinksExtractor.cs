using HtmlAgilityPack;
using MZK_parser.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MZK_parser.Interface
{
    public interface ILinksExtractor
    {
        void MainPageExtract(IEnumerable<HtmlNode> table, List<BusLink> busLinkList);
    }
}
