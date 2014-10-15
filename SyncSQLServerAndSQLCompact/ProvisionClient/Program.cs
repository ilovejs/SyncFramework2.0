using System.Data.SqlClient;
using System.Data.SqlServerCe;

using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServer;

namespace ProvisionClient
{
    class Program
    {
        static void Main(string[] args)
        {
            // 创建到SyncCompactDB数据库的连接
            var clientConn = new SqlConnection("Data Source=IT34;Initial Catalog=SyncDBClient;User ID=sa;Password=ISAsql07");

            // 创建到SyncDB服务器数据库的连接
            var serverConn = new SqlConnection("Data Source=IT34;Initial Catalog=SyncDBMaster;User ID=sa;Password=ISAsql07");

            // 从SyncDB服务器数据库中获取ProductsScope同步作用域
            DbSyncScopeDescription scopeDesc = SqlSyncDescriptionBuilder.GetDescriptionForScope("ProductsScope", serverConn);
//            DbSyncScopeDescription scopeDesc = SqlSyncDescriptionBuilder.GetDescriptionForScope()

            // 基于ProductsScope同步作用域创建CE设置对象
            var clientProvision = new SqlSyncScopeProvisioning();
           

            // 开始设置过程
            clientProvision.Apply(clientConn);
        }
    }
}
