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
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Synchronization.Services.SqlProvider;

namespace Microsoft.Synchronization.Data
{
    // These classes hold enough information to synthesize the SyncId
    // values that we need to create ranges in the knowledge that
    // will collapse into a scope level range at the end of the
    // synchronization.
    // *****************************************************************
    // For SyncIds in the same table we should create ranges like this:
    // Assume: 
    //       - SID1 was largest Sync ID in the previous application batch
    //       - Current batch min SyncId == SID2
    //       - Current batch max SyncId == SID3
    //       - Table(SID1)--Table(SID2)--Table(SID3)
    //       - SID1<SID2<SID3
    // Range for current batch = SID1+1, SID3
    // *****************************************************************
    // For last batch and first batch in a table we need to include
    // SyncId space outside the table range.
    // Assume: 
    //       - three tables Tab1 -- Tab2 -- Tab3
    //       - SyncId(Tab1) < SyncId(Tab2) < SyncId(Tab3)
    //       - SyncId(Tab1) = SyncId( "Table1Name" + "-")
    //       - SyncId(Tab1)+1
    //           = (byte[])SyncId(Tab1) with (byte)0 appended
    // Range for Tab1 = SyncId(Zero), SyncId(Tab2)
    // Range for Tab2 = SyncId(Tab2)+1,SyncId(Tab3)
    // Range for Tab3 = SyncId(Tab3)+1, SyncId(Infinity)
    // *****************************************************************
    // The Range of a batch from the start of Tab1 to SID1 in Tab1
    // would be:
    //      SyncId(Zero), SID1
    // *****************************************************************
    // The Range of a batch from the SID1 in Tab1 to SID3 in Tab3 and
    // missing Tab2 would be:
    //      (SID1, SyncId(Tab2))  + (SyncId(Tab3)+1, SyncId(SID3)) 
    internal class BatchRange
    {
        // each range has one and only one table associated per range set
        public string TableName;
        // start should never by null
        public SyncId Start;
        // End of null indicates that a new table was started but no
        // SyncIds where added, in that case this is not really a
        // range just a marker that the next SyncId belongs to a new table
        public SyncId End;

        public BatchRange() {}

        public BatchRange( BatchRange src ) 
        {
            TableName = src.TableName;
            Start     = src.Start;
            End       = src.End;
        }

        public bool RangeIsUsable 
        {
            get 
            { 
                return !(String.IsNullOrEmpty(TableName)
                       ||(Start == null)
                       ||(End == null));
            }
        }
    }

    internal class BatchRangeSet
    {
        private List<BatchRange> _ranges = new List<BatchRange>();
        
        public SyncKnowledge ProjectOnKnowledge( SyncKnowledge sourceKnowledge )
        {
            SyncKnowledge cumulativeKnowledge = null;

            foreach( BatchRange curRange in _ranges ) {
                if( !curRange.RangeIsUsable ) {
                    // break on last range if it is not usable
                    Debug.Assert( curRange == Last );
                    break; 
                }
                SyncKnowledge knowledgeForUnion = 
                    sourceKnowledge.GetKnowledgeForRange(curRange.Start, curRange.End);
                
                if( cumulativeKnowledge == null ) {
                    cumulativeKnowledge = knowledgeForUnion;
                } else {
                    cumulativeKnowledge.Combine( knowledgeForUnion );
                }
            }
            return cumulativeKnowledge;
        }
        public void Add( BatchRange newRange )
        {
            Debug.Assert( (Count == 0)||Last.RangeIsUsable );
            _ranges.Add( newRange );
        }
        public BatchRange Last
        {
            get { return _ranges[_ranges.Count-1]; }
        }
        public int Count
        {
            get { return _ranges.Count; }
        }
    }
    
    internal class BatchRangeSetBuilder
    {
        enum BuilderInternalErrors 
        {
            UnfinishedRangeSetInProgress = 0,
            IncorrectTablePassedInForItem = 1
        }
        // ***********************************
        // ****** state that persists across BatchRangeSets
        // ***********************************
        private SyncIdFormat _idFormat;
        // this keeps a set of precalculated start and end SyncIds for
        // each table that take into account the tables relative
        // ordering with other tables. If all the tableRanges are
        // combinded the effect is one range from 0 to infinity
        private Dictionary<string, BatchRange> _tableRanges;

        // ***********************************
        // ****** per BatchRangeSet state
        // ***********************************
        private BatchRangeSet _inProgressRS;

