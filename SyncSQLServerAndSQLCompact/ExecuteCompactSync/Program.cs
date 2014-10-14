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
            // 创建到SyncCompactDB database的连接
            SqlCeConnection clientConn = new SqlCeConnection(@"Data Source='E:\SyncCompactDB.sdf'");

            // 创建到SyncDB服务器数据库的连接
//            SqlConnection serverConn = new SqlConnection("Data Source=IT34; Initial Catalog=SyncDBMaster; Integrated Security=True");
            SqlConnection serverConn = new SqlConnection("Data Source=IT34;Initial Catalog=SyncDBMaster;User ID=sa;Password=ISAsql07");

            // 创建同步代理（sync orhcestrator）
            SyncOrchestrator syncOrchestrator = new SyncOrchestrator();

            // 设置同步代理的本地同步提供程序为SyncCompactDB compact database的ProductsScope
            syncOrchestrator.LocalProvider = new SqlCeSyncProvider("ProductsScope", clientConn);

            // 设置同步代理的远程同步提供程序为SyncDB server database的ProductsScope
            syncOrchestrator.RemoteProvider = new SqlSyncProvider("ProductsScope", serverConn);

            // 设置同步会话的同步方向为Upload和Download
            syncOrchestrator.Direction = SyncDirectionOrder.UploadAndDownload;

            // 侦听本地同步提供程序的ApplyChangeFailed事件
            ((SqlCeSyncProvider)syncOrchestrator.LocalProvider).ApplyChangeFailed += new EventHandler<DbApplyChangeFailedEventArgs>(Program_ApplyChangeFailed);

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
            // 显示冲突类型
            Console.WriteLine(e.Conflict.Type);

            // 显示错误信息
            Console.WriteLine(e.Error);
        }

    }
}
