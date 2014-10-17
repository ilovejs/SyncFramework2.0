using System;
using Microsoft.Synchronization;
using Microsoft.Synchronization.Data;

namespace Samples.Synchronization
{
    public class SampleSyncOrchestrator : SyncOrchestrator
    {
        public SampleSyncOrchestrator(RelationalSyncProvider localProvider, RelationalSyncProvider remoteProvider)
        {

            this.LocalProvider = localProvider;
            this.RemoteProvider = remoteProvider;
            this.Direction = SyncDirectionOrder.UploadAndDownload;
        }

        public void DisplayStats(SyncOperationStatistics syncStatistics, string syncType)
        {
            Console.WriteLine(String.Empty);
            if (syncType == "initial")
            {
                Console.WriteLine("****** Initial Synchronization ******");
            }
            else if (syncType == "subsequent")
            {
                Console.WriteLine("***** Subsequent Synchronization ****");
            }

            Console.WriteLine("Start Time: " + syncStatistics.SyncStartTime);
            Console.WriteLine("Total Changes Uploaded: " + syncStatistics.UploadChangesTotal);
            Console.WriteLine("Total Changes Downloaded: " + syncStatistics.DownloadChangesTotal);
            Console.WriteLine("Complete Time: " + syncStatistics.SyncEndTime);
            Console.WriteLine(String.Empty);
        }
    }

}