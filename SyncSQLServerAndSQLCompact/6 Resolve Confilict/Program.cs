// NOTE: Before running this application, run the database sample script that is
// available in the documentation. The script drops and re-creates the tables that 
// are used in the code, and ensures that synchronization objects are dropped so that 
// Sync Framework can re-create them.

using System;
using System.IO;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Synchronization;
using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServer;
using Microsoft.Synchronization.Data.SqlServerCe;
using Samples.Synchronization;

namespace _6_Resolve_Conflict
{
    class Program
    {
        static void Main(string[] args)
        {

            // Create the connections over which provisioning and synchronization
            // are performed. The Utility class handles all functionality that is not
            //directly related to synchronization, such as holding connection 
            //string information and making changes to the server database.
            var serverConn = new SqlConnection(Utility.ConnStr_SqlSync_Server);         //peer1
            var clientSqlConn = new SqlConnection(Utility.ConnStr_SqlSync_Client);      //peer2
            var clientSqlCe1Conn = new SqlConnection(Utility.ConnStr_SqlCeSync1);     //ce 1

            // Create a scope named "customer", and add the Customer table to the scope.
            // GetDescriptionForTable gets the schema of the table, so that tracking 
            // tables and triggers can be created for that table.
            var scopeDesc = new DbSyncScopeDescription("customer");

            scopeDesc.Tables.Add(
            SqlSyncDescriptionBuilder.GetDescriptionForTable("Sales.Customer", serverConn));

            //1. Create a provisioning object for "customer" and specify that base tables should not be created 
            // (They already exist in SyncSamplesDb_SqlPeer1).
            var serverConfig = new SqlSyncScopeProvisioning(scopeDesc);
            serverConfig.SetCreateTableDefault(DbSyncCreationOption.Skip);
            // Configure the scope and change-tracking infrastructure.
            //serverConfig.Apply(serverConn);

            //2. Retrieve scope information from the server and use the schema that is retrieved to provision the SQL Server and SQL Server Compact client databases.           
            // This database already exists on the server.
            var clientSqlDesc = SqlSyncDescriptionBuilder.GetDescriptionForScope("customer", serverConn);
            var clientSqlConfig = new SqlSyncScopeProvisioning(clientSqlDesc);  //get provision object
            //clientSqlConfig.Apply(clientSqlConn);

            //3. This database does not yet exist.
            var clientSqlCeDesc = SqlSyncDescriptionBuilder.GetDescriptionForScope("customer", serverConn);
            var clientSqlCeConfig = new SqlSyncScopeProvisioning(clientSqlCeDesc);
            //clientSqlCeConfig.Apply(clientSqlCe1Conn);


            //============================== Initial synchronization sessions ==============================
            SampleSyncOrchestrator syncOrchestrator;
            SyncOperationStatistics syncStats;

            // Data is downloaded from the server to the SQL Server client.
            syncOrchestrator = new SampleSyncOrchestrator(
                new SqlSyncProvider("customer", clientSqlConn),
                new SqlSyncProvider("customer", serverConn)
            );
            syncStats = syncOrchestrator.Synchronize();
            syncOrchestrator.DisplayStats(syncStats, "initial");

            // Data is downloaded from the SQL Server client to the SQL Server Compact client.
            syncOrchestrator = new SampleSyncOrchestrator(
                new SqlSyncProvider("customer", clientSqlCe1Conn),
                new SqlSyncProvider("customer", clientSqlConn)
            );
            syncStats = syncOrchestrator.Synchronize();
            syncOrchestrator.DisplayStats(syncStats, "initial");

            //============================== End: Initial synchronization sessions ==============================

            //============================== Make conflicting changes in two databases. =========================
            Utility.MakeConflictingChangeOnNode(Utility.ConnStr_SqlSync_Client, "Customer");
            Utility.MakeConflictingChangeOnNode(Utility.ConnStr_SqlSync_Server, "Customer");

            // Subsequent synchronization sessions.
            syncOrchestrator = new SampleSyncOrchestrator(
                new SqlSyncProvider("customer", clientSqlConn),
                new SqlSyncProvider("customer", serverConn)
            );
            syncStats = syncOrchestrator.Synchronize();
            syncOrchestrator.DisplayStats(syncStats, "subsequent");

            syncOrchestrator = new SampleSyncOrchestrator(
                new SqlSyncProvider("customer", clientSqlCe1Conn),
                new SqlSyncProvider("customer", clientSqlConn)
            );
            syncStats = syncOrchestrator.Synchronize();
            syncOrchestrator.DisplayStats(syncStats, "subsequent");

            //============================== End: Make conflicting changes in two databases ======================


            //===============================Make a change in SyncSamplesDb_Peer2 that will fail 
            //                               when synchronized with SyncSamplesDb_Peer1
            Utility.MakeFailingChangeOnNode(Utility.ConnStr_SqlSync_Client);


            // Subsequent synchronization sessions.
            syncOrchestrator = new SampleSyncOrchestrator(
                new SqlSyncProvider("customer", clientSqlConn),
                new SqlSyncProvider("customer", serverConn)
            );
            syncStats = syncOrchestrator.Synchronize();
            syncOrchestrator.DisplayStats(syncStats, "subsequent");

            syncOrchestrator = new SampleSyncOrchestrator(
                new SqlSyncProvider("customer", clientSqlCe1Conn),
                new SqlSyncProvider("customer", clientSqlConn)
                );
            syncStats = syncOrchestrator.Synchronize();
            syncOrchestrator.DisplayStats(syncStats, "subsequent");


            //Exit.
            Console.Write("\nPress Enter to close the window.");
            Console.ReadLine();
        }
    }

