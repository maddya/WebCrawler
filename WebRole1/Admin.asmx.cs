using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Services;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Portable;

namespace WebRole1
{
    /// <summary>
    /// Summary description for Admin
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class Admin : System.Web.Services.WebService
    {
        /**
         * stuff for the dashboard:
         * State of each worker role web crawler (loading, crawling, idle)
         * Machine counters (CPU Utilization%, RAM available)
         * #URLs crawled
         * Last 10 URLs crawled
         * Size of queue (#urls left in pipeline to be crawled)
         * Size of index (Table storage with our crawled data)
         * Any errors and their URLs 
         */


        private CloudQueue LoadQueue { get; set; }
        private CloudQueue CrawlQueue { get; set; }
        private CloudQueue StopQueue { get; set; }
        private CloudTable Table { get; set; }

        [WebMethod]
        public string StartCrawler()
        {
            StopQueue = CloudConfiguration.GetStopQueue();
            CloudQueueMessage stopMessage= StopQueue.GetMessage();
            while (stopMessage != null)
            {
                StopQueue.DeleteMessage(stopMessage);
                stopMessage = StopQueue.GetMessage();
            }

            LoadQueue = CloudConfiguration.GetLoadingQueue();

            //Add message
            CloudQueueMessage cnnRobots = new CloudQueueMessage("http://www.cnn.com/robots.txt");
            LoadQueue.AddMessage(cnnRobots);

            CloudQueueMessage bleacherReportRobots = new CloudQueueMessage("http://www.bleacherreport.com/robots.txt");
            LoadQueue.AddMessage(bleacherReportRobots);

            return LoadQueue.Name + " " + cnnRobots.AsString + " " + bleacherReportRobots.AsString;
        }

        [WebMethod]
        public string StopCrawler()
        {
            StopQueue = CloudConfiguration.GetStopQueue();
            CloudQueueMessage stopSignal = new CloudQueueMessage("stop");
            StopQueue.AddMessage(stopSignal);
            return StopQueue.Name + " " + stopSignal.AsString;

        }

        [WebMethod]
        public string GetPageTitle(string URL)
        {
            // Retrieve data from index (get page title for specific URL)
            Table = CloudConfiguration.GetTable();
            Table.CreateIfNotExists();

            string encodedURL = Base64.Base64Encode(URL);

            TableQuery<URLEntity> rangeQuery = new TableQuery<URLEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, encodedURL));

            var data = Table.ExecuteQuery(rangeQuery);

            if (data != null)
            {
                string title = data.First().Title;
                return title;
            }
            else
            {
                return $"URl {URL} not found in table storage";
            }
        }

        [WebMethod]
        public void ClearEverything()
        {
            Table.DeleteIfExists();
            LoadQueue.DeleteIfExists();
            CrawlQueue.DeleteIfExists();
            StopQueue.DeleteIfExists();
        }

    }
}
