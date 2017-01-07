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
    }

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
            var table = htmlDocument.DocumentNode.SelectNodes("//table").Descendants("tr");

            List<BusLink> busLinkList = new List<BusLink>();

            //extract content from page
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
            System.IO.File.WriteAllText("buss2.json", busLinks.ToString());



            List<BusStopLink> busStopLink = new List<BusStopLink>();

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

                            tempStopLink = new BusStopLink
                            {
                                stopRef = sLink.InnerText.Split('(', ')')[1],
                                stopLink = sLink.Attributes["href"].Value,
                                stopName = stopName
                            };

                            bool busStopExists = false;

                            foreach (var stopLink in busStopLink)
                            {
                                if (stopLink.stopRef == tempStopLink.stopRef && stopLink.stopLink == tempStopLink.stopLink
                                    && stopLink.stopName == tempStopLink.stopName)
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
            System.IO.File.WriteAllText("stops2.json", stops.ToString());

        }
    }

}




