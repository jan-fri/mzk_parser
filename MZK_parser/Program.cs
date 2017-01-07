using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace MZK_parser
{
    public class BusLink
    {
        public string busNumber;
        public string linktoBus;
        public string direction;
    }

    public class BusStopLink
    {
        public string stopName;
        public string stopRef;
        public string stopLink;
        public List<BusNo2Stop> timeTableLink;
    }

    public class BusNo2Stop
    {
        public string link;
        public string busNo;
    }

    class Program
    {
        private static void MainPageExtract(IEnumerable<HtmlNode> table, List<BusLink> busLinkList)
        {
            foreach (var item in table)
            {
                IEnumerable<HtmlNode> busNo = item.Descendants("td");

                IEnumerable<HtmlNode> links = item.Descendants("a").Where(x => x.Attributes.Contains("href"));
                foreach (var link in links)
                {
                    BusLink tempLinkList = new BusLink();

                    //take only emements with bus number
                    foreach (var no in busNo)
                    {
                        int digit = 0;

                        int.TryParse(no.InnerText, out digit);
                        Console.WriteLine(no.InnerText);
                        if (digit != 0)
                        {
                            tempLinkList.busNumber = digit.ToString();
                            tempLinkList.linktoBus = link.Attributes["href"].Value;
                            tempLinkList.direction = link.InnerText.Replace("\n", "");
                        }

                        string plainBusNo = no.InnerText.Replace("\n", "");

                        if (plainBusNo == "D" || plainBusNo == "N1" || plainBusNo == "N2")
                        {
                            tempLinkList.busNumber = plainBusNo;
                            tempLinkList.linktoBus = link.Attributes["href"].Value;
                            tempLinkList.direction = link.InnerText.Replace("\n", "");
                        }
                    }

                    bool busExist = false;

                    foreach (var busLink in busLinkList)
                    {
                        if (busLink.busNumber == tempLinkList.busNumber
                            && busLink.direction == tempLinkList.direction
                            && busLink.linktoBus == tempLinkList.linktoBus)
                        {
                            busExist = true;
                        }
                    }

                    if (!busExist)
                    {
                        busLinkList.Add(new BusLink
                        {
                            busNumber = tempLinkList.busNumber,
                            direction = tempLinkList.direction,
                            linktoBus = tempLinkList.linktoBus,
                        });
                    }
                }
            }

            JArray busLinks = (JArray)JToken.FromObject(busLinkList);
            System.IO.File.WriteAllText("buss4.json", busLinks.ToString());
        }


        private static void ExtractLinks(List<BusStopLink> busStopLink, List<BusLink> busLinkList, HtmlWeb htmlWeb, List<string> processsedLinks)
        {
            foreach (var bLink in busLinkList)
            {
                if (bLink.busNumber == null)
                    break;

                string link = "http://www.mzkb-b.internetdsl.pl/" + bLink.linktoBus;
                HtmlDocument doc = htmlWeb.Load(link);

                var tab = doc.DocumentNode.SelectNodes("//table").Descendants("tr");

                

                foreach (var stop in tab)
                {
                    IEnumerable<HtmlNode> stoplinks = stop.Descendants("a").Where(x => x.Attributes.Contains("href"));
                    foreach (var sLink in stoplinks)
                    {
                        BusStopLink tempStopLink;
                        if (sLink.InnerText.Contains('('))
                        {
                            string stopName = Regex.Replace(sLink.InnerText.Replace("&nbsp;", ""), "(\\(.*\\))", "");

                            string stopNo = sLink.Attributes["href"].Value.Split('_', '_')[1];
                            string extractedStopLink = "p_" + stopNo + "_l.htm";

                            tempStopLink = new BusStopLink
                            {
                                stopRef = sLink.InnerText.Split('(', ')')[1],
                                stopLink = extractedStopLink,
                                stopName = stopName
                            };

                            if (processsedLinks.Contains(extractedStopLink))
                            {
                                break;
                            }
                            else
                            {
                                processsedLinks.Add(extractedStopLink);
                            }

                            tempStopLink.timeTableLink = new List<BusNo2Stop>(ExtractTimeTableLinks(extractedStopLink));

                            bool busStopExists = false;

                            foreach (var stopLink in busStopLink)
                            {
                                if (stopLink.stopRef == tempStopLink.stopRef && stopLink.stopName == tempStopLink.stopName)
                                {
                                    busStopExists = true;
                                }
                            }

                            if (!busStopExists)
                            {
                                busStopLink.Add(tempStopLink);
                            }
                        }
                    }
                }
            }

            JArray stops = (JArray)JToken.FromObject(busStopLink);
            System.IO.File.WriteAllText("stops7.json", stops.ToString());
        }

        private static List<BusNo2Stop> ExtractTimeTableLinks(string extractedStopLink)
        {
            HtmlWeb htmlWeb = new HtmlWeb();
            string httpLink = "http://www.mzkb-b.internetdsl.pl/" + extractedStopLink;
            HtmlDocument htmlDocument = htmlWeb.Load(httpLink);
            IEnumerable<HtmlNode> links = htmlDocument.DocumentNode.SelectNodes("//a");

            List<BusNo2Stop> timeTableLinks = new List<BusNo2Stop>();
            foreach (var link in links)
            {
                timeTableLinks.Add(new BusNo2Stop
                {
                    busNo = link.InnerText,
                    link = link.Attributes["href"].Value
                });
                
            }
            //Console.WriteLine(extractedStopLink);
            return timeTableLinks;

        }

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
            
            //extract content from main page
            MainPageExtract(table, busLinkList);                       

            List<BusStopLink> busStopLink = new List<BusStopLink>();
            List<string> processedLinks = new List<string>();

            ExtractLinks(busStopLink, busLinkList, htmlWeb, processedLinks);       
        }


    }

}




