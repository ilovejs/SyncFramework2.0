using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Data.SqlClient;
using System.Data.SqlServerCe;

using Microsoft.Synchronization;
using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServer;
using Microsoft.Synchronization.Data.SqlServerCe;

namespace ExecuteSecondCompactSync
{
    class Program
    {
        static void Main(string[] args)
        {
            /**
             * compact version 2 upload to server side
             */
            
            //Stuff in Snapshot of second database
            var clientConn = new SqlCeConnection(@"Data Source='C:\SyncSQLServerAndSQLCompact\SyncCompactDB2.sdf'");

            //create connection to the server database
            var serverConn = new SqlConnection("Data Source=localhost; Initial Catalog=SyncDB; Integrated Security=True");

            // create a sync orchestrator
            var syncOrchestrator = new SyncOrchestrator();

            // set the local provider to a CE sync provider associated with the
            // ProductsScope in the Sync Compact DB 2 database
            syncOrchestrator.LocalProvider = new SqlCeSyncProvider("ProductsScope", clientConn);

            // set the remote provider to a server sync provider associated with the
            // ProductsScope in the Sync DB server database
            syncOrchestrator.RemoteProvider = new SqlSyncProvider("ProductsScope", serverConn);

            // set the diretion to Upload and Download
            syncOrchestrator.Direction = SyncDirectionOrder.UploadAndDownload;

            // subscribe for errors that occur when applying changes to the client
            ((SqlCeSyncProvider)syncOrchestrator.LocalProvider).ApplyChangeFailed += new EventHandler<DbApplyChangeFailedEventArgs>(Program_ApplyChangeFailed);

            // execute the synchronization process
            SyncOperationStatistics syncStats = syncOrchestrator.Synchronize();

            //print sync statistics
            Console.WriteLine("Start Time: " + syncStats.SyncStartTime);
            Console.WriteLine("Total Changes Uploaded: " + syncStats.UploadChangesTotal);
            Console.WriteLine("Total Changes Downloaded: " + syncStats.DownloadChangesTotal);
            Console.WriteLine("Complete Time: " + syncStats.SyncEndTime);
            Console.WriteLine(String.Empty);
        }

        static void Program_ApplyChangeFailed(object sender, DbApplyChangeFailedEventArgs e)
        {
            // display conflict type
            Console.WriteLine(e.Conflict.Type);

            // display error message 
            Console.WriteLine(e.Error);
        }
    }
}
