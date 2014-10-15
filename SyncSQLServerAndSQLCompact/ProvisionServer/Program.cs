using System;
using System.Data.SqlClient;
using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServer;
using System.Configuration;

namespace ProvisionServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var serverConn = new SqlConnection("Data Source=IT34;Initial Catalog=AGE14S;User ID=sa;Password=ISAsql07");

            // Define Sync Scope
            // Get table description/info
            // Add table description to Scope
            // Create Provision Object
            
            var scopeDesc = new DbSyncScopeDescription("FullTableScope");

            var enumerator = ConfigurationManager.AppSettings.GetEnumerator();
            
            while (enumerator.MoveNext())
            {
                var tableName = ConfigurationManager.AppSettings[enumerator.Current.ToString()];
                var tableDesc = SqlSyncDescriptionBuilder.GetDescriptionForTable(tableName, serverConn);
                scopeDesc.Tables.Add(tableDesc);    
            }

            
            var serverProvision = new SqlSyncScopeProvisioning(scopeDesc);

            // skip if table exists
            serverProvision.SetCreateTableDefault(DbSyncCreationOption.Skip);

            // apply setting
            serverProvision.Apply(serverConn);


        }
    }
}