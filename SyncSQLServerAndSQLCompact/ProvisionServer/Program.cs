using System.Data.SqlClient;
using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServer;

namespace ProvisionServer
{
    class Program
    {
        static void Main(string[] args)
        {
            // 创建数据库连接
            SqlConnection serverConn = new SqlConnection("Data Source=localhost; Initial Catalog=SyncDBMaster; Integrated Security=True");

            // 定义同步作用域ProductsScope
            DbSyncScopeDescription scopeDesc = new DbSyncScopeDescription("ProductsScope");

            //从SyncDB 服务器数据库检索Products表的架构
            DbSyncTableDescription tableDesc = SqlSyncDescriptionBuilder.GetDescriptionForTable("Products", serverConn);

            // 向同步作用域中添加同步表的描述
            scopeDesc.Tables.Add(tableDesc);

            // 创建一个同步作用域设置对象
            SqlSyncScopeProvisioning serverProvision = new SqlSyncScopeProvisioning(serverConn, scopeDesc);

            // 如果表已经存在则略过创建表的步骤
            serverProvision.SetCreateTableDefault(DbSyncCreationOption.Skip);

            // 开始设置过程
            serverProvision.Apply();
        }
    }
}
