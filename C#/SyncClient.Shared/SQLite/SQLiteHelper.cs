using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Synchronization.ClientServices.Common;
using Microsoft;
using System.Reflection;
#if ( WINDOWS_PHONE || NETFX_CORE) 
using Windows.Storage;
#endif
using SQLitePCL;
using System.IO;
using System.Globalization;



namespace Microsoft.Synchronization.ClientServices.SQLite
{
    internal class SQLiteHelper
    {
        private SQLiteManager manager;
        private String localFilePath;
        public SQLiteHelper(String localfilePath, SQLiteManager sqLiteManager)
        {
            this.manager = sqLiteManager;
            this.localFilePath = localfilePath;
        }

        /// <summary>
        /// Create a OfflineEntity Table with metadata information added
        /// </summary>
        internal void CreateTable(Type ty)
        {
            using (SQLiteConnection connection = new SQLiteConnection(localFilePath))
            {
                try
                {
                    // Get mapping from my type
                    var map = manager.GetMapping(ty);

                    // Create 2 tables : One for datas, one for tracking
                    var query = SQLiteConstants.CreateTable;
                    var queryTracking = SQLiteConstants.CreateTrackingTable;

                    var columnsDcl = new List<String>();
                    var columnsDclTracking = new List<String>();
                    var columnsPk = new List<String>();

                    // Foreach columns, create the tsql command to execute
                    foreach (var c in map.Columns)
                    {
                        string dec = "\"" + c.Name + "\" " + Orm.SqlType(c) + " ";

                        columnsDcl.Add(dec);

                        // If it's the PK, add it to tracking
                        if (!c.IsPK) continue;

                        columnsDclTracking.Add(dec);
                        columnsPk.Add(c.Name + " ");
                    }


                    var pkTracking = string.Join(",\n", columnsPk.ToArray());
                    // Adding metadatas to tracking table
                    columnsDclTracking.AddRange(GetOfflineEntityMetadataSQlDecl());

                    var decl = string.Join(",\n", columnsDcl.ToArray());
                    var declTracking = string.Join(",\n", columnsDclTracking.ToArray());

                    string pKeyDecl = String.Empty;
                    if (columnsDclTracking.Count > 0)
                        pKeyDecl = String.Format(",\n PRIMARY KEY ({0})", pkTracking);

                    query = String.Format(query, map.TableName, decl, pKeyDecl);
                    queryTracking = String.Format(queryTracking, map.TableName, declTracking, pKeyDecl);

                    using (var statement = connection.Prepare(query))
                    {
                        statement.Step();
                    }
                    using (var statement = connection.Prepare(queryTracking))
                    {
                        statement.Step();
                    }

                    var indexes = new Dictionary<string, IndexInfo>();
                    foreach (var c in map.Columns)
                    {
                        foreach (var i in c.Indices)
                        {
                            var iname = i.Name ?? map.TableName + "_" + c.Name;
                            IndexInfo iinfo;
                            if (!indexes.TryGetValue(iname, out iinfo))
                            {
                                iinfo = new IndexInfo
                                {
                                    //IndexName = iname,
                                    TableName = map.TableName,
                                    Unique = i.Unique,
                                    Columns = new List<IndexedColumn>()
                                };
                                indexes.Add(iname, iinfo);
                            }

                            if (i.Unique != iinfo.Unique)
                                throw new Exception(
                                    "All the columns in an index must have the same value for their Unique property");

                            iinfo.Columns.Add(new IndexedColumn
                            {
                                Order = i.Order,
                                ColumnName = c.Name
                            });
                        }
                    }

                    foreach (var indexName in indexes.Keys)
                    {
                        var index = indexes[indexName];
                        const string sqlFormat = "create {3} index if not exists \"{0}\" on \"{1}\"(\"{2}\")";
                        var columns = String.Join("\",\"",
                                                  index.Columns.OrderBy(i => i.Order).Select(i => i.ColumnName).ToArray());
                        var sql = String.Format(sqlFormat, indexName, index.TableName, columns,
                                                index.Unique ? "unique" : "");

                        using (var statement = connection.Prepare(sql))
                        {
                            statement.Step();
                        }
                    }

                    // Create Triggers
                    this.CreateTriggers(ty, connection);

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    throw;

                }
            }
        }

