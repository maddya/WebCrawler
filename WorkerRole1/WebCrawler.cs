using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Portable;

namespace WorkerRole1
{
    class WebCrawler
    {
        private CloudQueue LoadQueue { get; set; }
        private CloudQueue CrawlQueue { get; set; }
        private CloudTable Table { get; set; }
        private List<string> CNNRules { get; set; }
        private List<string> BleacherReportRules { get; set; }
        private HashSet<string> VisitedLinks { get; set; }
        private DateTime OldestAllowed { get; set; }
        private List<string>  BadExtensions { get; set; }

        public WebCrawler(List<string> CNNRules, List<string> BleacherReportRules)
        {
            LoadQueue = CloudConfiguration.GetLoadingQueue();
            CrawlQueue = CloudConfiguration.GetCrawlingQueue();
            Table = CloudConfiguration.GetTable();
            this.CNNRules = CNNRules;
            this.BleacherReportRules = BleacherReportRules;
            this.VisitedLinks = new HashSet<string>();
            OldestAllowed = new DateTime(2016, 12, 1);
            BadExtensions = new List<string> { ".jpg" };
        }

        public void ProcessURL(string URL)
        {
            URL = URL.ToLower();
            VisitedLinks.Add(URL);
            if (URL.EndsWith("txt"))
            {
                ProcessTxt(URL);
            }
            else if (URL.EndsWith("xml"))
            {
                ProcessXML(URL);
            }
            else
            {
                ProcessHTML(URL);
            }
        }

        private bool CheckIfAllowed(string URL)
        {
            if (URL.Contains("cnn.com"))
            {
                foreach (string s in CNNRules)
                {
                    if (URL.Contains($"cnn.com{s}"))
                    {
                        return false;
                    }
                }
            }
            else if (URL.Contains("bleacherreport.com"))
            {
                foreach (string s in BleacherReportRules)
                {
                    if (URL.Contains($"bleacerreport.com{s}"))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void ProcessXML(string URL)
        {
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(URL);

            httpRequest.Timeout = 10000;     // 10 secs
            httpRequest.UserAgent = "Code Sample Web Client";

            HttpWebResponse webResponse = (HttpWebResponse)httpRequest.GetResponse();
            var stream = new StreamReader(webResponse.GetResponseStream());

            XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
            xmlDoc.Load(stream);

            // Get elements
            XmlNodeList elements = xmlDoc.GetElementsByTagName("sitemap");
            if (elements.Count == 0) {
                elements = xmlDoc.GetElementsByTagName("url");
            }

            for (int i = 0; i < elements.Count; i++)
            {
                var link = elements[i].ChildNodes[0].InnerText;
                bool correctDate = true;
                if (elements[i].LastChild.InnerText != link)
                {
                    var date = elements[i].ChildNodes[1].InnerText;
                    correctDate = CheckLinkIsRecent(link, date);
                }
                if (CheckLinkDomain(link) && CheckLinkIsCorrectType(link) && CheckIfAllowed(link) && correctDate)
                {
                    CloudQueueMessage linkMessage = new CloudQueueMessage(link);
                    if (link.EndsWith("xml"))
                    {
                        LoadQueue.AddMessage(linkMessage);
                    }
                    else
                    {
                        CrawlQueue.AddMessage(linkMessage);
                    }
                }
            }
        }

        private void ProcessHTML(string URL)
        {
            //HtmlWeb hw = new HtmlWeb();
            //HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            //doc = hw.Load(tb_url.Text);
            //foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
            //{
            //    // Get the value of the HREF attribute
            //    string hrefValue = link.GetAttributeValue("href", string.Empty);
            //    cbl_items.Items.Add(hrefValue);
            //}
           
            
            
            
            //WebRequest myWebRequest = WebRequest.Create(URL);
            //WebResponse myWebResponse = myWebRequest.GetResponse(); // Returns a response from an Internet resource

            //Stream streamResponse = myWebResponse.GetResponseStream(); // return the data stream from the internet and save it in the stream

            //StreamReader reader = new StreamReader(streamResponse); // reads the data stream
            //string content = reader.ReadToEnd(); // reads it to the end

            ////Regex regexLink = new Regex("(?<=<a\\s*?href=(?:'|\"))[^'\"]*?(?=(?:'|\"))");
            ////foreach (var match in regexLink.Matches(content))
            ////{
            ////    CloudQueueMessage message = new CloudQueueMessage(match.ToString());
            ////    Queue.AddMessage(message);
            ////}
            //streamResponse.Close();
            //reader.Close();
            //myWebResponse.Close();
        }

        private void ProcessTxt(string URL)
        {
            WebClient client = new WebClient();
            string file = client.DownloadString(URL);
            string[] lines = file.Split('\n');
            HashSet<string> links = new HashSet<string>
                (lines.Where(x => x.ToLower().StartsWith("sitemap:"))
                .Select(x => x.Substring(x.IndexOf("http"))));
            foreach (string link in links)
            {
                if (CheckLinkDomain(link))
                {
                    CloudQueueMessage message = new CloudQueueMessage(link);
                    LoadQueue.AddMessage(message);
                }
            }
        }

        private bool CheckLinkDomain(string URL)
        {
            return (URL.Contains("cnn.com") || URL.Contains("bleacherreport.com"));
        }

        private bool CheckLinkIsRecent(string link, string date)
        {
            DateTime temp;
            if (DateTime.TryParse(date, out temp)) {
                return temp >= OldestAllowed;
            }
            else
            {
                Debug.Print(date);
                return true;
            }
            
        }

        private bool CheckLinkIsCorrectType(string URL)
        {
            foreach(string s in BadExtensions)
            {
                if (URL.EndsWith(s))
                {
                    return false;
                }
            }
            return true;
        }

    }
}