        /// <summary>
        /// Creates a Range set build and pre calculates the effective
        /// tables ranges for use later when building a BatchRangeSet.
        /// </summary>
        /// <param name="idFormat"> 
        /// The id format for the SyncIds
        /// </param>
        /// <param name="tableNames"> 
        /// All the table names in that will be in the sum of the
        /// batches. Order does not matter.
        /// </param>
        public BatchRangeSetBuilder( SyncIdFormat idFormat, List<string> tableNames )
        {
            _tableRanges = new Dictionary<string, BatchRange>( tableNames.Count );
            _idFormat = idFormat;

            int numTables = tableNames.Count;
            SortedList<SyncId,string> tablesSortedBySyncId 
                = new SortedList<SyncId,string>(numTables);

            List<object> emptyPkVals = new List<object>(0);

            foreach( string curTable in tableNames ) 
            {
                SyncId idBeforeTable = SyncUtil.InitRowId( curTable, emptyPkVals );
                tablesSortedBySyncId.Add( idBeforeTable, curTable );
            }

            // for each table we need to determine the:
            // - starting SyncId (zero or first id in table)
            // - ending SyncId (just before next table or infinity)
            BatchRange prevTableRange = null;
            
            foreach( KeyValuePair<SyncId,string> curElem in tablesSortedBySyncId ) 
            {
                BatchRange curTableRange = new BatchRange();
                curTableRange.TableName = curElem.Value;
                // assume this is the last table and then fix this if
                // there is another table
                curTableRange.End = _idFormat.Infinity;
                if( prevTableRange == null ) 
                {
                    // first table starts at zero
                    curTableRange.Start = _idFormat.Zero;
                } 
                else 
                {
                    // fix up prev end to be correct
                    // set it's end to be one before current tables
                    // starting SyncId
                    prevTableRange.End = curElem.Key;
                    curTableRange.Start = IdPlusOne( _idFormat, curElem.Key );
                }
                prevTableRange = curTableRange;
                _tableRanges.Add( curTableRange.TableName, curTableRange );
            }
        }

        /// <summary>
        /// Start building state for a BatchRangeSet.
        /// call sequence should be:
        ///      StartBuildingBatchRangeSet( prevRs )
        ///      AddSyncId(Sid)
        ///      ...
        ///      AddSyncId(Sid)
        ///      StartNextTable( tname ) 
        ///      AddSyncId(Sid)
        ///      result = FinishBuildingBatchRangeSet()
        ///
        /// Note that this implicitly starts the range with the end of
        /// prevRS+1
        /// ***** We don't track the prevRS internally because we want to be able
        /// ***** to rebuild a rangeset from scratch if we for
        /// ***** instance change the batching limits
        /// </summary>
        /// <param name="prevRS"> 
        /// The prev rangeset that was built.
        /// </param>
        public void StartBuildingBatchRangeSet( BatchRangeSet prevRS )
        {
            Debug.Assert( _inProgressRS == null );

            BatchRange prevLast = prevRS.Last;
            _inProgressRS = new BatchRangeSet();

            // check that this rangeset was not built as the last
            // rangeset for a knowledge. 
            Debug.Assert( prevLast.End != _tableRanges[prevLast.TableName].End );

            // was the end of the last range usable?
            if( !prevLast.RangeIsUsable ) 
            {
                Debug.Assert( prevLast.TableName != null );
                Debug.Assert( prevLast.Start != null );
                // if the prev range moved to the current table but did
                // not have any rows then we are starting the table again.
                // just take the unusable range as our own
                _inProgressRS.Add(new BatchRange(prevLast));
            }
            else 
            {
                // We have a started the current table already 
                // So continue from one greater than the last range
                BatchRange nextRange = new BatchRange();
                nextRange.TableName = prevLast.TableName;
                nextRange.Start = IdPlusOne( _idFormat, prevLast.End );
                // leave End as null
                _inProgressRS.Add( nextRange );
            }
        }
        /// <summary>
        /// Start building state for a BatchRangeSet.
        /// call sequence should be:
        ///      StartBuildingFirstBatchRangeSet( )
        ///      AddSyncId(Sid)
        ///      ...
        ///      AddSyncId(Sid)
        ///      StartNextTable( tname ) 
        ///      AddSyncId(Sid)
        ///      result = FinishBuildingBatchRangeSet()
        /// Note that this implicetly starts the range with 
        /// </summary>
        public void StartBuildingFirstBatchRangeSet()
        {
            // not valid to start a new range set while one is in progress
            Debug.Assert( _inProgressRS == null );
            _inProgressRS = new BatchRangeSet();
        }
        /// <summary>
        /// Returns the range set that we are using and resets the in
        /// progress state.
        /// </summary>
        public BatchRangeSet FinishBuildingBatchRangeSet()
        {
            Debug.Assert( _inProgressRS != null );
            // check if the in progress range set is valid
            if( _inProgressRS.Count == 1 && !_inProgressRS.Last.RangeIsUsable ) {
                // there where no tables finished or SyncIds added to
                // this range set!

                throw new DbSyncException("InteralBatchRangeSetError");
            }
            BatchRangeSet retSet = _inProgressRS;
            _inProgressRS = null;
            return retSet;
        }
        /// <summary>
        /// Aborts the partially build range set if it exists.
        /// </summary>
        public void AbortRangeSet()
        {
            _inProgressRS = null;
        }
        /// <summary>
        /// This ends the current table when there are no more tables
        /// add to ranges and returns the range set. The last range
        /// set can not be used to start building a new range set
        /// </summary>
        public BatchRangeSet FinishLastBatchRangeSet()
        {
            Debug.Assert( _inProgressRS != null );
            // fill in the end value for the current table.
            // return range set
            _inProgressRS.Last.End = _tableRanges[_inProgressRS.Last.TableName].End;
            return FinishBuildingBatchRangeSet();
        }
        /// <summary>
        /// End the range for the current table and starts the next table.
        /// </summary>
        /// <param name="tableName"> 
        /// Name of the next table.
        /// </param>
        public void StartNextTable( string tableName )
        {
            Debug.Assert( _inProgressRS != null );
            if( _inProgressRS.Count > 0 ) {
                if( _inProgressRS.Last.TableName.Equals( tableName ) ) {
                    return; // no effect if the table name matches
                }
                // fill in the end value for the current table.
                _inProgressRS.Last.End = _tableRanges[_inProgressRS.Last.TableName].End;
            }
            // add a new unusable range for the new table. 
            AppendNewTableRange( tableName );
        }
        /// <summary>
        /// This adds a SyncId as the maximum SyncId for the current table.
        /// </summary>
        /// <param name="tableName"> The name of the table which holds
        /// the SyncId</param>
        /// <param name="maxSyncIdInCurrentTable"> The new end SyncId</param>
        public void AddSyncId( string tableName, SyncId maxSyncIdInCurrentTable )
        {
            Debug.Assert( _inProgressRS != null );

            string currentTableName = _inProgressRS.Last.TableName;

            if( !currentTableName.Equals( tableName ) ) {

                throw new DbSyncException("InteralBatchRangeSetError");
            }

            Debug.Assert( maxSyncIdInCurrentTable > _tableRanges[currentTableName].Start );
            Debug.Assert( maxSyncIdInCurrentTable < _tableRanges[currentTableName].End );
            Debug.Assert( maxSyncIdInCurrentTable > _inProgressRS.Last.End );
            // Set this SyncId as the max in the current range
            _inProgressRS.Last.End = maxSyncIdInCurrentTable;
        }

        
        // adds range for given table to current range set
        private void AppendNewTableRange( string tableName )
        {
            // take the predetermined start range for the current
            // table and leave the current range in an unusable state
            // until the first row is added or this table ends
            BatchRange nextRange = new BatchRange();
            nextRange.TableName = tableName;
            nextRange.Start = _tableRanges[tableName].Start;
            _inProgressRS.Add( nextRange );
        }
        
