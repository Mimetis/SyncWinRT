// Copyright © Microsoft Corporation. All rights reserved.

// Microsoft Limited Permissive License (Ms-LPL)

// This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.

// 1. Definitions
// The terms “reproduce,” “reproduction,” “derivative works,” and “distribution” have the same meaning here as under U.S. copyright law.
// A “contribution” is the original software, or any additions or changes to the software.
// A “contributor” is any person that distributes its contribution under this license.
// “Licensed patents” are a contributor’s patent claims that read directly on its contribution.

// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any contributors’ name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
// (E) The software is licensed “as-is.” You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.
// (F) Platform Limitation- The licenses granted in sections 2(A) & 2(B) extend only to the software or derivative works that you create that run on a Microsoft Windows operating system product.

using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using Microsoft.Synchronization.Data;
using System.Diagnostics;
using System.Linq;
using Microsoft.Synchronization.Services.Formatters;
using System.Text;
using System.Globalization;

namespace Microsoft.Synchronization.Services.SqlProvider
{
    /// <summary>
    /// Converts DataSet to IOfflineEntity list and vice versa.
    /// </summary>
    internal sealed class DataSetToEntitiesConverter
    {
        #region Private Members

        private readonly Dictionary<string, Type> _tableGlobalNameToTypeMapping;
        private readonly Dictionary<Type, string> _typeToTableGlobalNameMapping;
        private readonly Dictionary<Type, string> _typeToTableLocalNameMapping;

        /// <summary>
        /// The _localToGlobalPropertyMapping and _globalToLocalPropertyMapping holds the localname - globalName
        /// mapping info for a given IOfflineEntity. If an IOfflineEntity has any SyncEntityPropertyMappingAttribute 
        /// annotating its properties then they will contain the mapping else this will be always empty dictionary. 
        /// LocalName represents the backend store name for that particular field/column
        /// GlobalName represents the actual property name which is being referenced on the wire.
        /// </summary>
        private readonly Dictionary<Type, Dictionary<string, string>> _localToGlobalPropertyMapping;
        private readonly Dictionary<Type, Dictionary<string, string>> _globalToLocalPropertyMapping;
        private static readonly Type _mappingAttrType = typeof(SyncEntityPropertyMappingAttribute);

        private const string SelectFromTableFormat = "select {0} from {1} WHERE {2}";

        #endregion

        #region Constructor

