using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Synchronization.ClientServices.SQLite
{
    public class SQLiteConstants
    {
        // File name used for storing scopeName info
        public static readonly string ScopeInfo = "ScopeInfoTable";

        public const string ScopeExist = "SELECT name FROM sqlite_master WHERE type='table' AND name='ScopeInfoTable'";
        public const string SelectAllTables = "SELECT name FROM sqlite_master WHERE type='table'";

        public static String CreateTriggerAfterInsert =
            "CREATE TRIGGER IF NOT EXISTS {0}_insert_trigger AFTER INSERT ON [{0}] " + Environment.NewLine +
            "Begin " + Environment.NewLine +
            "Insert into [{0}_tracking] " + Environment.NewLine +
            "({1}, Oem_IsTombstone, Oem_IsDirty, Oem_LastModifiedDate) " + Environment.NewLine +
            "Values " + Environment.NewLine +
            "({2}, 0, 1, DATETIME('now')); " + Environment.NewLine +
            "End;";

        public static string DeleteTriggerAfterInsert = "DROP TRIGGER IF EXISTS {0}_insert_trigger";

        public static String CreateTriggerAfterUpdate =
            "CREATE TRIGGER IF NOT EXISTS {0}_update_trigger AFTER UPDATE ON [{0}] " + Environment.NewLine +
            "Begin " + Environment.NewLine +
            "Update [{0}_tracking]  " + Environment.NewLine +
            "Set Oem_IsTombstone = 0, Oem_IsDirty = 1, " + Environment.NewLine +
            "Oem_LastModifiedDate = DATETIME('now') " + Environment.NewLine +
            "Where {1}; " + Environment.NewLine +
            "End;";

        public static string DeleteTriggerAfterUpdate = "DROP TRIGGER IF EXISTS {0}_update_trigger";

        public static String CreateTriggerAfterDelete =
            "CREATE TRIGGER IF NOT EXISTS {0}_delete_trigger AFTER DELETE ON [{0}] " + Environment.NewLine +
            "Begin " + Environment.NewLine +
            "Update [{0}_tracking]  " + Environment.NewLine +
            "Set Oem_IsTombstone = 1, Oem_IsDirty = 1, " + Environment.NewLine +
            "Oem_LastModifiedDate = DATETIME('now') " + Environment.NewLine +
            "Where {1}; " + Environment.NewLine +
            "End;";

        public static string DeleteTriggerAfterDelete = "DROP TRIGGER IF EXISTS {0}_delete_trigger";

        /// <summary>
        /// {0} : TableName
        /// {1} : Columns with Type list
        /// </summary>
        public static string CreateTable =
            "CREATE TABLE IF NOT EXISTS \"{0}\" (" + Environment.NewLine +
            "{1} {2});";

        /// <summary>
        /// {0} : TableName
        /// {1} : Columns with Type list
        /// </summary>
        public static string CreateTrackingTable =
            "CREATE TABLE IF NOT EXISTS \"{0}_tracking\"(" + Environment.NewLine +
            "{1} {2});";


        /// <summary>
        /// {0} : TableName
        /// {1} : PK
        /// {2} : Columns list
        /// </summary>
        public static String SelectChanges =
            "SELECT {2}, t.Oem_IsTombstone, t.Oem_Id, t.Oem_Etag, t.Oem_EditUri " + Environment.NewLine +
            "FROM [{0}_tracking] t " + Environment.NewLine +
            "LEFT JOIN [{0}] s on s.[{1}] = t.[{1}] " + Environment.NewLine +
            "Where (t.Oem_IsTombStone = 1  " + Environment.NewLine +
            "OR t.Oem_IsDirty = 1 )  " + Environment.NewLine +
            "And t.Oem_LastModifiedDate > ?";

        /// <summary>
        /// {0} : TableName
        /// {1} : Columns list
        /// {2} : Values
        /// </summary>
        public static String InsertOrReplaceFromChanges =
            "INSERT OR REPLACE INTO [{0}]  " +
            "({1}) " +
            "VALUES " +
            "({2}) ";

        
        /// <summary>
        /// {0} : TableName
        /// {1} : Primary Key Name
        /// </summary>
        public static String DeleteFromChanges =
            "DELETE FROM [{0}] Where {1}";

        /// <summary>
        /// {0} : TableName
        /// {1} : Primary Key Name
        /// </summary>
        public static String DeleteTrackingFromChanges =
            "DELETE FROM [{0}_tracking] Where {1}";

        /// <summary>
        /// {0} : TableName
        /// {1} : Primary Key Name
        /// </summary>
        public static String SelectItemPrimaryKeyFromTrackingChangesWithOemID =
            "SELECT {1} FROM [{0}_tracking] Where Oem_Id = ?";


        /// <summary>
        /// {0} : TableName
        /// {1} : PK Column
        /// </summary>
        public static String UpdateTrackingFromChanges =
            "UPDATE [{0}_tracking]  " +
            "Set Oem_IsTombstone = ?, Oem_IsDirty = ?, Oem_Id = ?, Oem_Etag = ?, Oem_EditUri = ?, Oem_LastModifiedDate = ? " +
            "Where {1}";

        /// <summary>
        /// {0} : TableName
        /// {1} : Columns list
        /// {2} : Values
        /// </summary>
        public static String InsertOrReplaceTrackingFromChanges =
            "INSERT OR REPLACE INTO [{0}_tracking]  " +
            "(Oem_IsTombstone, Oem_IsDirty, Oem_Id, Oem_Etag, Oem_EditUri, Oem_LastModifiedDate, {1}) " +
            "VALUES " +
            "(?, ?, ?, ?, ?, ?, {2}) ";

        /// <summary>
        /// {0} : TableName
        /// </summary>
        public static String UpdateDirtyTracking =
            "UPDATE [{0}_tracking] " +
            "Set Oem_IsTombstone = ?, Oem_IsDirty = ?, Oem_Etag = ?, Oem_EditUri = ? " +
            "WHERE Oem_ID = ? ";

    }
}
