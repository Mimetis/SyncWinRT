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
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.Synchronization.Services;
using Microsoft.Synchronization.Services.SqlProvider;

namespace Microsoft.Synchronization.Data
{
    // For multi transaction application we will collect incomming
    // datasets with a source knowledge and produce
    // batches with a correctly computed effective knowledge

    internal class RowSorter : IDisposable
    {
        //==================================    
        private bool _disposed = false;
        static private SyncIdComparer _syncIdComparer = new SyncIdComparer();

        // SyncId is stored outside the row
        private class SortedRow
        {
            public DataRowState _rowState;
            public object[] _rowValues;

            public SortedRow(DataRow src, int numCols)
            {
                _rowState = src.RowState;
                _rowValues = new object[numCols];
                DataRowVersion viewVersion =
                    (_rowState == DataRowState.Deleted)
                    ? DataRowVersion.Original
                    : DataRowVersion.Current;
                for (int colIndex = 0; colIndex < _rowValues.Length; colIndex++)
                {
                    object colValue = src[colIndex, viewVersion];
                    if (colValue == System.DBNull.Value)
                    {
                        colValue = null;
                    }
                    else
                    {
                        _rowValues[colIndex] = colValue;
                    }
                }
            }
        }
        private class SortedTable
        {
            public List<string> _idCols;
            public SortedDictionary<SyncId, SortedRow> _rows;
            public DataTable _schema;

            public SortedTable(List<string> idCols)
            {
                _idCols = idCols;
                _rows = null;
                _schema = null;
            }
        }
        //
        // ******** 
        // ******** RowSorter data members
        // ********
        //
        private string _scopeName;
        private long _maxSortedBatchSizeInKB;
        private SyncKnowledge _srcKnowledge;
        // list of tables stored in apply order
        private List<string> _tablesInApplyOrder;
        private Dictionary<string, SortedTable> _sortedTables;

