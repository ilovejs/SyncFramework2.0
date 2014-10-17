using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServer;
using Microsoft.Synchronization.Data.SqlServerCe;
namespace ProvisionFilteredScopeClient
{
    class Program
    {
        static void Main(string[] args)
        {
            /**
             * Provision/temp client with filtered scope
             */

            //create connection to the compact DB
            var clientConn = new SqlCeConnection(@"Data Source='C:\SyncSQLServerAndSQLCompact\SyncCompactDB.sdf'");

            //create connection to the server DB
            var serverConn = new SqlConnection("Data Source=localhost; Initial Catalog=SyncDB; Integrated Security=True");

            // get description for the OrdersScope-NC scope from the SyncDB server database
            DbSyncScopeDescription scopeDesc = SqlSyncDescriptionBuilder.GetDescriptionForScope("OrdersScope-NC", serverConn);

            // create a CE provisioning object
            var clientProvision = new SqlCeSyncScopeProvisioning(scopeDesc);

            // start the provisioning process
            clientProvision.Apply(clientConn);
        }
    }
}