        internal DataSetToEntitiesConverter(Dictionary<string, Type> tableGlobalNameToTypeMapping, 
                                            Dictionary<Type, string> typeToTableGlobalNameMapping, 
                                            Dictionary<Type, string> typeToTableLocalNameMapping)
        {
            _tableGlobalNameToTypeMapping = tableGlobalNameToTypeMapping;
            _typeToTableGlobalNameMapping = typeToTableGlobalNameMapping;
            _typeToTableLocalNameMapping = typeToTableLocalNameMapping;

            _localToGlobalPropertyMapping = new Dictionary<Type, Dictionary<string, string>>();
            _globalToLocalPropertyMapping = new Dictionary<Type, Dictionary<string, string>>();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Convert from a list of OfflineCapableEntities to a dataset.
        /// </summary>
        /// <param name="entities">Entity list</param>
        /// <returns>Dataset which contains all data from the entity list.</returns>
        /// <exception cref="SyncServiceException">For an unknown type that is passed in the input.</exception>
        internal DataSet ConvertEntitiesToDataSet(List<IOfflineEntity> entities)
        {
            var resultSet = new DataSet();

            foreach (var entity in entities)
            {
                if (!_typeToTableGlobalNameMapping.ContainsKey(entity.GetType()))
                {
                    throw SyncServiceException.CreateInternalServerError(
                        String.Format(CultureInfo.InvariantCulture, 
                                      "DataSetToEntitiesConverter.ConvertEntitiesToDataSet: Unknown type {0}", 
                                      entity.GetType()));
                }

                BuildPropertyMappingInfo(entity.GetType());

                string tableName = _typeToTableGlobalNameMapping[entity.GetType()];

                AddEntityToDataSet(entity, resultSet, tableName);
            }

            return resultSet;
        }

        /// <summary>
        /// Utility function to get the SELECT command for a given entity type.
        /// This will return a command of format SELECT [Fieldname1, FieldName2] from [TableName] WHERE PK1 = @PK1 [AND PK2= @pk2]
        /// where FieldNameN is the actual columns included in the scope
        /// </summary>
        /// <param name="t">Entity type</param>
        /// <returns>TSQL Select command text</returns>
        internal string GetSelectScriptForType(Type t)
        {
            if (!_typeToTableGlobalNameMapping.ContainsKey(t))
            {
                throw SyncServiceException.CreateInternalServerError(
                    String.Format(CultureInfo.InvariantCulture, "DataSetToEntitiesConverter.GetSelectScriptForType: Unknown type {0}", t));
            }

            if (!this._globalToLocalPropertyMapping.ContainsKey(t))
            {
                BuildPropertyMappingInfo(t);
            }

            Dictionary<string, string> mappingInfo = this._globalToLocalPropertyMapping[t];
            
            string delimiter = string.Empty;
            StringBuilder colsList = new StringBuilder();
           
            // Build cols list
            foreach (PropertyInfo pinfo in ReflectionUtility.GetPropertyInfoMapping(t))
            {
                colsList.Append(delimiter).Append((mappingInfo.ContainsKey(pinfo.Name)) ? mappingInfo[pinfo.Name] : pinfo.Name);
                if(string.IsNullOrEmpty(delimiter))
                {
                    delimiter = ", ";
                }
            }

            delimiter = string.Empty;
            StringBuilder pkList = new StringBuilder();

            int index = 1;

            // Build primary keys list
            foreach (PropertyInfo pinfo in ReflectionUtility.GetPrimaryKeysPropertyInfoMapping(t))
            {
                pkList.Append(delimiter).Append((mappingInfo.ContainsKey(pinfo.Name)) ? mappingInfo[pinfo.Name] : pinfo.Name);
                pkList.Append(" = @p").Append(index++);
                if (string.IsNullOrEmpty(delimiter))
                {
                    delimiter = " AND ";
                }
            }

            // Get the table local name.
            string tableName = _typeToTableLocalNameMapping[t];
            
            return string.Format(CultureInfo.InvariantCulture, SelectFromTableFormat, colsList, tableName, pkList);
        }

        /// <summary>
        /// Utility to get tablename for a given entity type
        /// </summary>
        /// <param name="t">Entity type</param>
        /// <returns>Table name</returns>
        internal string GetTableNameForType(Type t)
        {
            return this._typeToTableGlobalNameMapping[t];
        }

        /// <summary>
        /// Copies the individual properties from the entity back in to the DataTable's first row.
        /// This should be used only when merging a user conflict resolution back in to a DataRow.
        /// This returns the merged results as an object array
        /// </summary>
        /// <param name="entity">Entity from which to read values</param>
        /// <param name="table">The Table whose first DataRow will be updated.</param>
        /// <returns>The contents of the DataRow as an object array</returns>
        internal object[] CopyEntityToDataRow(IOfflineEntity entity, DataTable table)
        {
            Debug.Assert(table.Rows.Count == 1, "table.Rows.Count ==1");
            if (table.Rows.Count != 1)
            {
                throw new InvalidOperationException("Cannot copy Entity to a DataTable whose row count != 1");
            }

            Dictionary<string, string> mappingInfo = _localToGlobalPropertyMapping[entity.GetType()];

            // Check for tombstones
            bool isRowDeleted = false;
            if (table.Rows[0].RowState == DataRowState.Deleted)
            {
                isRowDeleted = true;
                table.Rows[0].RejectChanges();
            }


            // Retrieve the current row values
            object[] rowValues = table.Rows[0].ItemArray;
            PropertyInfo[] properties = entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < table.Columns.Count; i++)
            {
                // Iterate over each non sync column and read its value from the entity
                if (IsSyncSpecificColumn(table.Columns[i].ColumnName))
                {
                    continue;
                }

                // Read the value of the property
                string columnName = null;
                mappingInfo.TryGetValue(table.Columns[i].ColumnName, out columnName);
                columnName = columnName ?? table.Columns[i].ColumnName;
         
                // Retrieve the PropertyInfo
                PropertyInfo info = properties.Where(e => e.Name.Equals(columnName, StringComparison.Ordinal)).FirstOrDefault();

                Debug.Assert(info != null, "PropertyInfo is not null.");
                // Get the property's value and put it in the object array
                rowValues[i] = info.GetValue(entity, null) ?? DBNull.Value;
            }

            // Write the row values back to the DataRow
            table.Rows[0].ItemArray = rowValues;

            if (isRowDeleted)
            {
                table.Rows[0].Delete();
            }
            return rowValues;
        }

