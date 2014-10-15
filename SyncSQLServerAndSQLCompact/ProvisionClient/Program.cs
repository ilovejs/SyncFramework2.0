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
            var clientConn = new SqlConnection("Data Source=IT34;Initial Catalog=AGE14S_new;User ID=sa;Password=ISAsql07");

            var serverConn = new SqlConnection("Data Source=IT34;Initial Catalog=AGE14S;User ID=sa;Password=ISAsql07");

            DbSyncScopeDescription scopeDesc = SqlSyncDescriptionBuilder.GetDescriptionForScope("FullTableScope", serverConn);

            // 基于ProductsScope同步作用域创建CE设置对象
            var clientProvision = new SqlSyncScopeProvisioning(scopeDesc);

            clientProvision.SetCreateTableDefault(DbSyncCreationOption.Skip);
            // 开始设置过程
            clientProvision.Apply(clientConn);
        }
    }
}
