using MZK_parser.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MZK_parser.Models;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.IO;
using Newtonsoft.Json;

namespace MZK_parser.Classes
{
    public class TimeTableParser : ITimeTableExtractor
    {
        public void GetTimeTable(List<BusStopLink> busStopLink, List<BusLink> busLinkList, HtmlWeb htmlWeb, List<string> processedLinks)
        {
            ExtractLinks(busStopLink, busLinkList, htmlWeb, processedLinks);

            JArray busStopArray;

            using (StreamReader file = File.OpenText("stops.json"))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                busStopArray = (JArray)JToken.ReadFrom(reader);

            }

            BusStop busStop = new BusStop();
            busStop.busStopDetail = new List<BusStopDetail>();

            foreach (var stop in busStopArray)
            {

                List<TimeTable> tempTimeTableList = new List<TimeTable>();
                foreach (var tt in stop["timeTableLink"])
                {
                    List<Time> times = GetTimeTableHours((string)tt["link"]);

                    tempTimeTableList.Add(new TimeTable
                    {
                        line = (string)tt["busNo"],
                        time = times
                    });
                }

                BusStopDetail tempBusStopDetail = new BusStopDetail
                {
                    name = (string)stop["stopName"],
                    stopRef = (string)stop["stopRef"],
                    timeTable = tempTimeTableList
                };

                busStop.busStopDetail.Add(tempBusStopDetail);

                var n = busStop.busStopDetail;

                //JObject busStops3 = (JObject)JToken.FromObject(busStop);
                //System.IO.File.WriteAllText("timeTable2.json", busStops3.ToString());
            }

            JObject busStops = (JObject)JToken.FromObject(busStop);
            System.IO.File.WriteAllText("timeTable.json", busStops.ToString());

        }

        private List<Time> GetTimeTableHours(string link2TimeTable)
        {
            string link = "http://www.mzkb-b.internetdsl.pl/" + link2TimeTable;
            HtmlWeb htmlWeb = new HtmlWeb()
            {
                AutoDetectEncoding = false,
                OverrideEncoding = Encoding.GetEncoding("iso-8859-2")
            };

            HtmlDocument doc = htmlWeb.Load(link);

            var table = doc.DocumentNode.SelectNodes("//table").Descendants("tr");

            List<Time> times = new List<Time>();

            foreach (var item in table)
            {
                if (item.InnerText.Contains("DNI ROBOCZE") || item.InnerText.Contains("SOBOTY") || item.InnerText.Contains("NIEDZIELE I ŚWIĘTA"))
                {
                    Time time = new Time();
                    var rows = item.ChildNodes;

                    int rowNo = 0;

                    List<string> hours = new List<string>();


                    foreach (var row in rows)
                    {
                        if (row.InnerText == "DNI ROBOCZE" || row.InnerText == "SOBOTY" || row.InnerText == "NIEDZIELE I ŚWIĘTA")
                        {
                            time.day = row.InnerText;
                        }

                        if (row.InnerText == "&nbsp;\n")
                        {
                            rowNo++;
                            continue;
                        }

                        bool nextHour = false;
                        var values = row.ChildNodes;

                        foreach (var value in values)
                        {
                            if (value.InnerText.Length >= 2)
                            {
                                int minutes;
                                string hour = value.InnerText.Substring(0, 2);
                                if (int.TryParse(hour, out minutes))
                                {
                                    string tempRowNo = rowNo.ToString();
                                    string tempMinutes = minutes.ToString();
                                    if (tempRowNo.Length < 2)
                                        tempRowNo = "0" + tempRowNo;

                                    if (tempMinutes.Length < 2)
                                        tempMinutes = "0" + tempMinutes;


                                    hours.Add(tempRowNo + "." + tempMinutes);
                                    nextHour = true;
                                }
                            }
                        }
                        if (nextHour)
                        {
                            rowNo++;
                            nextHour = false;
                        }

                    }
                    time.hour = hours;
                    times.Add(time);
                }
            }
            return times;
        }

        //extracts bus stops for each bus line
        public void ExtractLinks(List<BusStopLink> busStopLink, List<BusLink> busLinkList, HtmlWeb htmlWeb, List<string> processedLinks)
        {
            foreach (var bLink in busLinkList)
            {
                if (bLink.busNumber == null)
                    continue;

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

                            if (processedLinks.Contains(extractedStopLink))
                            {
                                break;
                            }
                            else
                            {
                                processedLinks.Add(extractedStopLink);
                            }

                            tempStopLink.timeTableLink = new List<BusNo2Link>(ExtractTimeTableLinks(extractedStopLink));

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
            System.IO.File.WriteAllText("stops.json", stops.ToString());
        }

        private List<BusNo2Link> ExtractTimeTableLinks(string extractedStopLink)
        {
            HtmlWeb htmlWeb = new HtmlWeb();
            string httpLink = "http://www.mzkb-b.internetdsl.pl/" + extractedStopLink;
            HtmlDocument htmlDocument = htmlWeb.Load(httpLink);
            IEnumerable<HtmlNode> links = htmlDocument.DocumentNode.SelectNodes("//a");

            List<BusNo2Link> timeTableLinks = new List<BusNo2Link>();
            foreach (var link in links)
            {
                timeTableLinks.Add(new BusNo2Link
                {
                    busNo = link.InnerText,
                    link = link.Attributes["href"].Value
                });

            }
            //Console.WriteLine(extractedStopLink);
            return timeTableLinks;
        }
    }
}