        /// <summary>
        /// Creates syncid plus one. This method matches the method in
        /// ./sync/src/xproc/knowledge/SyncId.cpp -> SyncId::InitializeByIncrement()
        /// </summary>
        /// <param name="idFormat"> 
        /// The item id format information.
        /// </param>
        /// <param name="origId"> 
        /// The SyncId that we want the "plus one" for.
        /// </param>
        static public SyncId IdPlusOne( SyncIdFormat idFormat, SyncId origId )
        {
            Debug.Assert( origId.IsVariableLength == idFormat.IsVariableLength );

            byte[] origBytes = origId.RawId;
            int idSize; 
            byte[] idBytes;

            // first figure out what the length of the new id should
            // be in bytes.
            if (!idFormat.IsVariableLength)
            {
                idSize = origBytes.Length;
            }
            else if (origBytes.Length < idFormat.Length)
            {
                // we'll just append a 0 in this case
                idSize = origBytes.Length + 1;
            }
            else
            {
                // if 0xFF at the end will turn into 0s; we'll want to drop them
                for (idSize = origBytes.Length; 
                     0xFF == origBytes[idSize - 1]; 
                     --idSize);
            }

            idBytes = new byte[idSize];
            Array.Copy(origBytes, 
                       idBytes, 
                       Math.Min( origBytes.Length, idBytes.Length));
            
            if (idFormat.IsVariableLength)
            {
                if (origBytes.Length < idFormat.Length)
                {
                    // just append 0
                    idBytes[idSize - 1] = 0;
                }
                else
                {
                    idBytes[idSize - 1] += 1;
                }
            }
            else 
            {
                // if we are fixed length
                // increment LSB and carry if needed
                for( int cur_index = idSize-1; ;cur_index -= 1 ) 
                {
                    if( idBytes[cur_index] == 0xFF ) 
                    {
                        idBytes[cur_index] = 0;
                    } else {
                        idBytes[cur_index] += 1;
                        break;
                    }
                }
            }
            return new SyncId( idBytes, idFormat.IsVariableLength );
        }

        public SyncId MakeDummyFirstRowID(string tableName)
        {
            return IdPlusOne(_idFormat, _tableRanges[tableName].Start);
        }
    }
}
