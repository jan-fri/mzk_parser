using HtmlAgilityPack;
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

            //extract content from main page
            MainPageExtract(table, busLinkList);

            List<BusStopLink> busStopLink = new List<BusStopLink>();
            List<string> processedLinks = new List<string>();

           // ExtractLinks(busStopLink, busLinkList, htmlWeb, processedLinks);     
            GetBusStopList(busStopLink, busLinkList, htmlWeb, processedLinks);

          //  GetTimeTable();


        }
        //extracts bus line numbers, corresponding links to bus stops and route direction 
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
            System.IO.File.WriteAllText("busLinks.json", busLinks.ToString());
        }
        //extracts bus stops for each bus line
        private static void ExtractLinks(List<BusStopLink> busStopLink, List<BusLink> busLinkList, HtmlWeb htmlWeb, List<string> processedLinks)
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
        private static List<BusNo2Link> ExtractTimeTableLinks(string extractedStopLink)
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

        //extracts bus stop list for each bus line number - used in graph database
        private static void GetBusStopList(List<BusStopLink> busStopLink, List<BusLink> busLinkList, HtmlWeb htmlWeb, List<string> processedLinks)
        {
            List<string> processedStops = new List<string>();
            Dictionary<string, List<string>> processedRelations = new Dictionary<string, List<string>>();

            System.IO.StreamWriter busStopsFile = new System.IO.StreamWriter("busStops.csv");
            System.IO.StreamWriter createRelationsFile = new System.IO.StreamWriter("createRelations.csv");
            System.IO.StreamWriter mergeRelationsFile = new System.IO.StreamWriter("mergeRelations.csv");

            busStopsFile.WriteLine("ref,name,city");
            createRelationsFile.WriteLine("busA,line,busB");
            mergeRelationsFile.WriteLine("busA,line,busB");

            foreach (var bLink in busLinkList)
            {
                if (bLink.busNumber == null)
                    continue; // break;

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
                                break;
                            else
                                processedLinks.Add(extractedStopLink);

                            busStopLink.Add(tempStopLink);
                        }
                    }
                }

                processedLinks.Clear();

                int index = 0;
                StringBuilder busStopBuilder = new StringBuilder();
                StringBuilder createRelationsBuilder = new StringBuilder();
                StringBuilder mergeRelationsBuilder = new StringBuilder();

                foreach (var busStop in busStopLink)
                {
                    if (!processedStops.Contains(busStop.stopRef))
                    {
                        busStopBuilder.Append(busStop.stopRef + "," + busStop.stopName + "," + "Bielsko Biała" + "\n");
                        processedStops.Add(busStop.stopRef);
                    }

                    if (index < busStopLink.Count - 1)
                    {
                        bool relationCreated = false;
                        bool relationExists = false;

                        if (processedRelations.ContainsKey(busStop.stopRef))
                        {
                            relationExists = true;
                            foreach (var rel in processedRelations[busStop.stopRef])
                            {
                                if (rel == busStopLink[index + 1].stopRef)
                                {
                                    mergeRelationsBuilder.Append(busStop.stopRef + "," + bLink.busNumber + "," + busStopLink[index + 1].stopRef + "\n");
                                    relationCreated = true;
                                    break;
                                }
                            }
                            processedRelations[busStop.stopRef].Add(busStopLink[index + 1].stopRef);                            
                        }

                        if (!relationCreated)
                        {
                            createRelationsBuilder.Append(busStop.stopRef + "," + bLink.busNumber + "," + busStopLink[index + 1].stopRef + "\n");
                            if (!relationExists)
                            {
                                processedRelations.Add(busStop.stopRef, new List<string>() { busStopLink[index + 1].stopRef });
                            }
                        }
                    }
                    index++;
                }

                busStopLink.Clear();

                busStopsFile.WriteLine(busStopBuilder);
                createRelationsFile.WriteLine(createRelationsBuilder);
                mergeRelationsFile.WriteLine(mergeRelationsBuilder);
            }
            busStopsFile.Close();
            createRelationsFile.Close();
            mergeRelationsFile.Close();
        }
        private static void GetTimeTable()
        {

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
        private static List<Time> GetTimeTableHours(string link2TimeTable)
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


    }
}






//private static void GetBusStopList(List<BusStopLink> busStopLink, List<BusLink> busLinkList, HtmlWeb htmlWeb, List<string> processedLinks)
//{
//    List<string> processedStops = new List<string>();

//    System.IO.StreamWriter busStopsFile = new System.IO.StreamWriter("busStops.csv");
//    System.IO.StreamWriter relationsFile = new System.IO.StreamWriter("relations.csv");

//    busStopsFile.WriteLine("ref,name");
//    relationsFile.WriteLine("busA,line,busB");

//    foreach (var bLink in busLinkList)
//    {
//        if (bLink.busNumber == null)
//            continue; // break;

//        string link = "http://www.mzkb-b.internetdsl.pl/" + bLink.linktoBus;
//        HtmlDocument doc = htmlWeb.Load(link);

//        var tab = doc.DocumentNode.SelectNodes("//table").Descendants("tr");

//        foreach (var stop in tab)
//        {
//            IEnumerable<HtmlNode> stoplinks = stop.Descendants("a").Where(x => x.Attributes.Contains("href"));
//            foreach (var sLink in stoplinks)
//            {
//                BusStopLink tempStopLink;
//                if (sLink.InnerText.Contains('('))
//                {
//                    string stopName = Regex.Replace(sLink.InnerText.Replace("&nbsp;", ""), "(\\(.*\\))", "");

//                    string stopNo = sLink.Attributes["href"].Value.Split('_', '_')[1];
//                    string extractedStopLink = "p_" + stopNo + "_l.htm";

//                    tempStopLink = new BusStopLink
//                    {
//                        stopRef = sLink.InnerText.Split('(', ')')[1],
//                        stopLink = extractedStopLink,
//                        stopName = stopName
//                    };

//                    if (processedLinks.Contains(extractedStopLink))
//                        break;
//                    else
//                        processedLinks.Add(extractedStopLink);

//                    busStopLink.Add(tempStopLink);
//                }
//            }
//        }

//        processedLinks.Clear();

//        int index = 0;
//        StringBuilder busStopBuilder = new StringBuilder();
//        StringBuilder relationsBuilder = new StringBuilder();

//        foreach (var busStop in busStopLink)
//        {
//            if (!processedStops.Contains(busStop.stopRef))
//            {
//                busStopBuilder.Append(busStop.stopRef + "," + busStop.stopName + "\n");
//                processedStops.Add(busStop.stopRef);
//            }

//            if (index < busStopLink.Count - 1)
//            {
//                relationsBuilder.Append(busStop.stopRef + "," + bLink.busNumber + "," + busStopLink[index + 1].stopRef + "\n");
//            }

//            index++;
//        }

//        busStopLink.Clear();

//        busStopsFile.WriteLine(busStopBuilder);
//        relationsFile.WriteLine(relationsBuilder);
//    }
//    busStopsFile.Close();
//    relationsFile.Close();
//}
