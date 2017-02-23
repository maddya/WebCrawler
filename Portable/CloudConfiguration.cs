using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;

namespace Portable
{

    public static class CloudConfiguration
    {
        private static readonly CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                    ConfigurationManager.AppSettings["StorageConnectionString"]);

        public static CloudQueue GetLoadingQueue()
        {
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("sitemapqueue");
            queue.CreateIfNotExists();
            return queue;
        }

        public static CloudQueue GetCrawlingQueue()
        {
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("linkqueue");
            queue.CreateIfNotExists();
            return queue;
        }

        public static CloudQueue GetStopQueue()
        {
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("stopqueue");
            queue.CreateIfNotExists();
            return queue;
        }

        public static CloudTable GetTable()
        {
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("sitedatatable");
            table.CreateIfNotExists();
            return table;
        }
        
    }
}