        /// <summary>
        /// This method will try to locate the passed in DataRow's key values in the destination DataTable
        /// and if found will update its values with the new values passed.
        /// </summary>
        /// <param name="destinationTable">Table to make the merge in</param>
        /// <param name="rowToMatch">Row whose keys to match in destination table</param>
        /// <param name="rowvalues">Values to override</param>
        /// <param name="entityType">Entity type for the DataRow</param>
        internal void MergeChangeInToDataSet(DataTable destinationTable, DataRow rowToMatch, object[] rowvalues, Type entityType)
        {
            object[] primaryKeyColumns = new object[destinationTable.PrimaryKey.Length];
            int index = 0;

            bool isRowToMatchDeleted = false;
            if (rowToMatch.RowState == DataRowState.Deleted)
            {
                isRowToMatchDeleted = true;
                rowToMatch.RejectChanges();
            }

            // Find all PrimaryKey column indexes
            foreach (DataColumn pkColumn in destinationTable.PrimaryKey)
            {
                primaryKeyColumns[index++] = rowToMatch[pkColumn.ColumnName];
            }

            // Find the row in DestinationTable
            DataRow rowToModify = destinationTable.Rows.Find(primaryKeyColumns);

            // Check for tombstones
            bool isRowDeleted = false;

            if (rowToModify.RowState == DataRowState.Deleted)
            {
                isRowDeleted = true;
                rowToModify.RejectChanges();
            }

            // Suppress DataRow RowChanging events
            // Note: Call BeginEdit only after check for Deleted row state, 
            // otherwise this call will crash.
            rowToModify.BeginEdit();

            Debug.Assert(rowToModify != null);
            rowToModify.ItemArray = rowvalues;

            // Reset rowstates
            if (isRowDeleted)
            {
                rowToModify.Delete();
            }
            if (isRowToMatchDeleted)
            {
                rowToMatch.Delete();
            }

            rowToModify.EndEdit();
        }

        /// <summary>
        /// Convert a dataset to a list of OfflineCapableEntities.
        /// </summary>
        /// <param name="dataSet">DataSet that contains entity information.</param>
        /// <returns>List of OfflineCapabeEntities that contain the information from the dataset.</returns>
        internal List<IOfflineEntity> ConvertDataSetToEntities(DataSet dataSet)
        {
            var entities = new List<IOfflineEntity>();

            foreach (DataTable table in dataSet.Tables)
            {
                string tableName = table.TableName; //Note: Do not ToLower() as this is case sensitive.
                if (!_tableGlobalNameToTypeMapping.ContainsKey(tableName))
                {
                    throw SyncServiceException.CreateInternalServerError(
                        String.Format(CultureInfo.InvariantCulture, "DataSetToEntitiesConverter.ConvertDataSetToEntities: Unknown type {0}", tableName));
                }

                Type entityType = _tableGlobalNameToTypeMapping[tableName];

                BuildPropertyMappingInfo(entityType);
                
                ConstructorInfo constructorInfo = entityType.GetConstructor(Type.EmptyTypes);

                foreach (DataRow row  in table.Rows)
                {
                    var entity = constructorInfo.Invoke(null) as IOfflineEntity;

                    GetEntityFromDataRow(table.Columns, row, entity);

                    entities.Add(entity);
                }
            }

            return entities;
        }

