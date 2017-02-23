using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Portable;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        public static string State { get; private set; }
        private static PerformanceCounter CPUCount;
        private static PerformanceCounter MemCount;


        public override void Run()
        {
            CloudQueue LoadQueue = CloudConfiguration.GetLoadingQueue();
            CloudQueue CrawlQueue = CloudConfiguration.GetCrawlingQueue();
            CloudQueue StopQueue = CloudConfiguration.GetStopQueue();
            CloudTable Table = CloudConfiguration.GetTable();
            List<string> CNNRules = ProcessRobots("http://www.cnn.com/robots.txt");
            List<string> BleacherReportRules = ProcessRobots("http://www.bleacherreport.com/robots.txt");
            WebCrawler Crawler = new WebCrawler(CNNRules, BleacherReportRules);
            State = "Idle";
            Thread.Sleep(10000);

            CloudQueueMessage stopMessage = StopQueue.GetMessage();

            CPUCount = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            MemCount = new PerformanceCounter("Memory", "Available MBytes");

            while (true)
            {
                while (stopMessage == null)
                {
                    // Get the next message
                    CloudQueueMessage loadMessage = LoadQueue.GetMessage();

                    if (loadMessage != null)
                    {
                        State = "Loading";
                        string message = loadMessage.AsString;
                        Crawler.ProcessURL(message);
                        LoadQueue.DeleteMessage(loadMessage);
                    }
                    else if (State.Equals("Loading") || State.Equals("Crawling")) 
                    {
                        CloudQueueMessage crawlMessage = CrawlQueue.GetMessage();
                        // dequeue crawl message
                        if (crawlMessage != null)
                        {
                            State = "Crawling";
                            Crawler.ProcessURL(crawlMessage.AsString);
                            CrawlQueue.DeleteMessage(crawlMessage);
                        }
                    }
                    stopMessage = StopQueue.GetMessage();
                }
                State = "Idle";
            }
        }

        private List<string> ProcessRobots(string URL)
        {
            WebClient client = new WebClient();
            string file = client.DownloadString(URL);
            string[] lines = file.Split('\n');
            List<string> rules = new List<string>
                (lines.Where(x => x.ToLower().StartsWith("disallow:"))
                .Select(x => x.Substring(x.IndexOf("/"))));
            return rules;
        }


        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}
