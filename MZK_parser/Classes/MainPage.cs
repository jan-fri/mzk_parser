using MZK_parser.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using MZK_parser.Models;
using Newtonsoft.Json.Linq;

namespace MZK_parser
{
    public class MainPage : ILinksExtractor
    {
        //extracts bus line numbers, corresponding links to bus stops and route direction 
        public void MainPageExtract(IEnumerable<HtmlNode> table, List<BusLink> busLinkList)
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
    }
}