        internal void GetEntityFromDataRow(DataColumnCollection columnCollection, DataRow row, IOfflineEntity objectToConvert)
        {
            Type t = objectToConvert.GetType();
            Dictionary<string, string> mappingInfo = _localToGlobalPropertyMapping[t];

            bool isDeleted = false;
            if (row.RowState == DataRowState.Deleted)
            {
                isDeleted = true;
                row.RejectChanges();
            }

            // Note: Call BeginEdit only after check for Deleted row state, 
            // otherwise this call will crash.
            row.BeginEdit();

            for (Int32 i = 0; i <= columnCollection.Count - 1; i++)
            {
                if (IsSyncSpecificColumn(columnCollection[i].ColumnName))
                {
                    continue;
                }

                //NOTE: the datarow column names must match exactly (including case) to the IOfflineEntity's property names
                object columnValue = row[columnCollection[i].ColumnName];

                if (DBNull.Value != columnValue)
                {
                    t.InvokeMember((mappingInfo.ContainsKey(columnCollection[i].ColumnName)) 
                                        ? mappingInfo[columnCollection[i].ColumnName]  
                                        : columnCollection[i].ColumnName,
                                   BindingFlags.SetProperty, null,
                                   objectToConvert,
                                   new[] {columnValue});
                }
            }

            if (isDeleted)
            {
                row.Delete();

                // Mark the IsTombstone field if the RowState was deleted.
                objectToConvert.ServiceMetadata.IsTombstone = true;
            }

            row.EndEdit();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Inspects a type to see if it contains any SyncEntityPropertyMappingAttribute custom attribute
        /// and if present builds a localToGlobal and globalToLocal name mappings.
        /// </summary>
        /// <param name="type"></param>
        private void BuildPropertyMappingInfo(Type type)
        {
            // Check to see if the list has already been built.
            if (_localToGlobalPropertyMapping.ContainsKey(type))
            {
                return;
            }

            _localToGlobalPropertyMapping.Add(type, new Dictionary<string, string>());
            _globalToLocalPropertyMapping.Add(type, new Dictionary<string, string>());

            // Loop over each Property and see if the attribute is specified.
            foreach (PropertyInfo pinfo in type.GetProperties())
            {
                object[] attrs = pinfo.GetCustomAttributes(_mappingAttrType, true);
                if (attrs.Length > 0)
                {
                    SyncEntityPropertyMappingAttribute mappingAttr = attrs[0] as SyncEntityPropertyMappingAttribute;

                    if (string.IsNullOrEmpty(mappingAttr.LocalName))
                    {
                        throw new InvalidOperationException(
                            string.Format(CultureInfo.InvariantCulture, "No LocalName parameter was provided in SyncEntityPropertyMappingAttribute on Property '{0}' in Type '{1}'",
                                pinfo.Name, type.FullName));
                    }

                    // Add Local To Global mapping
                    _localToGlobalPropertyMapping[type].Add(mappingAttr.LocalName, pinfo.Name);

                    // Add Global to Local mapping
                    _globalToLocalPropertyMapping[type].Add(pinfo.Name, mappingAttr.LocalName);
                }
            }
        }
        
        /// <summary>
        /// Checks if the given column name was created by the SqlSyncProvider.
        /// </summary>
        /// <param name="columnName">Column name to check</param>
        /// <returns>True - if sync created column, false - otherwise.</returns>
        private static bool IsSyncSpecificColumn(string columnName)
        {
            switch (columnName)
            {
                case DbSyncSession.SyncCreatePeerKey:
                case DbSyncSession.SyncCreatePeerTimestamp:
                case DbSyncSession.SyncUpdatePeerKey:
                case DbSyncSession.SyncUpdatePeerTimestamp:
                    return true;
            }

            return false;
        }

        //Note: Removed ref here
        private void AddEntityToDataSet(IOfflineEntity objectToRead, DataSet dataSet, string tableName)
        {
            Type t = objectToRead.GetType();
            PropertyInfo[] properties = t.GetProperties();

            Dictionary<string, string> globalToLocalMappingInfo = _globalToLocalPropertyMapping[t];

            Dictionary<string, string> localToGlobalMappingInfo = _localToGlobalPropertyMapping[t];

            // We need to create the table if it doesn't already exist
            DataTable dataTable = dataSet.Tables[tableName];

            if (dataTable == null)
            {            
                dataSet.Tables.Add(tableName);

                dataTable = dataSet.Tables[tableName];

                // Create the columns of the table based off the 
                // properties we reflected from the type
                foreach (PropertyInfo property in properties)
                {
                    // Do not add service related properties.
                    if (IsOfflineEntityServiceProperty(property.PropertyType))
                    {
                        continue;
                    }

                    Type type = property.PropertyType;
                    if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        type = property.PropertyType.GetGenericArguments()[0];
                    }

                    if (globalToLocalMappingInfo.ContainsKey(property.Name))
                    {
                        dataTable.Columns.Add(globalToLocalMappingInfo[property.Name], type);
                    }
                    else
                    {
                        dataTable.Columns.Add(property.Name, type);
                    }
                }

                // SQL Provider does not set primary keys on DataTable. So set Primary Keys if its not set
                ReflectionUtility.SetDataTablePrimaryKeys(dataTable, objectToRead.GetType(), globalToLocalMappingInfo);
            }

            dataTable.BeginLoadData();

            // Now the table should exist so add records to it.
            var columnArray = new object[dataTable.Columns.Count];

            for (int i = 0; i <= columnArray.Length - 1; i++)
            {
                // The property name to be set on the IOfflineEntity is the ColumnName unless there is a
                // SyncEntityPropertyMappingAttribute for the property in which case the name of that property is used instead.
                string colName = dataTable.Columns[i].ColumnName;
                if (localToGlobalMappingInfo.ContainsKey(colName))
                {
                    colName = localToGlobalMappingInfo[colName];
                }

                if (objectToRead.ServiceMetadata.IsTombstone && !dataTable.PrimaryKey.Contains(dataTable.Columns[i]))
                {
                    columnArray[i] = DBNull.Value;
                }
                else
                {
                    columnArray[i] = t.InvokeMember(colName,
                                                BindingFlags.GetProperty, null,
                                                objectToRead, new object[0]);   
                }
            }

            // Add the row to the table in the dataset
            DataRow row = dataTable.LoadDataRow(columnArray, true);

            // If the entity is a tombstone, set the DataRowState property by calling the Delete or SetAdded method.
            if (objectToRead.ServiceMetadata.IsTombstone)
            {
                row.Delete();
            }
            else if (!String.IsNullOrEmpty(objectToRead.ServiceMetadata.Id))
            {
                row.SetModified();
            }
            else
            {
                row.SetAdded();
            }

            dataTable.EndLoadData();
        }

        /// <summary>
        /// Checks if the property is a sync service specific property.
        /// For example: the OfflineEntityMetadata type is used to store metadata about each entity
        /// and is not a part of the underlying datastore.
        /// </summary>
        /// <param name="propertyType">Type of the property</param>
        /// <returns>True - if property is service specific, False - otherwise.</returns>
        private static bool IsOfflineEntityServiceProperty(Type propertyType)
        {
            return (propertyType == typeof (OfflineEntityMetadata));
        }

        #endregion
    }
}