    //My wrapper for SyncOchestrator
    public class SampleSyncOrchestrator : SyncOrchestrator
    {

        //Create class-level variables so that the ApplyChangeFailedEvent 
        //handler can use them.
        private string _localProviderDatabase;
        private string _remoteProviderDatabase;


        public SampleSyncOrchestrator(RelationalSyncProvider localProvider, RelationalSyncProvider remoteProvider)
        {

            this.LocalProvider = localProvider;
            this.RemoteProvider = remoteProvider;
            this.Direction = SyncDirectionOrder.UploadAndDownload;

            _localProviderDatabase = localProvider.Connection.Database.ToString();
            _remoteProviderDatabase = remoteProvider.Connection.Database.ToString();

            //Specify event handlers for the ApplyChangeFailed event for each provider.
            //TODO: The handlers are used to resolve conflicting rows and log error information.
            localProvider.ApplyChangeFailed += new EventHandler<DbApplyChangeFailedEventArgs>(dbProvider_ApplyChangeFailed);
            remoteProvider.ApplyChangeFailed += new EventHandler<DbApplyChangeFailedEventArgs>(dbProvider_ApplyChangeFailed);

        }

        public void DisplayStats(SyncOperationStatistics syncStatistics, string syncType)
        {
            Console.WriteLine(String.Empty);
            if (syncType == "initial")
            {
                Console.WriteLine("****** Initial Synchronization ******");
            }
            else if (syncType == "subsequent")
            {
                Console.WriteLine("***** Subsequent Synchronization ****");
            }

            Console.WriteLine("Start Time: " + syncStatistics.SyncStartTime);
            Console.WriteLine("Total Changes Uploaded: " + syncStatistics.UploadChangesTotal);
            Console.WriteLine("Total Changes Downloaded: " + syncStatistics.DownloadChangesTotal);
            Console.WriteLine("Complete Time: " + syncStatistics.SyncEndTime);
            Console.WriteLine(String.Empty);
        }

        private void dbProvider_ApplyChangeFailed(object sender, DbApplyChangeFailedEventArgs e)
        {

            //For conflict detection, the "local" database is the one at which the ApplyChangeFailed event occurs. 
            //We determine at which database the event fired and then compare the name of that database to the names of
            //the databases specified as the LocalProvider and RemoteProvider.
            string DbConflictDetected = e.Connection.Database.ToString();
            string DbOther;

            DbOther = DbConflictDetected == _localProviderDatabase ? _remoteProviderDatabase : _localProviderDatabase;

            Console.WriteLine(String.Empty);
            Console.WriteLine("Conflict of type " + e.Conflict.Type + " was detected at " + DbConflictDetected + ".");

            if (e.Conflict.Type == DbConflictType.LocalUpdateRemoteUpdate)
            {
                //Get the conflicting changes from the Conflict object and display them. 
                //The Conflict object holds a copy of the changes; updates to this object will not be applied. 
                //To make changes, use the Context object.
                DataTable conflictingRemoteChange = e.Conflict.RemoteChange;
                DataTable conflictingLocalChange = e.Conflict.LocalChange;
                int remoteColumnCount = conflictingRemoteChange.Columns.Count;
                int localColumnCount = conflictingLocalChange.Columns.Count;

                Console.WriteLine(String.Empty);
                Console.WriteLine(String.Empty);
                Console.WriteLine("Row from database " + DbConflictDetected);
                Console.Write(" | ");

                //Display the local row. As mentioned above, this is the row
                //from the database at which the conflict was detected.
                for (int i = 0; i < localColumnCount; i++)
                {
                    Console.Write(conflictingLocalChange.Rows[0][i] + " | ");
                }

                Console.WriteLine(String.Empty);
                Console.WriteLine(String.Empty);
                Console.WriteLine(String.Empty);
                Console.WriteLine("Row from database " + DbOther);
                Console.Write(" | ");

                //Display the remote row.
                for (int i = 0; i < remoteColumnCount; i++)
                {
                    Console.Write(conflictingRemoteChange.Rows[0][i] + " | ");
                }

                //Ask for a conflict resolution option.
                Console.WriteLine(String.Empty);
                Console.WriteLine(String.Empty);
                Console.WriteLine("Enter a resolution option for this conflict:");
                Console.WriteLine("A = change from " + DbConflictDetected + " wins.");
                Console.WriteLine("B = change from " + DbOther + " wins.");

                string conflictResolution = Console.ReadLine();
                conflictResolution.ToUpper();

                if (conflictResolution == "A")
                {
                    e.Action = ApplyAction.Continue;
                }

                else if (conflictResolution == "B")
                {
                    //use syncronization adapter command
                    e.Action = ApplyAction.RetryWithForceWrite;
                }

                else
                {
                    Console.WriteLine(String.Empty);
                    Console.WriteLine("Not a valid resolution option.");
                }
            }

            //Write any errors to a log file.
            else if (e.Conflict.Type == DbConflictType.ErrorsOccurred)
            {

                string logFile = @"C:\SyncErrorLog.txt";

                Console.WriteLine(String.Empty);
                Console.WriteLine("An error occurred during synchronization.");
                Console.WriteLine("This error has been logged to " + logFile + ".");

                StreamWriter streamWriter = File.AppendText(logFile);
                StringBuilder outputText = new StringBuilder();

                outputText.AppendLine("** APPLY CHANGE FAILURE AT " + DbConflictDetected.ToUpper() + " **");
                outputText.AppendLine("Error source: " + e.Error.Source);
                outputText.AppendLine("Error message: " + e.Error.Message);

                streamWriter.WriteLine(DateTime.Now.ToShortTimeString() + " | " + outputText.ToString());
                streamWriter.Flush();
                streamWriter.Dispose();

            }
        }
    }
}