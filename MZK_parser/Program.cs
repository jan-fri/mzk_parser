using HtmlAgilityPack;
using MZK_parser.Classes;
using MZK_parser.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace MZK_parser
{
    class Program
    {
        static void Main(string[] args)
        {

            //html agile config
            HtmlWeb htmlWeb = new HtmlWeb()
            {
                AutoDetectEncoding = false,
                OverrideEncoding = Encoding.GetEncoding("iso-8859-2")
            };
            //read html page
            HtmlDocument htmlDocument = htmlWeb.Load("http://www.mzkb-b.internetdsl.pl/linie_r.htm#1");
            IEnumerable<HtmlNode> table = htmlDocument.DocumentNode.SelectNodes("//table").Descendants("tr");

            List<BusLink> busLinkList = new List<BusLink>();
            List<BusStopLink> busStopLink = new List<BusStopLink>();
            List<string> processedLinks = new List<string>();

            //extract content from main page
            MainPage mainPage = new MainPage();
            BusLinksExtractor busLinksExtractor = new BusLinksExtractor(mainPage, table, busLinkList);

            //Extract time tables from MZK Bielsko website
            TimeTableParser timeTableParser = new TimeTableParser();
            TimeTableExtractor timeTableExtractor = new TimeTableExtractor(timeTableParser, busStopLink, busLinkList, htmlWeb, processedLinks);

            //Extract busStops for Neo4j graph database
            BusStopsParser busStopsParser = new BusStopsParser();
            BusStopsExtractor busStopsExtractor = new BusStopsExtractor(busStopsParser, busStopLink, busLinkList,htmlWeb, processedLinks);
        }        
    }
}