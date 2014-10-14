using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlServerCe;

using Microsoft.Synchronization;
using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServer;
using Microsoft.Synchronization.Data.SqlServerCe;

namespace ExecuteCompactSync
{
    class Program
    {
        static void Main(string[] args)
        {
            var clientConn = new SqlConnection("Data Source=IT34;Initial Catalog=SyncDBClient;User ID=sa;Password=ISAsql07");

            var serverConn = new SqlConnection("Data Source=IT34;Initial Catalog=SyncDBMaster;User ID=sa;Password=ISAsql07");

            // 创建同步代理（sync orhcestrator）
            var syncOrchestrator = new SyncOrchestrator
            {
                LocalProvider = new SqlSyncProvider("ProductsScope", clientConn),
                RemoteProvider = new SqlSyncProvider("ProductsScope", serverConn),
                Direction = SyncDirectionOrder.UploadAndDownload
            };

            // 侦听本地同步提供程序的ApplyChangeFailed事件
            ((SqlSyncProvider)syncOrchestrator.LocalProvider).ApplyChangeFailed += Program_ApplyChangeFailed;

            // 执行同步过程
            SyncOperationStatistics syncStats = syncOrchestrator.Synchronize();

            // 输出统计数据
            Console.WriteLine("Start Time: " + syncStats.SyncStartTime);
            Console.WriteLine("Total Changes Uploaded: " + syncStats.UploadChangesTotal);
            Console.WriteLine("Total Changes Downloaded: " + syncStats.DownloadChangesTotal);
            Console.WriteLine("Complete Time: " + syncStats.SyncEndTime);
            Console.WriteLine(String.Empty);
        }

        static void Program_ApplyChangeFailed(object sender, DbApplyChangeFailedEventArgs e)
        {
            Console.WriteLine(e.Conflict.Type);

            Console.WriteLine(e.Error);
        }

    }
}
