using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.IO;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlServerCe;

using Microsoft.Synchronization;
using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServer;
using Microsoft.Synchronization.Data.SqlServerCe;

namespace Samples.Synchronization
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create the connections over which provisioning and synchronization
            // are performed. The Utility class handles all functionality that is not
            // directly related to synchronization, such as holding connection 
            // string information and making changes to the server database.
            SqlConnection serverConn = new SqlConnection(Utility.ConnStr_SqlSync_Server);
            SqlConnection clientSqlConn = new SqlConnection(Utility.ConnStr_SqlSync_Client);
            SqlCeConnection clientSqlCe1Conn = new SqlCeConnection(Utility.ConnStr_SqlCeSync1);
            SqlCeConnection clientSqlCe2Conn = new SqlCeConnection(Utility.ConnStr_SqlCeSync2);

            // Create a scope named "filtered_customer", and add two tables to the scope.
            // GetDescriptionForTable gets the schema of each table, so that tracking 
            // tables and triggers can be created for that table. For Customer, we add
            // the entire table. For CustomerContact, we add only two of the columns.
            DbSyncScopeDescription scopeDesc = new DbSyncScopeDescription("filtered_customer");

            // Definition for Customer.
            DbSyncTableDescription customerDescription =
                SqlSyncDescriptionBuilder.GetDescriptionForTable("Sales.Customer", serverConn);

            scopeDesc.Tables.Add(customerDescription);

            // Definition for CustomerContact, including the list of columns to include.
            Collection<string> columnsToInclude = new Collection<string>();
            columnsToInclude.Add("CustomerId");
            columnsToInclude.Add("PhoneType");
            DbSyncTableDescription customerContactDescription =
                SqlSyncDescriptionBuilder.GetDescriptionForTable("Sales.CustomerContact", columnsToInclude, serverConn);

            scopeDesc.Tables.Add(customerContactDescription);

            // Create a provisioning object for "filtered_customer". We specify that
            // base tables should not be created (They already exist in SyncSamplesDb_SqlPeer1),
            // and that all synchronization-related objects should be created in a 
            // database schema named "Sync". If you specify a schema, it must already exist
            // in the database.
            SqlSyncScopeProvisioning serverConfig = new SqlSyncScopeProvisioning(scopeDesc);
            serverConfig.SetCreateTableDefault(DbSyncCreationOption.Skip);
            serverConfig.ObjectSchema = "Sync";

            // Specify which column(s) in the Customer table to use for filtering data, 
            // and the filtering clause to use against the tracking table.
            // "[side]" is an alias for the tracking table.
            serverConfig.Tables["Sales.Customer"].AddFilterColumn("CustomerType");
            serverConfig.Tables["Sales.Customer"].FilterClause = "[side].[CustomerType] = 'Retail'";

            // Configure the scope and change-tracking infrastructure.
            //TODO:
            //serverConfig.Apply(serverConn);

            // Write the configuration script to a file. You can modify 
            // this script if necessary and run it against the server
            // to customize behavior.
            File.WriteAllText("SampleConfigScript.txt",
                serverConfig.Script("SyncSamplesDb_SqlPeer1"));


            // Provision each of the client databases.           

            // Create a SQL Server Compact database and provision it based on scope
            // information that is retrieved from the server. Compact databases
            // do not support separate schemas, so we prefix the name of all 
            // synchronization-related objects with "Sync" so that they are easy to
            // identify.
            Utility.DeleteAndRecreateCompactDatabase(Utility.ConnStr_SqlCeSync1, true);
            Utility.DeleteAndRecreateCompactDatabase(Utility.ConnStr_SqlCeSync2, false);
            DbSyncScopeDescription clientSqlCe1Desc = SqlSyncDescriptionBuilder.GetDescriptionForScope("filtered_customer", null, "Sync", serverConn);
            SqlCeSyncScopeProvisioning clientSqlCe1Config = new SqlCeSyncScopeProvisioning(clientSqlCe1Desc);
            clientSqlCe1Config.ObjectPrefix = "Sync";
            clientSqlCe1Config.Apply(clientSqlCe1Conn);

            // Provision the existing database SyncSamplesDb_SqlPeer2 based on scope
            // information that is retrieved from the SQL Server Compact database. We could
            // have also retrieved this information from the server.
            DbSyncScopeDescription clientSqlDesc = SqlCeSyncDescriptionBuilder.GetDescriptionForScope("filtered_customer", "Sync", clientSqlCe1Conn);
            SqlSyncScopeProvisioning clientSqlConfig = new SqlSyncScopeProvisioning(clientSqlDesc);
            clientSqlConfig.ObjectSchema = "Sync";
            clientSqlConfig.Apply(clientSqlConn);


            // Initial synchronization sessions. 7 rows are synchronized:
            // all rows (4) from CustomerContact, and the 3 rows from Customer 
            // that satisfy the filtering criteria.
            SampleSyncOrchestrator syncOrchestrator;
            SyncOperationStatistics syncStats;

            // Data is downloaded from the server to the SQL Server client.
            syncOrchestrator = new SampleSyncOrchestrator(
                new SqlSyncProvider("filtered_customer", clientSqlConn, null, "Sync"),
                new SqlSyncProvider("filtered_customer", serverConn, null, "Sync")
                );
            syncStats = syncOrchestrator.Synchronize();
            syncOrchestrator.DisplayStats(syncStats, "initial");

            // Data is downloaded from the SQL Server client to the 
            // first SQL Server Compact client.
            syncOrchestrator = new SampleSyncOrchestrator(
                new SqlCeSyncProvider("filtered_customer", clientSqlCe1Conn, "Sync"),
                new SqlSyncProvider("filtered_customer", clientSqlConn, null, "Sync")
                );
            syncStats = syncOrchestrator.Synchronize();
            syncOrchestrator.DisplayStats(syncStats, "initial");

            // Create a snapshot from the SQL Server Compact database, which will be used to
            // initialize a second Compact database. Again, this database could be provisioned
            // by retrieving scope information from another database, but we want to 
            // demonstrate the use of snapshots, which provide a convenient deployment
            // mechanism for Compact databases.
            SqlCeSyncStoreSnapshotInitialization syncStoreSnapshot = new SqlCeSyncStoreSnapshotInitialization("Sync");
            syncStoreSnapshot.GenerateSnapshot(clientSqlCe1Conn, "SyncSampleClient2.sdf");

            // The new SQL Server Compact client synchronizes with the server, but
            // no data is downloaded because the snapshot already contains 
            // all of the data from the first Compact database.
            syncOrchestrator = new SampleSyncOrchestrator(
                new SqlSyncProvider("filtered_customer", serverConn, null, "Sync"),
                new SqlCeSyncProvider("filtered_customer", clientSqlCe2Conn, "Sync")
                );
            syncStats = syncOrchestrator.Synchronize();
            syncOrchestrator.DisplayStats(syncStats, "initial");


            // Make changes on the server: 1 insert, 1 update, and 1 delete.
            Utility.MakeDataChangesOnNode(Utility.ConnStr_SqlSync_Server, "Customer");

            // Synchronize again. Three changes were made on the server, but
            // only two of them applied to rows that are in the "filtered_customer"
            // scope. The other row is not synchronized.
            // Notice that the order of synchronization is different from the initial
            // sessions, but the two changes are propagated to all nodes.
            syncOrchestrator = new SampleSyncOrchestrator(
                new SqlCeSyncProvider("filtered_customer", clientSqlCe1Conn, "Sync"),
                new SqlSyncProvider("filtered_customer", serverConn, null, "Sync")
                );
            syncStats = syncOrchestrator.Synchronize();
            syncOrchestrator.DisplayStats(syncStats, "subsequent");

            syncOrchestrator = new SampleSyncOrchestrator(
                new SqlSyncProvider("filtered_customer", clientSqlConn, null, "Sync"),
                new SqlCeSyncProvider("filtered_customer", clientSqlCe1Conn, "Sync")
            );
            syncStats = syncOrchestrator.Synchronize();
            syncOrchestrator.DisplayStats(syncStats, "subsequent");

            syncOrchestrator = new SampleSyncOrchestrator(
                new SqlCeSyncProvider("filtered_customer", clientSqlCe2Conn, "Sync"),
                new SqlSyncProvider("filtered_customer", clientSqlConn, null, "Sync")
            );
            syncStats = syncOrchestrator.Synchronize();
            syncOrchestrator.DisplayStats(syncStats, "subsequent");

            serverConn.Close();
            serverConn.Dispose();
            clientSqlConn.Close();
            clientSqlConn.Dispose();
            clientSqlCe1Conn.Close();
            clientSqlCe1Conn.Dispose();
            clientSqlCe2Conn.Close();
            clientSqlCe2Conn.Dispose();

            Utility.CleanUpSqlNode(Utility.ConnStr_SqlSync_Server);
            Utility.CleanUpSqlNode(Utility.ConnStr_SqlSync_Client);

            Console.Write("\nPress any key to exit.");
            Console.Read();

        }
    }
}
