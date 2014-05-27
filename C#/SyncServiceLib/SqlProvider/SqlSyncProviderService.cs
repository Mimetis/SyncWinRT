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
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServer;
using Microsoft.Synchronization.Services.Batching;
using System.IO;
using System.Globalization;
using Microsoft.Synchronization.Services.Formatters;

namespace Microsoft.Synchronization.Services.SqlProvider
{
    /// <summary>
    /// Implements the GetChanges and ApplyChanges method using the SqlSyncProvider.
    /// </summary>
    internal sealed class SqlSyncProviderService : IAsymmetricProviderService
    {
        #region Private Members

        private readonly string _serverConnectionString;
        private readonly string _scopeName;
        private readonly ConflictResolutionPolicy _conflictResolutionPolicy;
        private SqlSyncProvider _sqlSyncProvider;
        private readonly SyncServiceConfiguration _configuration;
        private List<SyncConflict> _conflicts;
        private List<SyncError> _syncErrors;
        private readonly DataSetToEntitiesConverter _converter;
        private string _clientScopeName;
        private SyncId _clientSyncId;
        private readonly List<SqlSyncProviderFilterParameterInfo> _filterParams;
        private readonly IBatchHandler _batchHandler;
        private SyncConflictContext _conflictContext = null;
        private SqlConnection _readConnection;
        private SqlTransaction _readTransaction;

        /// <summary>
        /// This dictionary is used to save the SyncId of conflicting rows. 
        /// We use it after applying all changes (after all ApplyChangeFailed events have been fired and handled)
        /// to add positive exceptions to the client knowledge. This will avoid sending the change
        /// back to the client on the next download changes request.
        /// </summary>
        private readonly Dictionary<SyncConflict, SyncId> _conflictToSyncEntityIdMapping = new Dictionary<SyncConflict, SyncId>();

        /// <summary>
        /// Contains the mapping between the entity SyncId (for all conflicts detected in change application) and the SyncConflict instances
        /// This is used for fast lookup based on the SyncId.
        /// There is a 1:1 mapping between this member variable and _conflictToSyncEntityIdMapping.
        /// </summary>
        private readonly Dictionary<SyncId, SyncConflict> _syncEntityIdToConflictMapping = new Dictionary<SyncId, SyncConflict>();

        /// <summary>
        /// This variable will contain the value of @@DBTS or get_new_rowversion (for SQL Azure) and is used to create knowledge
        /// for individual entities that caused conflicts.
        /// </summary>
        private ulong _serverTickCountAfterResolvingAllConflicts;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of the <see cref="SqlSyncProviderService" /> class. Also uses the batch handler passed as parameter if it is not null.
        /// </summary>
        /// <param name="configuration">Sync configuration</param>
        /// <param name="serverScope">Server scope/template</param>
        /// <param name="filterParams">Filter parameters. Pass null for no filter parameters.</param>
        /// <param name="operationContext">SyncOperationContext object to create the SyncConflictContext object.</param>
        /// <param name="batchHandler">Batch Handler for spooling and retrieving batches.</param>
        internal SqlSyncProviderService(SyncServiceConfiguration configuration, string serverScope, List<SqlSyncProviderFilterParameterInfo> filterParams,
                                        SyncOperationContext operationContext, IBatchHandler batchHandler)
        {
            WebUtil.CheckArgumentNull(serverScope, "serverScope");

            _configuration = configuration;
            _serverConnectionString = _configuration.ServerConnectionString;
            _scopeName = serverScope;
            _conflictResolutionPolicy = _configuration.ConflictResolutionPolicy;

            _filterParams = filterParams;

            _converter = new DataSetToEntitiesConverter(_configuration.TableGlobalNameToTypeMapping,
                                                        _configuration.TypeToTableGlobalNameMapping,
                                                        _configuration.TypeToTableLocalNameMapping);

            _batchHandler = batchHandler ?? new FileBasedBatchHandler(_configuration.BatchSpoolDirectory);
            
            if (operationContext != null)
            {
                _conflictContext = new SyncConflictContext()
                {
                    ScopeName = serverScope,
                    Operation = SyncOperations.Upload,
                    RequestHeaders = operationContext.RequestHeaders,
                    ResponseHeaders = operationContext.ResponseHeaders,
                    QueryString = operationContext.QueryString
                };
            }
        }

        /// <summary>
        /// Constructor that uses the FileBasedBatchHandler.
        /// </summary>
        /// <param name="configuration">Sync configuration</param>
        /// <param name="serverScope">Server scope/template</param>
        /// <param name="filterParams">Filter parameters. Pass null for no filter parameters.</param>
        /// <param name="operationContext">SyncOperationContext object to create the SyncConflictContext object.</param>
        internal SqlSyncProviderService(SyncServiceConfiguration configuration, string serverScope, List<SqlSyncProviderFilterParameterInfo> filterParams,
                                        SyncOperationContext operationContext) : this(configuration, serverScope, filterParams, operationContext, null)
        {
        }

        #endregion

        #region Internal Delegates
        internal Action<IOfflineEntity> ApplyClientChangeFailed;

        /// <summary>
        /// This method is used to retrieve the current data stored for a Entity in the database. This is used by the runtime to 
        /// detect the current values for a row that is being skipped due to data errors.
        /// </summary>
        /// <param name="entities">Entities whose current server version is required</param>
        /// <returns>Server version or null if entity doesnt exist in database.</returns>
        internal List<IOfflineEntity> GetCurrentServerVersionForEntities(IEnumerable<IOfflineEntity> entities)
        {
            using (_readConnection = new SqlConnection(_serverConnectionString))
            {
                _readConnection.Open();

                _readTransaction = _readConnection.BeginTransaction(IsolationLevel.Snapshot);

                List<IOfflineEntity> result = GetCurrentServerVersionForEntities(entities, _readConnection, _readTransaction);

                _readTransaction.Commit();

                return result;
            }
        }

        /// <summary>
        /// This method is used to retrieve the current data stored for a Entity in the database. This is used by the runtime to 
        /// detect the current values for a row that is being skipped due to data errors.
        /// </summary>
        /// <param name="entities">Entities whose current server version is required</param>
        /// <param name="connection">SqlConnection to use for reading from the database</param>
        /// <param name="transaction">SqlTransaction to use when reading from the database</param>
        /// <returns>Server version or null if entity doesnt exist in database.</returns>
        internal List<IOfflineEntity> GetCurrentServerVersionForEntities(IEnumerable<IOfflineEntity> entities, SqlConnection connection, SqlTransaction transaction)
        {
            var serverVersions = new List<IOfflineEntity>();

            foreach (IOfflineEntity entity in entities)
            {
                // Craft the command for retrieving the row.
                SqlCommand command = new SqlCommand(_converter.GetSelectScriptForType(entity.GetType()), connection, transaction);

                int index = 1;
                // Set parameters
                foreach (PropertyInfo pinfo in ReflectionUtility.GetPrimaryKeysPropertyInfoMapping(entity.GetType())
                    )
                {
                    SqlParameter p = new SqlParameter("@p" + index, pinfo.GetValue(entity, null));
                    index++;
                    command.Parameters.Add(p);
                }

                DataSet ds = new DataSet();
                using (SqlDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow))
                {
                    if (reader.HasRows)
                    {
                        ds.Load(reader, LoadOption.OverwriteChanges, new string[] {_converter.GetTableNameForType(entity.GetType())});

                        serverVersions.Add(_converter.ConvertDataSetToEntities(ds).First());
                    }
                    else
                    {
                        serverVersions.Add(null);
                    }
                }
            }