        private void DisableTriggers(TableMapping map, SQLiteConnection connection)
        {
            try
            {

                String triggerDeleteQuery = String.Format(SQLiteConstants.DeleteTriggerAfterDelete, map.TableName);
                String triggerInsertQuery = String.Format(SQLiteConstants.DeleteTriggerAfterInsert, map.TableName);
                String triggerUpdateQuery = String.Format(SQLiteConstants.DeleteTriggerAfterUpdate, map.TableName);

                using (var statement = connection.Prepare(triggerDeleteQuery))
                {
                    statement.Step();
                }
                using (var statement = connection.Prepare(triggerInsertQuery))
                {
                    statement.Step();
                }
                using (var statement = connection.Prepare(triggerInsertQuery))
                {
                    statement.Step();
                }

                //await connection.ExecuteStatementAsync(triggerDeleteQuery).AsTask().ConfigureAwait(false);
                //await connection.ExecuteStatementAsync(triggerInsertQuery).AsTask().ConfigureAwait(false);
                //await connection.ExecuteStatementAsync(triggerUpdateQuery).AsTask().ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;

            }

        }

        private void CreateTriggers(Type ty, SQLiteConnection connection)
        {
            try
            {
                var map = manager.GetMapping(ty);

                // Create the 3 triggers on table
                // Insert Trigger
                // Update Trigger
                // Delete Trigger

                var lstNewClauses = map.PrimaryKeys.Select(primaryKey => " new." + primaryKey.Name).ToList();
                string pkeysNames = String.Join(", ", map.PrimaryKeys.Select(column => column.Name));
                string pkeysNewNames = String.Join(", ", lstNewClauses);

                var declWhere =
                    map.PrimaryKeys.Select(primaryKey => String.Format("\"{0}\" = old.\"{0}\" ", primaryKey.Name))
                       .ToList();
                string updateOrWherePkeysName = String.Join(" and ", declWhere);

                String triggerInsertQuery =
                    String.Format(SQLiteConstants.CreateTriggerAfterInsert,
                                  map.TableName, pkeysNames, pkeysNewNames);

                using (var statement = connection.Prepare(triggerInsertQuery))
                {
                    statement.Step();
                }


                String triggerUpdateQuery =
                    String.Format(SQLiteConstants.CreateTriggerAfterUpdate,
                                  map.TableName, updateOrWherePkeysName);


                using (var statement = connection.Prepare(triggerUpdateQuery))
                {
                    statement.Step();
                }
                String triggerDeleteQuery =
                    String.Format(SQLiteConstants.CreateTriggerAfterDelete,
                                  map.TableName, updateOrWherePkeysName);

                using (var statement = connection.Prepare(triggerDeleteQuery))
                {
                    statement.Step();
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }

        }

        /// <summary>
        /// Delete tracking from Tracking table
        /// (Items have been deleted by users before so just need to delete Tracking information after sync is completed)
        /// </summary>
        internal void DeleteTombstoneTrackingEntities(Type ty, List<SQLiteOfflineEntity> entities)
        {
            using (SQLiteConnection connection = new SQLiteConnection(this.localFilePath))
            {
                // Get mapping from my type
                var map = manager.GetMapping(ty);

                var declWhere = map.PrimaryKeys.Select(primaryKey => String.Format("\"{0}\" = ? ", primaryKey.Name)).ToList();
                string updateOrWherePkeysName = String.Join(" and ", declWhere);

                var queryDeleteTracking = String.Format(SQLiteConstants.DeleteTrackingFromChanges,
                    map.TableName, updateOrWherePkeysName);

                try
                {
                    using (var statement = connection.Prepare("Begin Transaction"))
                    {
                        statement.Step();
                    }

                    foreach (var entity in entities)
                    {
                        using (var stmtDeleteItemTracking = connection.Prepare(queryDeleteTracking))
                        {
                            // We have Primary Key, so we can delete item form table and table_tracking
                            // Bind parameters
                            BindParameter(stmtDeleteItemTracking, 1, entity.ServiceMetadata.Id);

                            stmtDeleteItemTracking.Step();
                            stmtDeleteItemTracking.Reset();
                            stmtDeleteItemTracking.ClearBindings();
                        }
                    }
                    using (var statement = connection.Prepare("Commit Transaction"))
                    {
                        statement.Step();
                    }

                }
                catch (Exception ex)
                {
                    using (var statement = connection.Prepare("Rollback Transaction"))
                    {
                        statement.Step();
                    }
                    Debug.WriteLine(ex.Message);
                    throw;
                }


            }
        }

        /// <summary>
        /// After the response from server, we need to update IsDirty for all entities
        /// </summary>
        internal void UpdateDirtyTrackingEntities(Type ty, List<SQLiteOfflineEntity> entities)
        {
            using (SQLiteConnection connection = new SQLiteConnection(localFilePath))
            {
                // Get mapping from my type
                var map = manager.GetMapping(ty);
                var queryUpdateDirtyTracking = String.Format(SQLiteConstants.UpdateDirtyTracking, map.TableName);

                try
                {
                    using (var statement = connection.Prepare("Begin Transaction"))
                    {
                        statement.Step();
                    }

                    using (var stmtTracking = connection.Prepare(queryUpdateDirtyTracking))
                    {
                        foreach (var entity in entities)
                        {

                            // Set Values for tracking table
                            BindParameter(stmtTracking, 1, entity.ServiceMetadata.IsTombstone);
                            BindParameter(stmtTracking, 2, 0);
                            BindParameter(stmtTracking, 3, entity.ServiceMetadata.ETag);

                            var editUri = String.Empty;
                            if (entity.ServiceMetadata.EditUri != null && entity.ServiceMetadata.EditUri.IsAbsoluteUri)
                                editUri = entity.ServiceMetadata.EditUri.AbsoluteUri;

                            BindParameter(stmtTracking, 4, editUri);
                            BindParameter(stmtTracking, 5, entity.ServiceMetadata.Id);

                            //await stmtTracking.StepAsync().AsTask().ConfigureAwait(false);
                            stmtTracking.Step();

                            stmtTracking.Reset();
                            stmtTracking.ClearBindings();
                        }

                    }
                    using (var statement = connection.Prepare("Commit Transaction"))
                    {
                        statement.Step();
                    }



                }
                catch (Exception ex)
                {
                    using (var statement = connection.Prepare("Rollback Transaction"))
                    {
                        statement.Step();
                    }

                    Debug.WriteLine(ex.Message);
                    throw;
                }
            }
        }

        /// <summary>
        /// Merg entities in SQLite DB
        /// </summary>
        internal void MergeEntities(Type ty, List<SQLiteOfflineEntity> entities)
        {

            using (SQLiteConnection connection = new SQLiteConnection(this.localFilePath))
            {

                // Get mapping from my type
                var map = manager.GetMapping(ty);

                // Foreach columns, create the list of columns to insert or update
                var columnsDcl = new List<String>();
                var columnsValues = new List<String>();
                var columnsDclTracking = new List<String>();
                var columnsValuesTracking = new List<String>();

                foreach (var c in map.Columns)
                {
                    columnsDcl.Add("[" + c.Name + "]");
                    columnsValues.Add("? ");
                }
                foreach (var c in map.PrimaryKeys)
                {
                    columnsDclTracking.Add("\"" + c.Name + "\"");
                    columnsValuesTracking.Add("? ");
                }

                var decl = string.Join(",", columnsDcl.ToArray());
                var declValues = string.Join(",", columnsValues.ToArray());
                var declTracking = string.Join(",", columnsDclTracking.ToArray());
                var declValuesTracking = string.Join(",", columnsValuesTracking.ToArray());

                var declValuePairs = columnsDcl.Zip(columnsValues, (col,val) => col+"="+val).ToArray();
                var declValuePairsStr = string.Join(",", declValuePairs);
                

                // Creating queries
                var queryInsert = String.Format(SQLiteConstants.InsertOrIgnoreFromChanges, map.TableName, decl, declValues);
                var queryUpdate = String.Format(SQLiteConstants.UpdateOrIgnoreFromChanges, map.TableName, declValuePairsStr, map.GetPrimaryKeysWhereClause);
                var queryUpdateTracking = String.Format(SQLiteConstants.InsertOrReplaceTrackingFromChanges, map.TableName, declTracking, declValuesTracking);
                var queryDelete = String.Format(SQLiteConstants.DeleteFromChanges, map.TableName, map.GetPrimaryKeysWhereClause);
                var queryDeleteTracking = String.Format(SQLiteConstants.DeleteTrackingFromChanges, map.TableName, map.GetPrimaryKeysWhereClause);


                string pkeysNames = String.Join(", ", map.PrimaryKeys.Select(column => column.Name));
                var querySelectItemPrimaryKeyFromTrackingChangesWithOemID =
                    String.Format(SQLiteConstants.SelectItemPrimaryKeyFromTrackingChangesWithOemID, map.TableName,
                                  pkeysNames);



                try
                {
                    using (var statement = connection.Prepare("Begin Transaction"))
                    {
                        statement.Step();
                    }

                    // Disable Trigger
                    this.DisableTriggers(map, connection);

                    // Prepare commandsa
                    using (var stmtInsert = connection.Prepare(queryInsert))
                    using (var stmtUpdate = connection.Prepare(queryUpdate))
                    using (var stmtGetprimaryKey = connection.Prepare(querySelectItemPrimaryKeyFromTrackingChangesWithOemID))
                    using (var stmtDeleteItem = connection.Prepare(queryDelete))
                    using (var stmtDeleteItemTracking = connection.Prepare(queryDeleteTracking))
                    using (var stmtTracking = connection.Prepare(queryUpdateTracking))
                    {
                        foreach (var entity in entities)
                        {
                            // Foreach entity check if it's a delete action or un insert/update action
                            if (entity.ServiceMetadata.IsTombstone)
                            {
                                // Delete Action

                                // Bind parameter
                                BindParameter(stmtGetprimaryKey, 1, entity.ServiceMetadata.Id);

                                // Store values of primaryKeys
                                Object[] pkeys = new object[map.PrimaryKeys.Length];

                                // While row is available (only 1 if it's good)
                                while (stmtGetprimaryKey.Step() == SQLiteResult.ROW)
                                {
                                    for (int i = 0; i < pkeys.Length; i++)
                                    {
                                        // Read the column
                                        pkeys[i] = ReadCol(stmtGetprimaryKey, i, map.PrimaryKeys[i].ColumnType);
                                    }

                                }
                                stmtGetprimaryKey.Reset();

                                // Bind parameters
                                for (int i = 0; i < pkeys.Length; i++)
                                {
                                    BindParameter(stmtDeleteItem, i + 1, pkeys[i]);
                                    BindParameter(stmtDeleteItemTracking, i + 1, pkeys[i]);
                                }

                                // Execute the deletion of 2 rows
                                stmtDeleteItem.Step();
                                stmtDeleteItem.Reset();
                                stmtDeleteItem.ClearBindings();

                                stmtDeleteItemTracking.Step();
                                stmtDeleteItemTracking.Reset();
                                stmtDeleteItemTracking.ClearBindings();

                            }
                            else
                            {
                                // Get columns for insert
                                var cols = map.Columns;

                                // Set values for table
                                for (var i = 0; i < cols.Length; i++)
                                {
                                    var val = cols[i].GetValue(entity);
                                    BindParameter(stmtInsert, i + 1, val);
                                    BindParameter(stmtUpdate, i + 1, val);
                                }
                                // add where clause
                                for (var i = 0; i < map.PrimaryKeys.Length; i++)
                                {
                                    var val = map.PrimaryKeys[i].GetValue(entity);
                                    BindParameter(stmtUpdate, cols.Length + i + 1,val);
                                }
                                stmtUpdate.Step();
                                stmtUpdate.Reset();
                                stmtUpdate.ClearBindings();

                                stmtInsert.Step();
                                stmtInsert.Reset();
                                stmtInsert.ClearBindings();

                                // Set Values for tracking table
                                BindParameter(stmtTracking, 1, entity.ServiceMetadata.IsTombstone);
                                BindParameter(stmtTracking, 2, 0);
                                BindParameter(stmtTracking, 3, entity.ServiceMetadata.Id);
                                BindParameter(stmtTracking, 4, "ETag");

                                var editUri = String.Empty;
                                if (entity.ServiceMetadata.EditUri != null &&
                                    entity.ServiceMetadata.EditUri.IsAbsoluteUri)
                                    editUri = entity.ServiceMetadata.EditUri.AbsoluteUri;

                                BindParameter(stmtTracking, 5, editUri);
                                BindParameter(stmtTracking, 6, DateTime.UtcNow);

                                // Set values for tracking table
                                for (var i = 0; i < map.PrimaryKeys.Length; i++)
                                {
                                    var val = map.PrimaryKeys[i].GetValue(entity);
                                    BindParameter(stmtTracking, i + 7, val);
                                }

                                stmtTracking.Step();
                                stmtTracking.Reset();
                                stmtTracking.ClearBindings();

                            }

                        }


                        using (var statement = connection.Prepare("Commit Transaction"))
                        {
                            statement.Step();
                        }
                    }


                }
                catch (Exception ex)
                {
                    using (var statement = connection.Prepare("Rollback Transaction"))
                    {
                        statement.Step();
                    }
                    Debug.WriteLine(ex.Message);
                    throw;
                }

                // Re create Triggers
                this.CreateTriggers(ty, connection);
            }

        }

        /// <summary>
        /// Get total number of changes of sqlite database
        /// </summary>
        /// <param name="schema">All Tables</param>
        /// <param name="lastModifiedDate">Changes since this date</param>
        internal long GetChangeCount(OfflineSchema schema, DateTime lastModifiedDate)
        {
            long totalCount = 0;

            using (SQLiteConnection connection = new SQLiteConnection(localFilePath))
            {
                try
                {
                    foreach (var ty in schema.Collections)
                    {
                        // Get mapping from my type
                        var map = manager.GetMapping(ty);

                        // Create query to select changes 
                        var querySelect = SQLiteConstants.SelectChangeCount;

                        querySelect = String.Format(querySelect, map.TableName);


                        // Prepare command
                        using (var stmt = connection.Prepare(querySelect))
                        {
                            try
                            {
                                // Set Values
                                BindParameter(stmt, 1, lastModifiedDate);

                                stmt.Step();

                                var count = stmt.GetInteger(0);

                                Debug.WriteLine($"Table {map.TableName} has {count} changes");

                                totalCount += count;
                            }
                            finally
                            {
                                stmt.Reset();
                                stmt.ClearBindings();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    throw;
                }
            }
            Debug.WriteLine($"Total change count: {totalCount}");

            return totalCount;
        }

        /// <summary>
        /// Get all changes fro SQLite Database
        /// </summary>
        /// <param name="schema">All Tables</param>
        /// <param name="lastModifiedDate">Changes since this date</param>
        /// <param name="uploadBatchSize">Maximum number of rows to upload</param>
        internal IEnumerable<SQLiteOfflineEntity> GetChanges(OfflineSchema schema, DateTime lastModifiedDate, int uploadBatchSize)
        {
            List<SQLiteOfflineEntity> lstChanges = new List<SQLiteOfflineEntity>();

            using (SQLiteConnection connection = new SQLiteConnection(localFilePath))
            {
                try
                {
                    foreach (var ty in schema.Collections)
                    {
                        // Get mapping from my type
                        var map = manager.GetMapping(ty);

                        // Create query to select changes 
                        var querySelect = SQLiteConstants.SelectChanges;

                        var columnsDcl = new List<String>();
                        var columnsPK = new List<String>();


                        // Foreach columns, create the tsql command to execute
                        foreach (var c in map.Columns)
                        {
                            if (!c.IsPK)
                                columnsDcl.Add("[s].[" + c.Name + "]");

                            // If it's the PK, add it from Tracking (because of deleted items not in real table
                            if (c.IsPK)
                            {
                                columnsDcl.Add("[t].[" + c.Name + "]");
                                columnsPK.Add("[s].[" + c.Name + "] = [t].[" + c.Name + "]");
                            }

                        }

                        var decl = string.Join(",\n", columnsDcl.ToArray());
                        var pk = string.Join(" \nAND ", columnsPK.ToArray());
                        querySelect = String.Format(querySelect, map.TableName, pk, decl);

                        // add limit if specified
                        if (uploadBatchSize > 0)
                            querySelect += $" LIMIT {uploadBatchSize}";

                        // Prepare command
                        using (var stmt = connection.Prepare(querySelect))
                        {
                            try
                            {
                                // Set Values
                                BindParameter(stmt, 1, lastModifiedDate);

                                // Get mapping form the statement
                                var cols = new TableMapping.Column[map.Columns.Length];

                                // Foreach column, get the property in my object
                                for (int i = 0; i < cols.Length; i++)
                                {
                                    var name = stmt.ColumnName(i);
                                    var c = map.FindColumn(name);
                                    if (c != null)
                                        cols[i] = map.FindColumn(name);
                                }

                                // While row is available
                                //while (await stmt.StepAsync().AsTask().ConfigureAwait(false))
                                while (stmt.Step() == SQLiteResult.ROW)
                                {
                                    // Create the object
                                    SQLiteOfflineEntity obj = (SQLiteOfflineEntity)Activator.CreateInstance(map.MappedType);

                                    for (int i = 0; i < cols.Length; i++)
                                    {
                                        if (cols[i] == null)
                                            continue;

                                        // Read the column
                                        var val = ReadCol(stmt, i, cols[i].ColumnType);

                                        // Set the value
                                        cols[i].SetValue(obj, val);
                                    }

                                    // Read the Oem Properties
                                    var newIndex = map.Columns.Count();

                                    obj.ServiceMetadata = new OfflineEntityMetadata();

                                    obj.ServiceMetadata.IsTombstone = (Boolean)ReadCol(stmt, newIndex, typeof(Boolean));
                                    obj.ServiceMetadata.Id = (String)ReadCol(stmt, newIndex + 1, typeof(String));
                                    obj.ServiceMetadata.ETag = (String)ReadCol(stmt, newIndex + 2, typeof(String));
                                    String absoluteUri = (String)ReadCol(stmt, newIndex + 3, typeof(String));
                                    obj.ServiceMetadata.EditUri = String.IsNullOrEmpty(absoluteUri) ? null : new Uri(absoluteUri);

                                    lstChanges.Add(obj);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex.Message);
                                throw;
                            }
                            finally
                            {
                                stmt.Reset();
                                stmt.ClearBindings();
                            }
                        }

                        // if we are batching uploads and the upload rowcount has been reached, skip
                        if (uploadBatchSize > 0 && lstChanges.Count >= uploadBatchSize)
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    throw;
                }

            }

            // if we are batching uploads, limit the in-memory result set as well
            if (uploadBatchSize > 0)
                return lstChanges.Take(uploadBatchSize);

            return lstChanges;
        }


        private static IEnumerable<string> GetOfflineEntityMetadataSQlDecl()
        {
            List<string> columnsDcl = new List<string>
            {
                "\"Oem_AbsoluteUri\" varchar(255) ",
                "\"Oem_IsTombstone\" integer ",
                "\"Oem_IsDirty\" integer ",
                "\"Oem_Id\" varchar(255) ",
                "\"Oem_Etag\" varchar(255) ",
                "\"Oem_EditUri\" varchar(255) ",
                "\"Oem_LastModifiedDate\" datetime "
            };
            return columnsDcl;
        }

        public static object ReadCol(ISQLiteStatement stmt, int index, Type clrType)
        {
            var result = stmt[index];
            if (result == null)
                return null;

            if (clrType == typeof(String))
                return result as string;
            if (clrType == typeof(Int32))
                return Convert.ToInt32(result, CultureInfo.InvariantCulture);
            if (clrType == typeof(Boolean))
                return (Int64)result == 1;
            if (clrType == typeof(Double))
                return Convert.ToDouble(result, CultureInfo.InvariantCulture);
            if (clrType == typeof(Single))
                return Convert.ToSingle(result, CultureInfo.InvariantCulture);
            if (clrType == typeof(DateTime))
                return DateTime.Parse((string)result, CultureInfo.InvariantCulture);
            if (clrType == typeof(DateTimeOffset))
                return DateTime.Parse((string)result, CultureInfo.InvariantCulture);
            if (clrType == typeof(TimeSpan))
                return TimeSpan.FromTicks((Int64)result);
            if (clrType.GetTypeInfo().IsEnum)
                return Convert.ToInt32(result, CultureInfo.InvariantCulture);
            if (clrType == typeof(Int64))
                return (Int64)result;
            if (clrType == typeof(UInt32))
                return Convert.ToUInt32(result, CultureInfo.InvariantCulture);
            if (clrType == typeof(Decimal))
                return Convert.ToDecimal(result, CultureInfo.InvariantCulture);
            if (clrType == typeof(Byte))
                return Convert.ToByte(result, CultureInfo.InvariantCulture);
            if (clrType == typeof(UInt16))
                return Convert.ToUInt16(result, CultureInfo.InvariantCulture);
            if (clrType == typeof(Int16))
                return Convert.ToInt16(result, CultureInfo.InvariantCulture);
            if (clrType == typeof(sbyte))
                return Convert.ToSByte(result, CultureInfo.InvariantCulture);
            if (clrType == typeof(byte[]))
                return (byte[])result;
            if (clrType == typeof(Guid))
                return new Guid((String)result);

            throw new NotSupportedException("Don't know how to read " + clrType);
        }


        internal static void BindParameter(ISQLiteStatement stmt, int index, object value)
        {
            if (value == null)
            {
                stmt.Bind(index, null);
            }
            else if (value is Int32)
            {
                stmt.Bind(index, (int)value);
            }
            else if (value is String)
            {
                stmt.Bind(index, (string)value);
            }
            else if (value is Byte || value is UInt16 || value is SByte || value is Int16)
            {
                stmt.Bind(index, Convert.ToInt32(value, CultureInfo.InvariantCulture));
            }
            else if (value is Boolean)
            {
                stmt.Bind(index, (bool)value ? 1 : 0);
            }
            else if (value is UInt32 || value is Int64)
            {
                stmt.Bind(index, Convert.ToInt64(value, CultureInfo.InvariantCulture));
            }
            else if (value is Single || value is Double || value is Decimal)
            {
                stmt.Bind(index, Convert.ToDouble(value, CultureInfo.InvariantCulture));
            }
            else if (value is DateTimeOffset)
            {
                stmt.Bind(index, ((DateTimeOffset)value).ToString("yyyy-MM-dd HH:mm:ss"));
            }
            else if (value is TimeSpan)
            {
                stmt.Bind(index, ((TimeSpan)value).Ticks);
            }
            else if (value is DateTime)
            {
                stmt.Bind(index, ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss"));
#if !NETFX_CORE
            }
            else if (value.GetType().IsEnum)
            {
#else
            }
            else if (value.GetType().GetTypeInfo().IsEnum)
            {
#endif
                stmt.Bind(index, Convert.ToInt32(value, CultureInfo.InvariantCulture));
            }
            else if (value is byte[])
            {
                var vByte = (byte[])value;
                //var iBuffer = vByte.AsBuffer();

                stmt.Bind(index, vByte);
            }
            else if (value is Guid)
            {
                stmt.Bind(index, ((Guid)value).ToString());
            }
            else
            {
                throw new NotSupportedException("Cannot store type: " + value.GetType());
            }
        }


        private struct IndexedColumn
        {
            public int Order;
            public string ColumnName;
        }

        private struct IndexInfo
        {
            //public string IndexName;
            public string TableName;
            public bool Unique;
            public List<IndexedColumn> Columns;
        }




    }
}