        // need the scopeName for error message
        public RowSorter(SyncKnowledge srcKnowledge,
                          string scopeName,
                          long MaximumSortedBatchSizeInKB)
        {
            _scopeName = scopeName;
            _maxSortedBatchSizeInKB = MaximumSortedBatchSizeInKB;
            _srcKnowledge = srcKnowledge;

            _tablesInApplyOrder = new List<string>();
            _sortedTables = new Dictionary<string, SortedTable>();
        }
        public void Dispose()
        {
            Dispose(true);
            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_sortedTables != null)
                    {
                        _sortedTables = null;
                    }
                }
                // Indicate that the instance has been disposed.
                _sortedTables = null;
                _disposed = true;
            }
        }

        // 
        // ****** Methods for building state of sorter
        // 
        // table must be added in apply order
        // make sure that the tablename we pass in the the remote tablename
        public void AddTable(string tableName, List<string> idCols)
        {
            Debug.Assert(!_sortedTables.ContainsKey(tableName));
            _tablesInApplyOrder.Add(tableName);
            _sortedTables.Add(tableName, new SortedTable(idCols));
        }
        // 
        // ****** Methods for adding unsorted data
        // 
        public void AddUnsortedDataSet(DataSet dataSet)
        {
            // for each table in the DataSet
            foreach (DataTable curTable in dataSet.Tables)
            {
                int numColumns = curTable.Columns.Count;
                // this table should already be in the list of added
                // tables
                SortedTable sortedTable;
                if (!_sortedTables.TryGetValue(curTable.TableName, out sortedTable))
                {
                    SyncTracer.Error("Cannot Apply Changes since Adapters are missing for the following tables: {0}.  " +
                                     "Please ensure that the local and global names on the Adapters are set properly.", curTable);

                    throw new DbSyncException("MissingProviderAdapter");
                }
                if (sortedTable._schema == null)
                {
                    // add a new row storage dictionary and schema if we
                    // need one
                    sortedTable._schema = curTable.Clone();
                    Debug.Assert(sortedTable._schema.DataSet == null);
                    sortedTable._rows = new SortedDictionary<SyncId, SortedRow>(_syncIdComparer);
                }
                if (curTable.Rows.Count == 0) continue;
                object[] idColVals = new object[sortedTable._idCols.Count];
                // for each row
                foreach (DataRow curRow in curTable.Rows)
                {
                    DataRowVersion viewVersion =
                        (curRow.RowState == DataRowState.Deleted)
                        ? DataRowVersion.Original
                        : DataRowVersion.Current;
                    for (int idx = 0; idx < idColVals.Length; idx += 1)
                    {
                        idColVals[idx] = curRow[sortedTable._idCols[idx], viewVersion];
                    }

                    // Add the row to this tables row storage dictionary
                    SyncId curRowId = SyncUtil.InitRowId(curTable.TableName, idColVals);

                    // Note: There is an issue with batching in the provider which causes the same primary key to be repeated across files.
                    // This crashes the .Add call below. To work-around this problem, we need to check if the key already exists.
                    // If it does, then remove it and add it again so that the latest record is always used.

                    if (sortedTable._rows.ContainsKey(curRowId))
                    {
                        sortedTable._rows.Remove(curRowId);
                    }

                    sortedTable._rows.Add(curRowId, new SortedRow(curRow, numColumns));
                }
            }
        }

        public void DoneUnsortedDataSets()
        {
            // not much to do here
        }

        public class SortedBatch
        {
            public DataSet sortedDataSet;
            public SyncKnowledge sortedDataSetKnowledge;

            public SortedBatch()
            {
                sortedDataSet = new DataSet();
                sortedDataSet.Locale = CultureInfo.InvariantCulture;
            }
        }
        //
        // **** Methods for pulling sorted data
        // 
        public IEnumerable<SortedBatch> PullSortedBatches()
        {
            // start the first batch and range
            SortedBatch pendingBatch = new SortedBatch();
            long sizeOfBatch = 0;

            // intialize the range set build because we will be pullig
            // rows now and need to calculate the correct range sets
            BatchRangeSetBuilder rangeSetBuilder = new BatchRangeSetBuilder(_srcKnowledge.GetSyncIdFormatGroup().ItemIdFormat, _tablesInApplyOrder);
            rangeSetBuilder.StartBuildingFirstBatchRangeSet();

            // for each table in apply order
            foreach (string tableName in _tablesInApplyOrder)
            {
                // start the next table range
                rangeSetBuilder.StartNextTable(tableName);

                // if we have a datatable for this name
                SortedTable curTable;
                if (_sortedTables.TryGetValue(tableName, out curTable)
                    && curTable._schema != null)
                {
                    // add the current table to the batch we are working
                    // on
                    DataTable curDataTable = curTable._schema.Clone();
                    pendingBatch.sortedDataSet.Tables.Add(curDataTable);
                    curDataTable.BeginLoadData();

                    // if there are no rows in the table just add it to
                    // the current dataset and move on
                    SyncId maxIdInCurrentTable = null;
                    // pull the rows in SyncId order
                    foreach (KeyValuePair<SyncId, SortedRow> kvp in curTable._rows)
                    {
                        long curRowSize = SyncUtil.GetRowSizeForObjectArray(kvp.Value._rowValues);
                        if (curRowSize > (_maxSortedBatchSizeInKB * 1024))
                        {
                            // Note: This code is modified to throw a more specific exception.
                            // If we end up merging this code with the provider, then the caller has to be tested to 
                            // make sure it works with the logic in the provider codebase.
                            throw SyncServiceException.CreateInternalServerError(
                                String.Format(Strings.RowExceedsConfiguredBatchSize, _maxSortedBatchSizeInKB, tableName, curRowSize));
                        }
                        // fixme: if this row won't fit then return
                        // the current batch
                        if ((sizeOfBatch + curRowSize) > (_maxSortedBatchSizeInKB * 1024))
                        {
                            // * done loading data
                            curDataTable.EndLoadData();
                            // * add last sync id in batch
                            if (maxIdInCurrentTable == null)
                            {
                                // we have not added any rows to the
                                // current table so we should create a
                                // dummy id in the current table for
                                // the range
                                maxIdInCurrentTable = rangeSetBuilder.MakeDummyFirstRowID(tableName);
                            }

                            rangeSetBuilder.AddSyncId(tableName, maxIdInCurrentTable);
                            // start a new batch
                            BatchRangeSet curRS = rangeSetBuilder.FinishBuildingBatchRangeSet();
                            pendingBatch.sortedDataSetKnowledge = curRS.ProjectOnKnowledge(_srcKnowledge);

                            yield return pendingBatch;

                            // *** tricky
                            // after yielding the current batch we
                            // need to start a new one for the rest of
                            // the rows in this table. 
                            // we must reset all the neede state and
                            // this is tricky
                            maxIdInCurrentTable = null;
                            // start a new batch
                            pendingBatch = new SortedBatch();
                            sizeOfBatch = 0;
                            // start a new range after the current one
                            rangeSetBuilder.StartBuildingBatchRangeSet(curRS);
                            // add the current table to the batch we are working
                            // on
                            curDataTable = curTable._schema.Clone();
                            pendingBatch.sortedDataSet.Tables.Add(curDataTable);
                            curDataTable.BeginLoadData();
                        }
                        AddSortedRowToDataTable(curDataTable, kvp.Value);
                        sizeOfBatch += curRowSize;
                        maxIdInCurrentTable = kvp.Key;
                    }
                    curDataTable.EndLoadData();
                }
            }
            // we should always be working on a batch
            {
                Debug.Assert(pendingBatch != null);
                BatchRangeSet curRS = rangeSetBuilder.FinishLastBatchRangeSet();
                pendingBatch.sortedDataSetKnowledge = curRS.ProjectOnKnowledge(_srcKnowledge);
            }
            yield return pendingBatch;
        }


        private void AddSortedRowToDataTable(DataTable curDataTable,
                                              SortedRow sortedRow)
        {
            DataRow row = curDataTable.NewRow();
            int colCount = curDataTable.Columns.Count;

            row.BeginEdit();

            for (int colIndex = 0; colIndex < colCount; colIndex++)
            {
                object curVal = sortedRow._rowValues[colIndex];
                if (curVal == null)
                {
                    row[colIndex] = System.DBNull.Value;
                }
                else
                {
                    row[colIndex] = curVal;
                }
            }

            curDataTable.Rows.Add(row);
            switch (sortedRow._rowState)
            {
                case DataRowState.Unchanged:
                    row.AcceptChanges();
                    break;
                case DataRowState.Added:
                    break;
                case DataRowState.Modified:
                    row.AcceptChanges();
                    row.SetModified();
                    break;
                case DataRowState.Deleted:
                    row.AcceptChanges();
                    row.Delete();
                    break;
                default:
                    Debug.Fail("Invalid row state");
                    break;
            }
            row.EndEdit();
        }

        private class SyncIdComparer : IComparer<SyncId>
        {
            public int Compare(SyncId x, SyncId y)
            {
                return x.CompareTo(y);
            }
        }
    }
}