            return serverVersions;
        }

        #endregion

        #region IAsymmetricProviderService Members

        /// <summary>
        /// Gets the next batch of changes for a client.
        /// </summary>
        /// <param name="serverBlob">Client knowledge as byte[]</param>
        /// <param name="batchCode">batchcode for the batch</param>
        /// <param name="nextBatchSequenceNumber">Sequence number of the next batch</param>
        /// <returns>Response containing the new knowledge and the list of changes.</returns>
        private GetChangesResponse GetChanges(byte[] serverBlob, Guid batchCode, Guid nextBatchSequenceNumber)
        {
            WebUtil.CheckArgumentNull(serverBlob, "clientKnowledgeBlob");

            // Get the next batch using the batch handler implementation.
            Batch batch = _batchHandler.GetNextBatch(batchCode, nextBatchSequenceNumber);

            if (null == batch)
            {
                // Since we did'nt get a batch, default to the full get changes call.
                return GetChanges(serverBlob);
            }

            // Intialize a SqlSyncProvider object.
            _sqlSyncProvider = CreateSqlSyncProviderInstance(_clientScopeName, _serverConnectionString, _configuration.SyncObjectSchema);

            SyncKnowledge clientKnowledge = GetSyncKnowledgeFromBlob(serverBlob);

            List<IOfflineEntity> entities = _converter.ConvertDataSetToEntities(batch.Data);

            //Only combine the knowledge of this batch.
            clientKnowledge.Combine(SyncKnowledge.Deserialize(_sqlSyncProvider.IdFormats, batch.LearnedKnowledge));

            var syncBlob = new SyncBlob
                               {
                                   ClientScopeName = _clientSyncId.GetGuidId().ToString(),
                                   ClientKnowledge = clientKnowledge.Serialize(),
                                   BatchCode = batch.BatchCode,
                                   IsLastBatch = batch.IsLastBatch,
                                   NextBatch = batch.NextBatch
                               };

            // Save data in the response object.
            var response = new GetChangesResponse
                               {
                                   EntityList = entities,
                                   IsLastBatch = batch.IsLastBatch,
                                   ServerBlob = syncBlob.Serialize()
                               };

            return response;
        }

        /// <summary>
        /// Get changes for a client using the knowledge that is passed in.
        /// </summary>
        /// <param name="serverBlob">Client knowledge as byte[]</param>
        /// <returns>Response containing the new knowledge and the list of changes.</returns>
        public GetChangesResponse GetChanges(byte[] serverBlob)
        {
            bool isNewClient = false;
            var response = new GetChangesResponse();

            var syncBlob = new SyncBlob();
            byte[] clientKnowledgeBlob = null;

            // If the incoming knowledge blob is null, then we need to initialize a new scope
            // for this request. 
            if (null == serverBlob || 0 == serverBlob.Length)
            {
                // Create a new Guid and use that as the client Id.
                Guid clientId = Guid.NewGuid();

                _clientScopeName = String.Format(CultureInfo.InvariantCulture, "{0}_{1}", _scopeName, clientId);

                _clientSyncId = new SyncId(clientId);

                CreateNewScopeForClient();

                isNewClient = true;

                syncBlob.ClientScopeName = clientId.ToString();
            }
            else
            {
                SyncBlob incomingBlob = SyncBlob.DeSerialize(serverBlob);

                PopulateClientScopeNameAndSyncId(incomingBlob);

                syncBlob.ClientScopeName = incomingBlob.ClientScopeName;

                clientKnowledgeBlob = incomingBlob.ClientKnowledge;

                if (null != incomingBlob.BatchCode && null != incomingBlob.NextBatch)
                {
                    // This is a batched request, so handle it separately.
                    return GetChanges(incomingBlob.ClientKnowledge, incomingBlob.BatchCode.Value, incomingBlob.NextBatch.Value);
                }
            }

            // Intialize a SqlSyncProvider object.
            _sqlSyncProvider = CreateSqlSyncProviderInstance(_clientScopeName, _serverConnectionString, _configuration.SyncObjectSchema);

            var sessionContext = new SyncSessionContext(_sqlSyncProvider.IdFormats, new SyncCallbacks());

            _sqlSyncProvider.BeginSession(SyncProviderPosition.Remote, sessionContext);

            try
            {
                // Get the SyncKnowledge from the blob. If the blob is null, initialize a default SyncKnowledge object.
                SyncKnowledge clientKnowledge = GetSyncKnowledgeFromBlob(clientKnowledgeBlob);

                DbSyncContext dbSyncContext;

                uint changeBatchSize = (_configuration.IsBatchingEnabled)
                                           ? (uint)_configuration.DownloadBatchSizeInKB
                                           : 0;

                RowSorter rowSorter = null;

                do
                {
                    object changeDataRetriever;

                    // Get the next batch.
                    _sqlSyncProvider.GetChangeBatch(changeBatchSize, clientKnowledge,
                                                    out changeDataRetriever);

                    dbSyncContext = (DbSyncContext)changeDataRetriever;

                    // Only initialize the RowSorter, if the data is batched.
                    if (null == rowSorter && _configuration.IsBatchingEnabled)
                    {
                        // Clone the client knowledge.
                        var clonedClientKnowledge = clientKnowledge.Clone();

                        // Combine with the MadeWithKnowledge of the server.
                        clonedClientKnowledge.Combine(dbSyncContext.MadeWithKnowledge);

                        // Use the new knowledge and get and instance of the RowSorter class.
                        rowSorter = GetRowSorter(clonedClientKnowledge);
                    }

                    // Remove version information from the result dataset.
                    RemoveSyncVersionColumns(dbSyncContext.DataSet);

                    // For a new client, we don't want to send tombstones. This will reduce amount of data
                    // transferred and the client doesn't care about tombstones anyways.
                    if (isNewClient)
                    {
                        RemoveTombstoneRowsFromDataSet(dbSyncContext.DataSet);
                    }

                    // Add the dataset to the row sorter. Only use this if batching is enabled.
                    if (_configuration.IsBatchingEnabled)
                    {
                        rowSorter.AddUnsortedDataSet(dbSyncContext.DataSet);

                        // Delete the batch file generated by the provider, since we have read it.
                        // Otherwise we will keep accumulating files which are not needed.
                        if (!String.IsNullOrEmpty(dbSyncContext.BatchFileName) && File.Exists(dbSyncContext.BatchFileName))
                        {
                            File.Delete(dbSyncContext.BatchFileName);
                        }
                    }

                } while (!dbSyncContext.IsLastBatch && dbSyncContext.IsDataBatched);

                List<IOfflineEntity> entities;

                if (_configuration.IsBatchingEnabled)
                {
                    // If batching is enabled.
                    Batch batch = SaveBatchesAndReturnFirstBatch(rowSorter);

                    if (null == batch)
                    {
                        entities = new List<IOfflineEntity>();
                    }
                    else
                    {
                        // Conver to to entities.
                        entities = _converter.ConvertDataSetToEntities(batch.Data);

                        //Only combine the knowledge of this batch.
                        clientKnowledge.Combine(SyncKnowledge.Deserialize(_sqlSyncProvider.IdFormats,
                                                                          batch.LearnedKnowledge));

                        response.IsLastBatch = batch.IsLastBatch;
                        syncBlob.IsLastBatch = batch.IsLastBatch;

                        if (batch.IsLastBatch)
                        {
                            syncBlob.NextBatch = null;
                            syncBlob.BatchCode = null;
                        }
                        else
                        {
                            syncBlob.NextBatch = batch.NextBatch;
                            syncBlob.BatchCode = batch.BatchCode;
                        }
                    }
                }
                else
                {
                    // No batching.
                    response.IsLastBatch = true;

                    entities = _converter.ConvertDataSetToEntities(dbSyncContext.DataSet);

                    // combine the client and the server knowledge.
                    // the server may have an updated knowledge from the last time the client sync'd.
                    clientKnowledge.Combine(dbSyncContext.MadeWithKnowledge);
                }

                // Save data in the response object.
                syncBlob.ClientKnowledge = clientKnowledge.Serialize();

                response.ServerBlob = syncBlob.Serialize();
                response.EntityList = entities;
            }
            finally
            {
                _sqlSyncProvider.EndSession(sessionContext);
            }

            return response;
        }

