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
using SQLitePCL.pretty;


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
            
            using (SQLiteDatabaseConnection connection = SQLitePCL.pretty.SQLite3.Open(localFilePath))
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

                    connection.Execute(query);
                    connection.Execute(queryTracking);
                    
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

                        connection.Execute(sql);
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

        private void DisableTriggers(TableMapping map, IDatabaseConnection connection)
        {
            try
            {
                String triggerDeleteQuery = String.Format(SQLiteConstants.DeleteTriggerAfterDelete, map.TableName);
                String triggerInsertQuery = String.Format(SQLiteConstants.DeleteTriggerAfterInsert, map.TableName);
                String triggerUpdateQuery = String.Format(SQLiteConstants.DeleteTriggerAfterUpdate, map.TableName);

                connection.Execute(triggerDeleteQuery);
                connection.Execute(triggerInsertQuery);
                connection.Execute(triggerUpdateQuery);

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

        private void CreateTriggers(Type ty, IDatabaseConnection connection)
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
                connection.Execute(triggerInsertQuery);


                String triggerUpdateQuery =
                    String.Format(SQLiteConstants.CreateTriggerAfterUpdate,
                                  map.TableName, updateOrWherePkeysName);

                connection.Execute(triggerUpdateQuery);

                String triggerDeleteQuery =
                    String.Format(SQLiteConstants.CreateTriggerAfterDelete,
                                  map.TableName, updateOrWherePkeysName);

                connection.Execute(triggerDeleteQuery);
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
            using (SQLiteDatabaseConnection connection = SQLitePCL.pretty.SQLite3.Open(this.localFilePath))
            {
                // Get mapping from my type
                var map = manager.GetMapping(ty);

                var declWhere = map.PrimaryKeys.Select(primaryKey => String.Format("\"{0}\" = ? ", primaryKey.Name)).ToList();
                string updateOrWherePkeysName = String.Join(" and ", declWhere);

                var queryDeleteTracking = String.Format(SQLiteConstants.DeleteTrackingFromChanges,
                    map.TableName, updateOrWherePkeysName);

                connection.RunInTransaction((conn) =>
                {
                    try
                    {
                        foreach (var entity in entities)
                        {
                            conn.Execute(queryDeleteTracking, entity.ServiceMetadata.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        throw;
                    }
                });
            }
        }

        /// <summary>
        /// After the response from server, we need to update IsDirty for all entities
        /// </summary>
        internal void UpdateDirtyTrackingEntities(Type ty, List<SQLiteOfflineEntity> entities)
        {
            using (SQLiteDatabaseConnection connection = SQLitePCL.pretty.SQLite3.Open(localFilePath))
            {
                // Get mapping from my type
                var map = manager.GetMapping(ty);
                var queryUpdateDirtyTracking = String.Format(SQLiteConstants.UpdateDirtyTracking, map.TableName);

                connection.RunInTransaction((conn) =>
                {
                    try
                    {
                        foreach (var entity in entities)
                        {

                            // Set Values for tracking table
                            var parameters = new object[5];
                            parameters[0] = entity.ServiceMetadata.IsTombstone;
                            parameters[1] = 0;
                            parameters[2] = entity.ServiceMetadata.ETag;


                            var editUri = String.Empty;
                            if (entity.ServiceMetadata.EditUri != null && entity.ServiceMetadata.EditUri.IsAbsoluteUri)
                                editUri = entity.ServiceMetadata.EditUri.AbsoluteUri;

                            parameters[3] = editUri;
                            parameters[4] = entity.ServiceMetadata.Id;

                            conn.Execute(queryUpdateDirtyTracking, parameters);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        throw;
                    }
                });
            }
        }

        /// <summary>
        /// Merg entities in SQLite DB
        /// </summary>
        internal void MergeEntities(Type ty, List<SQLiteOfflineEntity> entities)
        {

            using (SQLiteDatabaseConnection connection = SQLitePCL.pretty.SQLite3.Open(this.localFilePath))
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


                connection.RunInTransaction((conn) =>
                {
                    try
                    {
                        // Disable Trigger
                        this.DisableTriggers(map, conn);

                        foreach (var entity in entities)
                        {
                            // Foreach entity check if it's a delete action or un insert/update action
                            if (entity.ServiceMetadata.IsTombstone)
                            {

                                // Store values of primaryKeys
                                Object[] pkeys = new object[map.PrimaryKeys.Length];

                                // While row is available (only 1 if it's good)
                                foreach(var pkRow in conn.Query(querySelectItemPrimaryKeyFromTrackingChangesWithOemID, entity.ServiceMetadata.Id))
                                {
                                    for (int i = 0; i < pkeys.Length; i++)
                                    {
                                        // Read the column
                                        pkeys[i] = ReadCol(pkRow, i, map.PrimaryKeys[i].ColumnType);
                                    }

                                }

                                // delete
                                conn.Execute(queryDelete, pkeys);
                                conn.Execute(queryDeleteTracking, pkeys);
                            }
                            else
                            {
                                // Get columns for insert
                                var cols = map.Columns;

                                // Set values for table
                                var insertParameters = new List<object>();
                                var updateParameters = new List<object>();
                                for (var i = 0; i < cols.Length; i++)
                                {
                                    var val = cols[i].GetValue(entity);
                                    //BindParameter(stmtInsert, i + 1, val);
                                    //BindParameter(stmtUpdate, i + 1, val);
                                    insertParameters.Add(val);
                                    updateParameters.Add(val);
                                }
                                // add where clause
                                for (var i = 0; i < map.PrimaryKeys.Length; i++)
                                {
                                    var val = map.PrimaryKeys[i].GetValue(entity);
                                    //BindParameter(stmtUpdate, cols.Length + i + 1, val);
                                    updateParameters.Add(val);
                                }
                                conn.Execute(queryUpdate, updateParameters.ToArray());
                                conn.Execute(queryInsert, insertParameters.ToArray());


                                // Set Values for tracking table
                                var trackingParameters = new List<object>();
                                trackingParameters.Add(entity.ServiceMetadata.IsTombstone);
                                trackingParameters.Add(0);
                                trackingParameters.Add(entity.ServiceMetadata.Id);
                                trackingParameters.Add("ETag");

                                var editUri = String.Empty;
                                if (entity.ServiceMetadata.EditUri != null &&
                                    entity.ServiceMetadata.EditUri.IsAbsoluteUri)
                                    editUri = entity.ServiceMetadata.EditUri.AbsoluteUri;

                                trackingParameters.Add(editUri);
                                trackingParameters.Add(DateTime.UtcNow);

                                // Set values for tracking table
                                for (var i = 0; i < map.PrimaryKeys.Length; i++)
                                {
                                    var val = map.PrimaryKeys[i].GetValue(entity);
                                    trackingParameters.Add(val);
                                }

                                conn.Execute(queryUpdateTracking, trackingParameters.ToArray());
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        throw;
                    }

                    // Re create Triggers
                    this.CreateTriggers(ty, conn);
                });
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

            using (SQLiteDatabaseConnection connection = SQLitePCL.pretty.SQLite3.Open(localFilePath))
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

                        var count = connection.Query(querySelect).SelectScalarInt().First();

                        Debug.WriteLine($"Table {map.TableName} has {count} changes");

                        totalCount += count;
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

            using (SQLiteDatabaseConnection connection = SQLitePCL.pretty.SQLite3.Open(localFilePath))
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
                        
                        try
                        {
                            // Get mapping form the statement
                            var cols = new TableMapping.Column[map.Columns.Length];
                            bool firstRow = true;

                            // While row is available
                            foreach(var row in connection.Query(querySelect, lastModifiedDate))
                            {
                                if (firstRow)
                                {
                                    // Foreach column, get the property in my object
                                    for (int i = 0; i < cols.Length; i++)
                                    {
                                        var name = row[i].ColumnInfo.Name;
                                        var c = map.FindColumn(name);
                                        if (c != null)
                                            cols[i] = map.FindColumn(name);
                                    }

                                    firstRow = false;
                                }

                                // Create the object
                                SQLiteOfflineEntity obj = (SQLiteOfflineEntity)Activator.CreateInstance(map.MappedType);

                                for (int i = 0; i < cols.Length; i++)
                                {
                                    if (cols[i] == null)
                                        continue;

                                    // Read the column
                                    var val = ReadCol(row, i, cols[i].ColumnType);

                                    // Set the value
                                    cols[i].SetValue(obj, val);
                                }

                                // Read the Oem Properties
                                var newIndex = map.Columns.Count();

                                obj.ServiceMetadata = new OfflineEntityMetadata();

                                obj.ServiceMetadata.IsTombstone = row[newIndex].ToBool(); //ReadCol(stmt, newIndex, typeof(Boolean));
                                obj.ServiceMetadata.Id = row[newIndex + 1].ToString(); //(String)ReadCol(stmt, newIndex + 1, typeof(String));
                                obj.ServiceMetadata.ETag = row[newIndex + 2].ToString(); //(String)ReadCol(stmt, newIndex + 2, typeof(String));
                                String absoluteUri = row[newIndex + 3].ToString(); //(String)ReadCol(stmt, newIndex + 3, typeof(String));
                                obj.ServiceMetadata.EditUri = String.IsNullOrEmpty(absoluteUri) ? null : new Uri(absoluteUri);

                                lstChanges.Add(obj);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            throw;
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

        public static object ReadCol(IReadOnlyList<IResultSetValue> stmt, int index, Type clrType)
        {
            var result = stmt[index];
            if (result == null)
                return null;

            if (clrType == typeof(String))
                return result.ToString(); //result as string;
            if (clrType == typeof(Int32))
                return result.ToInt(); //Convert.ToInt32(result, CultureInfo.InvariantCulture);
            if (clrType == typeof(Boolean))
                return result.ToBool(); //(Int64)result == 1;
            if (clrType == typeof(Double))
                return result.ToDouble(); //Convert.ToDouble(result, CultureInfo.InvariantCulture);
            if (clrType == typeof(Single))
                return result.ToFloat(); //Convert.ToSingle(result, CultureInfo.InvariantCulture);
            if (clrType == typeof(DateTime))
                return DateTime.Parse(result.ToString(), CultureInfo.InvariantCulture);
            if (clrType == typeof(DateTimeOffset))
                return DateTime.Parse(result.ToString(), CultureInfo.InvariantCulture);
            if (clrType == typeof(TimeSpan))
                return TimeSpan.FromTicks(result.ToInt64());
            if (clrType.GetTypeInfo().IsEnum)
                return result.ToInt(); //Convert.ToInt32(result, CultureInfo.InvariantCulture);
            if (clrType == typeof(Int64))
                return result.ToInt64(); //(Int64)result;
            if (clrType == typeof(UInt32))
                return result.ToUInt32(); //Convert.ToUInt32(result, CultureInfo.InvariantCulture);
            if (clrType == typeof(Decimal))
                return result.ToDecimal(); //Convert.ToDecimal(result, CultureInfo.InvariantCulture);
            if (clrType == typeof(Byte))
                return result.ToByte(); //Convert.ToByte(result, CultureInfo.InvariantCulture);
            if (clrType == typeof(UInt16))
                return result.ToUInt16(); //Convert.ToUInt16(result, CultureInfo.InvariantCulture);
            if (clrType == typeof(Int16))
                return result.ToInt(); //Convert.ToInt16(result, CultureInfo.InvariantCulture);
            if (clrType == typeof(sbyte))
                return result.ToSByte(); //Convert.ToSByte(result, CultureInfo.InvariantCulture);
            if (clrType == typeof(byte[]))
                return result.ToBlob(); //[])result;
            if (clrType == typeof(Guid))
                return new Guid(result.ToString());

            throw new NotSupportedException("Don't know how to read " + clrType);
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
