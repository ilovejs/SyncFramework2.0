using System.Data.SqlClient;
using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServer;

namespace ProvisionServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var serverConn = new SqlConnection("Data Source=IT34;Initial Catalog=SyncDBMaster;User ID=sa;Password=ISAsql07");

            // Define Sync Scope
            // Get table description/info
            // Add table description to Scope
            // Create Provision Object
            
            var scopeDesc = new DbSyncScopeDescription("ProductsScope");

            var tableDesc = SqlSyncDescriptionBuilder.GetDescriptionForTable("Products", serverConn);

            scopeDesc.Tables.Add(tableDesc);

            var serverProvision = new SqlSyncScopeProvisioning();

            // skip if table exists
            serverProvision.SetCreateTableDefault(DbSyncCreationOption.Skip);

            // apply setting
            serverProvision.Apply(serverConn);


        }
    }
}