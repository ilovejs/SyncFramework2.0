using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlServerCe;
using Microsoft.Synchronization.Data.SqlServerCe;
namespace ProvisionSecondCompactClient
{
    class Program
    {
        static void Main(string[] args)
        {
            /**
             * Take snapshot for compact version and name it 'SyncCompactDB2'
             */
            // connect to the first compact client DB
            var clientConn = new SqlCeConnection(@"Data Source='C:\SyncSQLServerAndSQLCompact\SyncCompactDB.sdf'");

            // create a snapshot of SyncCompactDB and save it to SyncCompactDB2 database
            var syncStoreSnapshot = new SqlCeSyncStoreSnapshotInitialization();
            syncStoreSnapshot.GenerateSnapshot(clientConn, @"C:\SyncSQLServerAndSQLCompact\SyncCompactDB2.sdf");
        }
    }
}
