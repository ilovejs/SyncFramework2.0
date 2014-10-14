using System.Data.SqlClient;
using System.Data.SqlServerCe;

using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServer;
using Microsoft.Synchronization.Data.SqlServerCe;

namespace ProvisionClient
{
    class Program
    {
        static void Main(string[] args)
        {
            // 创建到SyncCompactDB数据库的连接
            SqlCeConnection clientConn = new SqlCeConnection(@"Data Source='D:\Sync Framework\CompactDB\SyncCompactDB.sdf'");

            // 创建到SyncDB服务器数据库的连接
            SqlConnection serverConn = new SqlConnection("Data Source=localhost; Initial Catalog=SyncDB; Integrated Security=True");

            // 从SyncDB服务器数据库中获取ProductsScope同步作用域
            DbSyncScopeDescription scopeDesc = SqlSyncDescriptionBuilder.GetDescriptionForScope("ProductsScope", serverConn);

            // 基于ProductsScope同步作用域创建CE设置对象
            SqlCeSyncScopeProvisioning clientProvision = new SqlCeSyncScopeProvisioning(clientConn, scopeDesc);

            // 开始设置过程
            clientProvision.Apply();
        }
    }
}