        /// <summary>
        /// Apply changes sent by a client to the server.
        /// </summary>
        /// <param name="serverBlob">Blob sent in the incoming request</param>
        /// <param name="entities">Changes from the client</param>
        /// <returns>Response containing the new knowledge and conflict/error information.</returns>
        public ApplyChangesResponse ApplyChanges(byte[] serverBlob, List<IOfflineEntity> entities)
        {
            WebUtil.CheckArgumentNull(serverBlob, "serverBlob");
            WebUtil.CheckArgumentNull(entities, "entities");
            
            if (0 == serverBlob.Length)
            {
                throw new InvalidOperationException("serverBlob is empty");
            }

            var syncBlob = new SyncBlob();

            SyncBlob incomingBlob = SyncBlob.DeSerialize(serverBlob);

            PopulateClientScopeNameAndSyncId(incomingBlob);
            
            // Set the scope name in the response blob.
            syncBlob.ClientScopeName = incomingBlob.ClientScopeName;
            
            // If the requested scope does not exists, then throw an error since we 
            // don't initialize scopes on upload requests.
            if (!CheckIfScopeExists())
            {
                throw SyncServiceException.CreateResourceNotFound("Scope does not exist");
            }

            byte[] clientKnowledgeBlob = incomingBlob.ClientKnowledge;

            // Initialize a SqlSyncProvider object.
            _sqlSyncProvider = CreateSqlSyncProviderInstance(_clientScopeName, _serverConnectionString, _configuration.SyncObjectSchema);

            var response = new ApplyChangesResponse();

            // Deserialize the knowledge or create new empty knowledge.
            SyncKnowledge clientKnowledge = GetSyncKnowledgeFromBlob(clientKnowledgeBlob);

            // If there are no entities to upload, then return the client knowledge as is.
            if (entities.Count == 0)
            {
                response.Conflicts = new List<SyncConflict>();
                response.Errors = new List<SyncError>();
                
                syncBlob.ClientKnowledge = clientKnowledge.Serialize();

                response.ServerBlob = syncBlob.Serialize();

                return response;
            }

            // Client never has any forgotten knowledge. So create a new one.
            var forgottenKnowledge = new ForgottenKnowledge(_sqlSyncProvider.IdFormats, clientKnowledge);

            // Convert the entities to dataset using the custom converter.
            DataSet changesDS = _converter.ConvertEntitiesToDataSet(entities);

            var stats = new SyncSessionStatistics();
            var sessionContext = new SyncSessionContext(_sqlSyncProvider.IdFormats, new SyncCallbacks());

            _sqlSyncProvider.BeginSession(SyncProviderPosition.Remote, sessionContext);

            ulong tickCount = 0;
            SyncKnowledge updatedClientKnowldege;

            try
            {
                uint batchSize;
                SyncKnowledge serverKnowledge;

                // This gives us the server knowledge.
                _sqlSyncProvider.GetSyncBatchParameters(out batchSize, out serverKnowledge);

                var changeBatch = new ChangeBatch(_sqlSyncProvider.IdFormats, clientKnowledge, forgottenKnowledge);
                changeBatch.SetLastBatch();

                //Note: There is a possiblity of (-ve) item exceptions , between two uploads from the 
                // same client (for example: in case of RI failures). This would result in an incorrect value if the function
                // FindMinTickCountForReplica is used to get the last tickcount. So, we need to ignore the -ve item exceptions 
                // when finding the tickcount for the client replica from the server knowledge.

                /* Logic:
                 * SyncKnowledge.GetKnowledgeForItemId could be used for itemid Zero and then we can find the mintickcount for client replica id.
                 * This does not however seem to work, so we use the KnowledgeInspector and enumerate over each ClockVector
                 * and find the client clockvector and get its tickcount.
                 * 
                 * Assumption: The above approach assumes that we don't have any positive exceptions in the knowledge.
                 */
                try
                {
                    // Check if the client replica key exists.
                    uint clientReplicaKey = serverKnowledge.ReplicaKeyMap.LookupReplicaKey(_clientSyncId);

                    var ki = new KnowledgeInspector(1, serverKnowledge);
                    var clockVector = (ClockVector)ki.ScopeClockVector;
                    int noOfReplicaKeys = clockVector.Count;

                    for (int i = noOfReplicaKeys - 1; i >= 0; i--)
                    {
                        if (clockVector[i].ReplicaKey == clientReplicaKey)
                        {
                            tickCount = clockVector[i].TickCount;
                            break;
                        }
                    }
                }
                catch (ReplicaNotFoundException exception)
                {
                    SyncTracer.Info("ReplicaNotFoundException. NEW CLIENT. Exception details: {0}",
                                    WebUtil.GetExceptionMessage(exception));
                    // If the knowedge does not contain the client replica (first apply), initialize tickcount to zero.
                    tickCount = 0;
                }

                // Increment the tickcount
                tickCount++;

                // update the made with knowledge to include the new tickcount.
                updatedClientKnowldege = new SyncKnowledge(_sqlSyncProvider.IdFormats, _clientSyncId, tickCount);
                updatedClientKnowldege.Combine(clientKnowledge);

                // The incoming data does not have metadata for each item, so we need to create it at this point.
                AddSyncColumnsToDataSet(changesDS, tickCount);

                // Make DbSyncContext
                var dbSyncContext = new DbSyncContext
                {
                    IsDataBatched = false,
                    IsLastBatch = true,
                    DataSet = changesDS,
                    MadeWithKnowledge = updatedClientKnowldege,
                    MadeWithForgottenKnowledge = forgottenKnowledge,
                    ScopeProgress = new DbSyncScopeProgress()
                };

                _conflicts = new List<SyncConflict>();
                _syncErrors = new List<SyncError>();

                // Subscribe to the ApplyChangeFailed event to handle conflicts.
                _sqlSyncProvider.ApplyChangeFailed += SqlSyncProviderApplyChangeFailed;

                // Subscribe to the ChangesApplied event to read the server tickcount incase there are any conflicts.
                _sqlSyncProvider.ChangesApplied += SqlSyncProviderChangesApplied;

                //NOTE: The ConflictResolutionPolicy pass into the method is IGNORED.
                // Conflicts can be logged by subscribing to the failed events
                _sqlSyncProvider.ProcessChangeBatch(Microsoft.Synchronization.ConflictResolutionPolicy.DestinationWins,
                                                   changeBatch,
                                                   dbSyncContext, new SyncCallbacks(), stats);

                if (0 != _conflicts.Count)
                {
                    _sqlSyncProvider.GetSyncBatchParameters(out batchSize, out serverKnowledge);

                    // The way the current P2P provider works, versions are bumped up when conflicts are resolved on the server.
                    // This would result in us sending the changes to the client on the next download request. We want
                    // to not enumerate that change again on the next request from the same client. 
                    // The solution is to get the server knowledge after all changes are applied and then
                    // project the knowledge of each conflictign item and add it as a positive exception to the updated client knowledge.

                    AddConflictItemsKnowledgeToClientKnowledge(updatedClientKnowldege, serverKnowledge);
                }
            }
            finally
            {
                _sqlSyncProvider.EndSession(sessionContext);
            }

            // Don't send any updates to the server knowledge since the client has not got any updates yet.
            // This updated knowledge will only include an update to the client tickcount.
            // The client would obtain the server knowledge when it does a get changes.
            // If we include the serverknowlege, the client would never get any items that are
            // between the current server knowledge and the client known server knowledge.

            syncBlob.ClientKnowledge = updatedClientKnowldege.Serialize();
            response.ServerBlob = syncBlob.Serialize();

            response.Conflicts = _conflicts;
            response.Errors = _syncErrors;

            return response;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Get a new instance of the RowSorter class using the knowledge passed as a parameter. 
        /// This is used to pull out sorted batches with learned knowledge to send to the client.
        /// </summary>
        /// <param name="clientKnowledge">Knowledge to initialize the RowSorter instance.</param>
        /// <returns>An instance of the RowSorter class.</returns>
        private RowSorter GetRowSorter(SyncKnowledge clientKnowledge)
        {
            DbSyncScopeDescription scopeDescription = GetScopeDescription();
            Debug.Assert(null != scopeDescription);

            /* Note: The RowSorter class and dependencies are copied over from the 2.1 providers. 
             * If an when we decide to make this assembly a friend of the provider assembly,
             * we can remove the local copy of the source code. 
            */

            // Initialize an instance of the RowSorter class. 
            var rowSorter = new RowSorter(clientKnowledge, _clientScopeName, _configuration.DownloadBatchSizeInKB.Value);

            // Add all the tables to the RowSorter instance.
            foreach (DbSyncTableDescription table in scopeDescription.Tables)
            {
                // Get primary keys.
                List<string> pkColumns = table.PkColumns.Select(c => c.UnquotedName).ToList();

                // Set up the tables in the RowSorter class. 
                // The DataTable names will be equal to the UnquotedGlobalName // property value.
                rowSorter.AddTable(table.UnquotedGlobalName, pkColumns);
            }

            return rowSorter;
        }

        /// <summary>
        /// Pull out sorted batches from the RowSorter instance and save them using the IBatchHandler implementation.
        /// Also return the first batch as an output.
        /// </summary>
        /// <param name="rowSorter">RowSorter instance.</param>
        /// <returns>The first batch pulled from the RowSorter.</returns>
        private Batch SaveBatchesAndReturnFirstBatch(RowSorter rowSorter)
        {
            // Get an enumerator over the batch list.
            IEnumerable<RowSorter.SortedBatch> batchEnumerator = rowSorter.PullSortedBatches();

            // Prepare a batch header instance. The BatchCode will be the folder name under which we will write files corresponding
            // to each sorted batch. Every batch will have a knowledge that contains the information in the batch. So we have to combine 
            // the existing client knowledge with the batch knowledge before returning it back to the caller.
            var header = new BatchHeader
            {
                BatchCode = Guid.NewGuid(), // New batch code.
                BatchFileNames = new List<string>()
            };

            var batchList = new List<Batch>();

            Guid batchName = Guid.NewGuid();
            Guid nextBatchName = Guid.NewGuid();

            foreach (var sortedBatch in batchEnumerator)
            {
                // Create a new batch instance. 
                var b = new Batch
                {
                    BatchCode = header.BatchCode,
                    FileName = batchName.ToString(), // Random file name
                    LearnedKnowledge = sortedBatch.sortedDataSetKnowledge.Serialize(),
                    Data = sortedBatch.sortedDataSet,
                    ClientScopeName = _clientScopeName,
                    NextBatch = nextBatchName
                };

                header.BatchFileNames.Add(b.FileName);
                batchList.Add(b);

                // Set the batch names.
                batchName = nextBatchName;
                nextBatchName = Guid.NewGuid();
            }

            // If there are no batches, just return.
            if (0 == batchList.Count)
            {
                return null;
            }

            // Set properties for the last batch.
            batchList[batchList.Count - 1].IsLastBatch = true;
            batchList[batchList.Count - 1].NextBatch = null;

            // Only save if we have more than 1 batch.
            if (batchList.Count > 1)
            {
                // Save all but the first batch, since we are sending that to the client as a response to the current request.
                _batchHandler.SaveBatches(batchList.Skip(1).ToList(), header);
            }

            // Return the first batch.
            return batchList[0];
        }

        /// <summary>
        /// Deserialize the blob passed in as a parameter or create new knowledge if the blob is null.
        /// </summary>
        /// <param name="clientKnowledgeBlob">Serialized knowledge blob</param>
        /// <returns>New SyncKnowledge instance if the blob is null or a deserialized instance of the blob.</returns>
        private SyncKnowledge GetSyncKnowledgeFromBlob(byte[] clientKnowledgeBlob)
        {
            if (null == clientKnowledgeBlob)
            {
                return new SyncKnowledge(_sqlSyncProvider.IdFormats, _clientSyncId, 0);
            }

            return SyncKnowledge.Deserialize(_sqlSyncProvider.IdFormats, clientKnowledgeBlob);
        }

        /// <summary>
        /// Handler for the ChangesApplied event of the SqlSyncProvider class. This event is raised after the changes are 
        /// applied but before the SqlTransaction is committed. We use this event to read the server timestamp (@@DBTS/get_new_rowversion())
        /// and use this to generate positive exceptions in the knowledge that is sent back in the response.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SqlSyncProviderChangesApplied(object sender, DbChangesAppliedEventArgs e)
        {
            Debug.Assert(e.Connection is SqlConnection, "Connection is not of type SqlConnection");
            Debug.Assert(null != e.Transaction, "Transaction is null");
            Debug.Assert(e.Transaction is SqlTransaction, "Transaction is not of type SqlTransaction");

            if (0 != _conflicts.Count)
            {
                _serverTickCountAfterResolvingAllConflicts =
                    Microsoft.Synchronization.Services.SqlProvider.SqlEditionHelper.GetServerTickCountFromDatabase(e.Connection as SqlConnection, e.Transaction as SqlTransaction);
            }
        }

        /// <summary>
        /// Handler for the ApplyChangedFailed event of the SqlSyncProvider class. This is used to record
        /// conflict information and apply the service conflict resolution policy.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event args</param>
        private void SqlSyncProviderApplyChangeFailed(object sender, DbApplyChangeFailedEventArgs e)
        {
            ApplyAction applyAction = ApplyAction.Continue;

            // Note: LocalChange table name may be null if the record does not exist on the server. So use the remote table name.
            string tableName = e.Conflict.RemoteChange.TableName;

            Type entityType = _configuration.TableGlobalNameToTypeMapping[tableName];

            ConstructorInfo constructorInfo = entityType.GetConstructor(Type.EmptyTypes);
            
            // Handle Errors first
            if (null != e.Error)
            {
                var syncError = new SyncError
                                    {
                                        LiveEntity = (IOfflineEntity)constructorInfo.Invoke(null),
                                        ErrorEntity = (IOfflineEntity)constructorInfo.Invoke(null)
                                    };

                //Note: When Error is not null, the conflict type should be ErrorsOccurred. Assert, just to make sure this is always correct.
                Debug.Assert(e.Conflict.Type == DbConflictType.ErrorsOccurred, "Conflict.Type is not ErrorsOccurred.");

                syncError.Description = e.Error.Message;

                // Fill in the error entity. This is the value of the client entity.
                _converter.GetEntityFromDataRow(e.Conflict.RemoteChange.Columns, e.Conflict.RemoteChange.Rows[0], syncError.ErrorEntity);

                // Mark the error entity as a tombstone, if the client sent a delete.
                // Note: The DataRow.RowState property is not marked as deleted, so we cannot use that
                // to determine if the client change was a delete.
                if (e.Conflict.Stage == DbSyncStage.ApplyingDeletes)
                {
                    syncError.ErrorEntity.ServiceMetadata.IsTombstone = true;
                }

                // Get the current version from the server.
                IOfflineEntity serverVersion = GetCurrentServerVersionForEntities(
                                                                new List<IOfflineEntity> { syncError.ErrorEntity },
                                                                (SqlConnection)e.Connection,
                                                                (SqlTransaction)e.Transaction)
                                                                .FirstOrDefault();

                // There is no item corresponding to the item sent from the client.
                // Example is an INSERT which caused an RI error and server does not have the record.
                if (null == serverVersion)
                {
                    // If there is no server record and the client changes is not a tombstone, then 
                    // set the LiveEntity as a compensating action which is a tombstone.
                    // This means that the client has to apply the delete locally
                    // for data convergence.

                    // If there is no server record and the client is a tombstone, then we ideally should
                    // just ackowledge the action and don't send any response but
                    // for now we will keep the sync error as is. 

                    _converter.GetEntityFromDataRow(e.Conflict.RemoteChange.Columns, e.Conflict.RemoteChange.Rows[0], syncError.LiveEntity);
                    syncError.LiveEntity.ServiceMetadata.IsTombstone = true;
                }
                else
                {
                    syncError.LiveEntity = serverVersion;
                }

                if (this.ApplyClientChangeFailed != null)
                {
                    this.ApplyClientChangeFailed(syncError.ErrorEntity);
                }

                // This will add the item as an exception to the server knowledge and will also send the exception to the client.
                // However the exception will be cleared the next time the client uploads changes, since we increment tickcounts always.

                applyAction = ApplyAction.Continue;

                // The ApplyChangesFailed event is fired when a conflict is detected. During resolution,
                // if change application fails due to some errors (such as RI, connectivity issues etc), 
                // the provider fires the ApplyChangesFailed event again 
                // to report an error. If we save the error entity as is, then both the original conflict and this new error
                // will be sent back to the client in the response. Since this is not desirable, we first need to remove the
                // corresponding conflict entity from the _conflicts collection before recording the error.

                // Note: item versions are not bumped since the conflict has not yet been resolved.

                RemoveEntityFromConflictCollection(tableName, syncError.LiveEntity);

                _syncErrors.Add(syncError);

                e.Action = applyAction;

                return;
            }

            // Create instances of OfflineCapableEntities and initialize the WinningChange and LosingChange properties.
            var c = new SyncConflict
                        {
                            LiveEntity = (IOfflineEntity)constructorInfo.Invoke(null),
                            LosingEntity = (IOfflineEntity)constructorInfo.Invoke(null)
                        };

            ConflictResolutionPolicy policyToUse = _conflictResolutionPolicy;

            SyncConflictResolution? userResolution = null;
            // Check and fire any Conflict interceptors            
            if (_configuration.HasConflictInterceptors(this._scopeName) ||
                _configuration.HasTypedConflictInterceptor(this._scopeName, entityType))
            {
                userResolution = GetUserConflictResolution(e, constructorInfo, entityType);

                if (userResolution != null && userResolution == SyncConflictResolution.ServerWins)
                {
                    policyToUse = ConflictResolutionPolicy.ServerWins;
                }
                else if (userResolution != null &&
                    (userResolution == SyncConflictResolution.ClientWins || userResolution == SyncConflictResolution.Merge))
                {
                    // If resolution is Merge or ClientWins, set the resolution to ClientWins so the runtime will 
                    // retry with force write and save the merged values back
                    policyToUse = ConflictResolutionPolicy.ClientWins;
                }
            }

            // If there were no Errors, then act based on the service conflict resolution policy.
            switch (policyToUse)
            {
                // ServerWins policy...
                case ConflictResolutionPolicy.ServerWins:

                    // For OCS, ApplyAction.Continue means ServerWins (local change will be maintained).
                    applyAction = ApplyAction.Continue;

                    // If the local change exists, then save it in the WinningChange property
                    if (null != e.Conflict.LocalChange && 1 == e.Conflict.LocalChange.Rows.Count)
                    {
                        _converter.GetEntityFromDataRow(e.Conflict.LocalChange.Columns, e.Conflict.LocalChange.Rows[0], c.LiveEntity);
                    }
                    // If local change does not exist
                    else
                    {
                        _converter.GetEntityFromDataRow(e.Conflict.RemoteChange.Columns, e.Conflict.RemoteChange.Rows[0], c.LiveEntity);
                        c.LiveEntity.ServiceMetadata.IsTombstone = true;
                    }

                    // Save the remote change in the LosingChange property.
                    if (1 == e.Conflict.RemoteChange.Rows.Count)
                    {
                        _converter.GetEntityFromDataRow(e.Conflict.RemoteChange.Columns, e.Conflict.RemoteChange.Rows[0], c.LosingEntity);
                    }

                    // Save the conflict resolution policy. 
                    c.Resolution = WebUtil.GetSyncConflictResolution(ConflictResolutionPolicy.ServerWins);

                    // Set the tombstone flag based on the type of the conflict.
                    switch (e.Conflict.Type)
                    {
                        case DbConflictType.LocalDeleteRemoteDelete:
                            c.LosingEntity.ServiceMetadata.IsTombstone = true;
                            c.LiveEntity.ServiceMetadata.IsTombstone = true;
                            break;
                        case DbConflictType.LocalDeleteRemoteUpdate:
                            c.LiveEntity.ServiceMetadata.IsTombstone = true;
                            break;
                        case DbConflictType.LocalUpdateRemoteDelete:
                            c.LosingEntity.ServiceMetadata.IsTombstone = true;
                            break;
                        // No changes to the tombstone flag for other cases.
                        default:
                            break;
                    }

                    if (this.ApplyClientChangeFailed != null)
                    {
                        this.ApplyClientChangeFailed(c.LosingEntity);
                    }

                    break;

                // ClientWins policy...
                case ConflictResolutionPolicy.ClientWins:

                    // For OCS, client change can be kept by using ApplyAction.RetryWithForceWrite.
                    applyAction = ApplyAction.RetryWithForceWrite;

                    if (1 == e.Conflict.RemoteChange.Rows.Count)
                    {
                        _converter.GetEntityFromDataRow(e.Conflict.RemoteChange.Columns, e.Conflict.RemoteChange.Rows[0], c.LiveEntity);
                    }

                    // If the local change exists, then save it in the WinningChange property
                    if (1 == e.Conflict.LocalChange.Rows.Count)
                    {
                        _converter.GetEntityFromDataRow(e.Conflict.RemoteChange.Columns, e.Conflict.LocalChange.Rows[0], c.LosingEntity);
                    }

                    // Save the conflict resolution policy. 
                    c.Resolution = userResolution ?? WebUtil.GetSyncConflictResolution(ConflictResolutionPolicy.ClientWins);

                    // Set the tombstone flag based on the type of the conflict.
                    switch (e.Conflict.Type)
                    {
                        case DbConflictType.LocalDeleteRemoteDelete:
                            c.LosingEntity.ServiceMetadata.IsTombstone = true;
                            c.LiveEntity.ServiceMetadata.IsTombstone = true;
                            break;
                        case DbConflictType.LocalDeleteRemoteUpdate:
                            c.LosingEntity.ServiceMetadata.IsTombstone = true;
                            break;
                        case DbConflictType.LocalUpdateRemoteDelete:
                            c.LiveEntity.ServiceMetadata.IsTombstone = true;
                            break;
                        // No changes to the tombstone flag for other cases.
                        default:
                            break;
                    }

                    if (this.ApplyClientChangeFailed != null)
                    {
                        this.ApplyClientChangeFailed(c.LiveEntity);
                    }

                    break;
            }

            // After deciding on the Live and the Losing entities for the conflict, we need to generate and save the SyncId 
            // of the LiveEntity. This value is used later after all changes are applied to project on the latest 
            // server knowledge and add positive exceptions to the updated client knowledge that is sent in the response.

            SyncId rowId = GenerateSyncIdForConflictingEntity(tableName, c.LiveEntity);

            if (!_conflictToSyncEntityIdMapping.ContainsKey(c))
            {
                _conflictToSyncEntityIdMapping.Add(c, rowId);

                // Note: SyncId's are unique for each entity.
                Debug.Assert(!_syncEntityIdToConflictMapping.ContainsKey(rowId), "!_syncEntityIdToConflictMapping.ContainsKey(rowId)");

                // Also fill the reverse mapping of syncId to the conflict entity.
                _syncEntityIdToConflictMapping.Add(rowId, c);
            }
            
            _conflicts.Add(c);

            e.Action = applyAction;
        }

        /// <summary>
        /// Remove an entity from a conflicts collection. The conflicts collection contains all the 
        /// SyncConflict instances that are detected when applying changes to the server.
        /// </summary>
        /// <param name="tableName">Global table name of the entity</param>
        /// <param name="entity">Entity to remove from the conflicts collection</param>
        private void RemoveEntityFromConflictCollection(string tableName, IOfflineEntity entity)
        {
            SyncId errorEntitySyncId = GenerateSyncIdForConflictingEntity(tableName, entity);

            // Lookup the conflict entity in the dictionary that we record conflicts.
            SyncConflict conflict;
            _syncEntityIdToConflictMapping.TryGetValue(errorEntitySyncId, out conflict);

            // If a conflict was found...
            if (null != conflict)
            {
                _conflictToSyncEntityIdMapping.Remove(conflict);

                _syncEntityIdToConflictMapping.Remove(errorEntitySyncId);
                    
                // Remove the item from the conflicts collection.
                _conflicts.Remove(conflict);
            }
        }

        /// <summary>
        /// Generate and save the SyncId of the LiveEntity. 
        /// This value is used later after all changes are applied to project on the latest 
        /// server knowledge and add positive exceptions to the updated client knowledge that is sent in the response.
        /// </summary>
        /// <param name="tableName">Table name that the entity represents</param>
        /// <param name="c">Conflicting entity for which we need to save the SyncId.</param>
        private SyncId GenerateSyncIdForConflictingEntity(string tableName, IOfflineEntity c)
        {
            Debug.Assert(null != c, "null != c");

            var pkValues = new List<object>();

            // Get the primary key values from the LiveEntity
            Type entityType = c.GetType();

            // The ordering of keys here is assumed to be the same order in which SyncId's are generated.
            // Otherwise, behavior is undefined and incorrect positive exceptions are added.
            PropertyInfo[] primaryKeyPropertyInfoMapping = ReflectionUtility.GetPrimaryKeysPropertyInfoMapping(entityType);

            foreach (var propertyInfo in primaryKeyPropertyInfoMapping)
            {
                pkValues.Add(propertyInfo.GetValue(c, null));
            }

            // Generate the SyncId for the conflicting item.
            SyncId rowId = SyncUtil.InitRowId(tableName, pkValues);

            return rowId;
        }

        /// <summary>
        /// Invokes the users Conflict Interceptor and returns back with a resolution.
        /// </summary>
        /// <param name="e">Actual event args</param>
        /// <param name="constructorInfo">ConstructorInfo object</param>
        /// <param name="entityType">Entity type of the conflict</param>
        private SyncConflictResolution? GetUserConflictResolution(DbApplyChangeFailedEventArgs e, ConstructorInfo constructorInfo, Type entityType)
        {
            // Create the Client and Server entities
            IOfflineEntity clientVersion = (IOfflineEntity)constructorInfo.Invoke(null);
            IOfflineEntity serverVersion = null;

            // Read RemoteChange as client version
            _converter.GetEntityFromDataRow(e.Conflict.RemoteChange.Columns, e.Conflict.RemoteChange.Rows[0], clientVersion);

            // Set tombstone based on DbSyncConflict as the DataRow is always marked unchanged
            clientVersion.ServiceMetadata.IsTombstone =
                e.Conflict.Type == DbConflictType.LocalDeleteRemoteDelete ||
                e.Conflict.Type == DbConflictType.LocalUpdateRemoteDelete;

            if (e.Conflict.LocalChange != null && e.Conflict.LocalChange.Rows.Count > 0)
            {
                // Server row exists. Create memory for row.
                serverVersion = (IOfflineEntity)constructorInfo.Invoke(null);
                _converter.GetEntityFromDataRow(e.Conflict.LocalChange.Columns, e.Conflict.LocalChange.Rows[0], serverVersion);

                // Set tombstone based on DbSyncConflict as the DataRow is always marked unchanged
                serverVersion.ServiceMetadata.IsTombstone =
                    e.Conflict.Type == DbConflictType.LocalDeleteRemoteDelete ||
                    e.Conflict.Type == DbConflictType.LocalDeleteRemoteUpdate;
            }

            IOfflineEntity mergedVersion = null;
            this._conflictContext.ClientChange = clientVersion;
            this._conflictContext.ServerChange = serverVersion;

            SyncConflictResolution? userResolution = this._configuration.InvokeConflictInterceptor(
                this._conflictContext,
                entityType,
                out mergedVersion);

            // Check to see the resolution.
            if (userResolution != null && userResolution == SyncConflictResolution.Merge)
            {
                // Check that mergedVersion is not null and is of the expected type
                if (mergedVersion == null)
                {
                    throw new InvalidOperationException("User SyncConflictInterceptor returned a conflict resolution of 'Merge' but did not specify a merged version.");
                }
                if (mergedVersion.GetType() != clientVersion.GetType())
                {
                    throw new InvalidOperationException(
                        string.Format(CultureInfo.InvariantCulture,
                                      "User SyncConflictInterceptor returned merged version entity type '{0} does not match required type '{1}'.",
                                      mergedVersion.GetType().Name, clientVersion.GetType().Name));
                }

                // Merge is required
                // If merge is requested then the server version is always the losing version as changes are overwritten. In this case
                // set the policy to clientWins and copy the mergedVersion values in to the Args.Conflict.RemoteChange
                object[] rowValues = _converter.CopyEntityToDataRow(mergedVersion, e.Conflict.RemoteChange);

                // Now add this row back to the DataSet to we can retry applying this.
                _converter.MergeChangeInToDataSet(e.Context.DataSet.Tables[e.Conflict.RemoteChange.TableName], e.Conflict.RemoteChange.Rows[0], rowValues, mergedVersion.GetType());
            }

            return userResolution;
        }

        /// <summary>
        /// Create a new scope for a client. This method is called when GetChanges is passed a null blob.
        /// The requested scope is compared to an existing scope or a template and a new scope is provisioned
        /// If the requested scope is an existing template, filter parameters, if present, are added when provisioning.
        /// 
        /// Note: If both scope and template match the requested scope, we prefer the scope. We would need to expose this 
        /// out to the service if we want to make this choice configurable.
        /// </summary>
        private void CreateNewScopeForClient()
        {
            using (var serverConnection = new SqlConnection(_configuration.ServerConnectionString))
            {
                // Default's to scope.
                // Note: Do not use constructors that take in a DbSyncScopeDescription since there are checks internally
                // to ensure that it has atleast 1 table. In this case we would be passing in a non-existing scope which throws an
                // exception.
                var provisioning = new SqlSyncScopeProvisioning(serverConnection);

                // Set the ObjectSchema property. Without this, the TemplateExists and ScopeExists method
                // always return false if the sync objects are provisioned in a non-dbo schema.
                if (!String.IsNullOrEmpty(_configuration.SyncObjectSchema))
                {
                    provisioning.ObjectSchema = _configuration.SyncObjectSchema;
                }

                // Determine if this is a scope or a template.
                //Note: Scope has a higher priority than a template. See method summary for more info.
                bool isTemplate;
                if (provisioning.ScopeExists(_scopeName))
                {
                    isTemplate = false;
                }
                else if (provisioning.TemplateExists(_scopeName))
                {
                    isTemplate = true;
                }
                else
                {
                    throw SyncServiceException.CreateBadRequestError(Strings.NoScopeOrTemplateFound);
                }

                // If scope...
                if (!isTemplate)
                {
                    DbSyncScopeDescription scopeDescription = String.IsNullOrEmpty(_configuration.SyncObjectSchema) ?
                        SqlSyncDescriptionBuilder.GetDescriptionForScope(_scopeName, serverConnection) :
                        SqlSyncDescriptionBuilder.GetDescriptionForScope(_scopeName, string.Empty /*objectPrefix*/, _configuration.SyncObjectSchema, serverConnection);

                    scopeDescription.ScopeName = _clientScopeName;

                    provisioning.PopulateFromScopeDescription(scopeDescription);

                    // If scope then disable bulk procedures. 
                    // Template provisioning does not create anything.
                    provisioning.SetUseBulkProceduresDefault(false);
                }
                // If template...
                else
                {
                    provisioning.PopulateFromTemplate(_clientScopeName, _scopeName);

                    // Add filter parameters.
                    if (null != _filterParams && 0 != _filterParams.Count)
                    {
                        foreach (var param in _filterParams)
                        {
                            provisioning.Tables[param.TableName].FilterParameters[param.SqlParameterName].Value = param.Value;
                        }
                    }
                }

                if (!provisioning.ScopeExists(_clientScopeName))
                {
                    provisioning.Apply();
                }
            }
        }

        /// <summary>
        /// Check if a scope exists.
        /// </summary>
        /// <returns>True - if the scope exists, false - otherwise.</returns>
        private bool CheckIfScopeExists()
        {
            using (var serverConnection = new SqlConnection(_serverConnectionString))
            {
                var provisioning = new SqlSyncScopeProvisioning(serverConnection);

                if (!String.IsNullOrEmpty(_configuration.SyncObjectSchema))
                {
                    provisioning.ObjectSchema = _configuration.SyncObjectSchema;
                }

                if (provisioning.ScopeExists(_clientScopeName))
                {
                    return true;
                }

                return false;
            }
        }

        private DbSyncScopeDescription GetScopeDescription()
        {
            using (var serverConnection = new SqlConnection(_serverConnectionString))
            {
                DbSyncScopeDescription scopeDescription = String.IsNullOrEmpty(_configuration.SyncObjectSchema) ?
                        SqlSyncDescriptionBuilder.GetDescriptionForScope(_clientScopeName, serverConnection) :
                        SqlSyncDescriptionBuilder.GetDescriptionForScope(_clientScopeName, string.Empty /*objectPrefix*/, _configuration.SyncObjectSchema, serverConnection);

                return scopeDescription;
            }
        }

        /// <summary>
        /// Add Sync related columns to the dataset. 
        /// 
        /// These are:
        ///  DbSyncSession.SyncCreatePeerKey
        ///  DbSyncSession.SyncCreatePeerTimestamp
        ///  DbSyncSession.SyncUpdatePeerKey
        ///  DbSyncSession.SyncUpdatePeerTimestamp
        /// </summary>
        /// <param name="changes">Dataset to which columns need to be added.</param>
        /// <param name="tickCount">New tickcount to use when creating versions.</param>
        private static void AddSyncColumnsToDataSet(DataSet changes, ulong tickCount)
        {
            foreach (DataTable table in changes.Tables)
            {
                if (!table.Columns.Contains(DbSyncSession.SyncCreatePeerKey))
                {
                    var dc = new DataColumn(DbSyncSession.SyncCreatePeerKey, typeof(int)) { DefaultValue = 0 };
                    table.Columns.Add(dc);
                }

                if (!table.Columns.Contains(DbSyncSession.SyncCreatePeerTimestamp))
                {
                    var dc = new DataColumn(DbSyncSession.SyncCreatePeerTimestamp, typeof(long)) { DefaultValue = 0 };
                    table.Columns.Add(dc);
                }

                if (!table.Columns.Contains(DbSyncSession.SyncUpdatePeerKey))
                {
                    var dc = new DataColumn(DbSyncSession.SyncUpdatePeerKey, typeof(int)) { DefaultValue = 0 };
                    table.Columns.Add(dc);
                }

                if (!table.Columns.Contains(DbSyncSession.SyncUpdatePeerTimestamp))
                {
                    var dc = new DataColumn(DbSyncSession.SyncUpdatePeerTimestamp, typeof(long)) { DefaultValue = 0 };
                    table.Columns.Add(dc);
                }

                // For each datarow, set the create peerkey or update peer key based on the rowstate.
                // The SyncCreatePeerKey and SyncUpdatePeerKey values are 0 which means the client replica sent these changes.
                foreach (DataRow row in table.Rows)
                {
                    switch (row.RowState)
                    {
                        case DataRowState.Added:
                            // for rows that have been added we need to
                            // update both the create and update versions to be the same.
                            // for ex, if a row was deleted and added again, the server update version will otherwise have a higher value
                            // since the sent update version will be set to 0. This results in the DbChangeHandler.ApplyInsert returning LocalSupersedes
                            // internally after it compares the versions.
                            row[DbSyncSession.SyncCreatePeerKey] = 0;
                            row[DbSyncSession.SyncCreatePeerTimestamp] = tickCount;
                            row[DbSyncSession.SyncUpdatePeerKey] = 0;
                            row[DbSyncSession.SyncUpdatePeerTimestamp] = tickCount;
                            break;
                        case DataRowState.Modified:
                            // Only update the update version for modified rows.
                            row[DbSyncSession.SyncUpdatePeerKey] = 0;
                            row[DbSyncSession.SyncUpdatePeerTimestamp] = tickCount;
                            break;
                        case DataRowState.Deleted:
                            row.RejectChanges();
                            row[DbSyncSession.SyncUpdatePeerKey] = 0;
                            row[DbSyncSession.SyncUpdatePeerTimestamp] = tickCount;
                            row.AcceptChanges();
                            row.Delete();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Remove sync related columns from the DataSet.
        /// </summary>
        /// <param name="dataSet">DataSet from which sync related columns should be removed.</param>
        private static void RemoveSyncVersionColumns(DataSet dataSet)
        {
            // Remove the Sync version columns
            foreach (DataTable dt in dataSet.Tables)
            {
                dt.Columns.Remove(DbSyncSession.SyncCreatePeerKey);
                dt.Columns.Remove(DbSyncSession.SyncCreatePeerTimestamp);
                dt.Columns.Remove(DbSyncSession.SyncUpdatePeerKey);
                dt.Columns.Remove(DbSyncSession.SyncUpdatePeerTimestamp);
            }
        }

        /// <summary>
        /// Remove all rows which are marked deleted in a dataset.
        /// </summary>
        /// <param name="dataSet">DataSet from which to remove all deleted rows.</param>
        private static void RemoveTombstoneRowsFromDataSet(DataSet dataSet)
        {
            foreach (DataTable table in dataSet.Tables)
            {
                var rowsToRemove = new List<DataRow>();
                foreach (DataRow row in table.Rows)
                {
                    if (row.RowState == DataRowState.Deleted)
                    {
                        rowsToRemove.Add(row);
                    }
                }

                foreach (var row in rowsToRemove)
                {
                    table.Rows.Remove(row);
                }
            }
        }

        /// <summary>
        /// The way the current P2P provider works, versions are bumped up when conflicts are resolved on the server.
        /// This would result in us sending the changes to the client on the next download request. We want
        /// to not enumerate that change again. So one solution is to get the server knowledge after all changes are applied and then
        /// project the knowledge of each conflict and add it as a positive exception to the updated client knowledge.
        /// </summary>
        /// <param name="updatedClientKnowledge">Knowledge that is going to be sent to the client in the response</param>
        /// <param name="serverKnowledge">Server knowledge after applying changes</param>
        private void AddConflictItemsKnowledgeToClientKnowledge(SyncKnowledge updatedClientKnowledge, SyncKnowledge serverKnowledge)
        {
            foreach (var conflict in _conflicts)
            {
                SyncId entitySyncId;

                _conflictToSyncEntityIdMapping.TryGetValue(conflict, out entitySyncId);

                if (null == entitySyncId)
                {
                    throw new InvalidOperationException("SyncId is missing for a conflicting entity.");
                }

                // Create a new SyncKnowledge which only includes the server replica and set the local tickcount to
                // the value of @@DBTS that was read before committing the Apply transaction.
                var localKnowledge = new SyncKnowledge(serverKnowledge.GetSyncIdFormatGroup(),
                                                       serverKnowledge.ReplicaId,
                                                       _serverTickCountAfterResolvingAllConflicts);

                // Add the knowledge of the conflicting item to the client knowledge. This will be 
                // sent back to the client. In the next download request, the conflicting item will
                // not be enumerated since it is already contained in the knowledge.
                // After enumeration the knowledge is compacted and the single item positive exception
                // is removed.
                // Note: If there are a lot of conflicts, the knowledge sent back to the client will be 
                // large for that one instance. However the size will is not very significant compared to the amount
                // of data that is sent back in the response in the winning and the losing entities. 
                // The large knowledge in this case will be compacted on a subsequent download from the same client.

                // Project the knowledge of the single row from the created knowledge and combine it with
                // the updated client knowledge. This will add a positive exception since the server tickcount in the 
                // knowledge that is created (localKnowledge) is newer than that in the updatedClientKnowledge.
                updatedClientKnowledge.Combine(localKnowledge.GetKnowledgeForItem(entitySyncId));
            }
        }

        private void PopulateClientScopeNameAndSyncId(SyncBlob incomingBlob)
        {
            _clientScopeName = String.Format("{0}_{1}", _scopeName, incomingBlob.ClientScopeName);
            _clientSyncId = new SyncId(new Guid(incomingBlob.ClientScopeName));
        }

        /// <summary>
        /// Create a new instance of the SqlSyncProvider class.
        /// </summary>
        /// <param name="clientScopeName">Scope name</param>
        /// <param name="serverConnectionString">Connection string</param>
        /// <param name="syncObjectSchema">Schema for sync objects</param>
        /// <returns>Instance of <see cref="SqlSyncProvider" /> class.</returns>
        private static SqlSyncProvider CreateSqlSyncProviderInstance(string clientScopeName, string serverConnectionString, string syncObjectSchema)
        {
            var sqlSyncProvider = new SqlSyncProvider(clientScopeName, new SqlConnection(serverConnectionString));

            if (!String.IsNullOrEmpty(syncObjectSchema))
            {
                sqlSyncProvider.ObjectSchema = syncObjectSchema;
            }

            return sqlSyncProvider;
        }


        #endregion
    }
}
