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
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Channels;
using Microsoft.Synchronization.Services.Formatters;
using Microsoft.Synchronization.Services.SqlProvider;
using System.Collections.Specialized;
using System.Net;
using System.Reflection;

namespace Microsoft.Synchronization.Services
{
    /// <summary>
    /// Handler for the upload changes request command.
    /// </summary>
    internal class UploadChangesRequestProcessor : SyncRequestProcessorBase, IRequestProcessor
    {
        #region Private Members

        private ApplyChangesResponse _applyChangesResponse;
        private Uri _baseUri;
        private SyncSerializationFormat _responseSerializationFormat;
        private List<IOfflineEntity> _incomingEntities;

        /// <summary>
        /// This field contains the list of clent inserts that were successfully processed.
        /// As and when a Conflict/Error occures or the user rejects an entity that was an insert
        /// then the item is removed from this collection. At the end this collection will give us a
        /// easy way of passing to the UploadResponseOperationContext.OutgoingChanges property.
        /// </summary>
        private List<IOfflineEntity> _incomingNewInsertEntities;
        private Dictionary<string, string> _idToTempIdMapping;
        private Dictionary<IOfflineEntity, string> _rejectedEntities;

        private readonly WebHeaderCollection _interceptorsResponseHeaders = new WebHeaderCollection();

        #endregion

        #region Constructor

        public UploadChangesRequestProcessor(SyncServiceConfiguration configuration, HttpContextServiceHost serviceHost)
            : base(configuration, serviceHost)
        {
            base._syncOperation = SyncOperations.Upload;
        }

        #endregion

        protected override void InitRequestOperationContext()
        {
            base._operationContext = new SyncUploadRequestOperationContext()
            {
                ScopeName = this._scopeName,
                Operation = this._syncOperation,
                QueryString = new NameValueCollection(_serviceHost.QueryStringCollection.Count, _serviceHost.QueryStringCollection),
                RequestHeaders = _serviceHost.RequestHeaders,
                ResponseHeaders = _interceptorsResponseHeaders
            };
        }

        protected override void InitResponseOperationContext()
        {
            base._operationContext = new SyncUploadResponseOperationContext()
            {
                ScopeName = this._scopeName,
                Operation = this._syncOperation,
                QueryString = new NameValueCollection(_serviceHost.QueryStringCollection.Count, _serviceHost.QueryStringCollection),
                RequestHeaders = _serviceHost.RequestHeaders,
                ResponseHeaders = _interceptorsResponseHeaders
            };
        }

        #region IRequestProcessor Members

        /// <summary>
        /// Process the incoming request and forms a formatted outgoing response.
        /// </summary>
        /// <param name="incomingRequest">Incoming request</param>
        /// <returns>Message instance containing the outgoing response</returns>
        public Message ProcessRequest(Request incomingRequest)
        {
            _baseUri = _serviceHost.ServiceBaseUri;

            _responseSerializationFormat = incomingRequest.ResponseSerializationFormat;

            _incomingEntities = incomingRequest.EntityList;

            _idToTempIdMapping = incomingRequest.IdToTempIdMapping;

            // Check and invoke request interceptor
            this.PrepareAndProcessRequestInterceptor();

            IAsymmetricProviderService providerService =
                new SqlSyncProviderService(_configuration,
                                           Convert.ToString(incomingRequest.CommandParams[CommandParamType.ScopeName]),
                                           incomingRequest.ProviderParams,
                                           base._operationContext);

            // Set a callback for the ApplyClientChangeFailed delegate.
            ((SqlSyncProviderService)providerService).ApplyClientChangeFailed = ClientChangeFailedToApply;

            // Loop over client input and pull all inserts to a different collection.
            _incomingNewInsertEntities = _incomingEntities.Where(e => string.IsNullOrEmpty(e.ServiceMetadata.Id)).ToList();

            _applyChangesResponse = providerService.ApplyChanges(incomingRequest.SyncBlob, incomingRequest.EntityList);

            // Give the inserts permanent Ids
            AssignRealIdsForClientInserts(_incomingNewInsertEntities);

            // Process the rejected entities if any
            this.ProcessRejectedEntities((SqlSyncProviderService)providerService);

            // Check and fire response interceptor
            this.PrepareAndProcessResponseInterceptor(providerService);

            var oDataWriter = GetSyncWriterWithContents();

            return base.CreateResponseMessage(incomingRequest.ResponseSerializationFormat, oDataWriter);
        }

        /// <summary>
        /// Function that initializes the SyncUploadRequestContext and then invokes user interceptor code
        /// </summary>
        private void PrepareAndProcessRequestInterceptor()
        {
            if (_configuration.HasRequestInterceptors(this._scopeName, SyncOperations.Upload) ||
                _configuration.HasTypedRequestInterceptors(this._scopeName) ||
                _configuration.HasConflictInterceptors(this._scopeName) ||
                _configuration.HasTypedConflictInterceptors(this._scopeName))
            {
                // Init the request SyncOperationContext if any of typed/untyped request/conflict interceptor is present
                this.InitRequestOperationContext();
            }

            if (_configuration.HasRequestInterceptors(this._scopeName, SyncOperations.Upload) ||
                _configuration.HasTypedRequestInterceptors(this._scopeName))
            {
                if (_configuration.HasRequestInterceptors(this._scopeName, SyncOperations.Upload))
                {
                    // Set context properties.
                    ((SyncUploadRequestOperationContext)base._operationContext).IncomingChanges = _incomingEntities.AsReadOnly();

                    // Fire the request Interceptors if any
                    base.ProcessRequestInterceptors();
                }
                else
                {
                    // Group the entities by its Type. This gives us a grouping with a Key parameter
                    // that has the actual type.
                    foreach (IGrouping<Type, IOfflineEntity> grouping in _incomingEntities.GroupBy(e => e.GetType()))
                    {
                        if (_configuration.HasTypedRequestInterceptor(this._scopeName, grouping.Key))
                        {
                            // Set context properties.
                            ((SyncUploadRequestOperationContext)base._operationContext).IncomingChanges =
                                grouping.ToList().AsReadOnly();

                            // Fire the request Interceptors if any
                            base.ProcessTypedRequestInterceptors(grouping.Key);
                        }
                    }
                }

                // Check for rejected entities
                CheckForRejectedEntities(((SyncUploadRequestOperationContext)base._operationContext).RejectedEntries);
            }
        }

        /// <summary>
        /// Function that initializes the SyncUploadResponseContext and then invokes user interceptor code
        /// </summary>
        /// <param name="providerService">SqlSyncProviderService instance</param>
        private void PrepareAndProcessResponseInterceptor(IAsymmetricProviderService providerService)
        {
            if (_configuration.HasResponseInterceptors(this._scopeName, SyncOperations.Upload) ||
                _configuration.HasTypedResponseInterceptors(this._scopeName, SyncOperations.Upload))
            {

                // Init the response SyncOperationContext
                this.InitResponseOperationContext();

                // Set context properties.
                SyncUploadResponseOperationContext responseContext = (SyncUploadResponseOperationContext)base._operationContext;

                // Create a list of conflicts
                List<Conflict> conflicts = new List<Conflict>(_applyChangesResponse.Conflicts.Cast<Conflict>());

                // Merge the Conflicts and Errors properties
                conflicts.AddRange(_applyChangesResponse.Errors.Cast<Conflict>());

                if (_configuration.HasResponseInterceptors(this._scopeName, SyncOperations.Upload))
                {
                    // Set context's conflicts collection
                    responseContext.Conflicts = conflicts.AsReadOnly();

                    // Set items that is being responded with a permanent Id. These will be entires from the 
                    // _incomingNewInsertEntities that were not conflicts or errors
                    responseContext.OutgoingChanges = _incomingNewInsertEntities.AsReadOnly();

                    // Fire the response Interceptors if any
                    base.ProcessResponseInterceptors();
                }
                else
                {
                    // This means we have typed interceptors. We need to fire the interceptor if the type exists
                    // either in the OutgoingChanges collection or in Conflicts collection. When item exists in both
                    // collections then it must be fired only once and must contain both entires. So visit the outoging
                    // group first and keep track of Type already visited. Then go over the Conflicts collection and then
                    // visit types that arent visites yet.

                    List<Type> visitedTypes = new List<Type>();
                    foreach (IGrouping<Type, IOfflineEntity> grouping in _incomingNewInsertEntities.GroupBy(e => e.GetType()))
                    {
                        if (_configuration.HasTypedResponseInterceptor(this._scopeName, SyncOperations.Upload, grouping.Key))
                        {
                            visitedTypes.Add(grouping.Key);

                            // Set context's conflicts collection
                            responseContext.Conflicts = conflicts.Where(e => e.LiveEntity.GetType() == grouping.Key).ToList().AsReadOnly();

                            // Set items that is being responded with a permanent Id. These will be entires from the 
                            // _incomingNewInsertEntities that were not conflicts or errors
                            responseContext.OutgoingChanges = grouping.ToList().AsReadOnly();

                            // Fire the typed response Interceptors if any
                            base.ProcessTypedResponseInterceptors(grouping.Key);
                        }
                    }

                    // Now visit the Conflict collection
                    foreach (IGrouping<Type, Conflict> grouping in conflicts.GroupBy(e => e.LiveEntity.GetType()))
                    {
                        if (_configuration.HasTypedResponseInterceptor(this._scopeName, SyncOperations.Upload, grouping.Key) &&
                            !visitedTypes.Contains(grouping.Key))
                        {
                            // Set context's conflicts collection
                            responseContext.OutgoingChanges = _incomingNewInsertEntities.Where(e => e.GetType() == grouping.Key).ToList().AsReadOnly();

                            // Set items that is being responded with a permanent Id. These will be entires from the 
                            // _incomingNewInsertEntities that were not conflicts or errors
                            responseContext.Conflicts = grouping.ToList().AsReadOnly();

                            // Fire the typed response Interceptors if any
                            base.ProcessTypedResponseInterceptors(grouping.Key);
                        }
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// This is called for every conflict/error with the client entity. 
        /// We use this function to check if the error was a client insert and if yes then remove it from
        /// the list of _incomingNewInsertEntities. This way we know which entities are successful uploads so
        /// we can pass this info to the UploadResponseInterceptor.OutgoingChanges list.
        /// </summary>
        /// <param name="entity">The client version of the entity. We match the actual reference via its primary key</param>
        internal void ClientChangeFailedToApply(IOfflineEntity entity)
        {
            string primaryKey = ReflectionUtility.GetPrimaryKeyString(entity);
            // Check to see if this element is in the insert list and if yes then remove it
            IOfflineEntity matchedEntity = this._incomingNewInsertEntities.Where(
                e => ReflectionUtility.GetPrimaryKeyString(e).Equals(primaryKey, StringComparison.InvariantCulture)).FirstOrDefault();
            if (matchedEntity != null)
            {
                this._incomingNewInsertEntities.Remove(matchedEntity);
            }
        }

        #region Private Methods

        /// <summary>
        /// Iterates over the rejected entities and removes them from the Entities to be sent to the database.
        /// </summary>
        /// <param name="rejects">rejected entities</param>
        private void CheckForRejectedEntities(Dictionary<IOfflineEntity, string> rejects)
        {
            this._rejectedEntities = rejects;

            foreach (KeyValuePair<IOfflineEntity, string> kvp in this._rejectedEntities)
            {
                // Remove the entity from the list of entities
                this._incomingEntities.Remove(kvp.Key);
            }
        }

        /// <summary>
        /// This function loops the rejected entites and sends back a SyncError for each entity. For each entity it does the following
        /// 1. Retrieve the current version in server.
        /// 1.a If its null then it copies the primary key to a new object and marks it as tombstone.
        /// 2. Adds the SyncError to existing list of SyncErrors.
        /// </summary>
        /// <param name="sqlProvider"></param>
        private void ProcessRejectedEntities(SqlSyncProviderService sqlProvider)
        {
            if (this._rejectedEntities == null || this._rejectedEntities.Count == 0)
            {
                return;
            }

            try
            {
                List<IOfflineEntity> serverVersions = sqlProvider.GetCurrentServerVersionForEntities(this._rejectedEntities.Keys);
                if (serverVersions.Count != this._rejectedEntities.Count)
                {
                    // Ensure we get a server version for each entity we passed
                    throw new InvalidOperationException("Did not get server versions for all rejected entities.");
                }
                for (int i = 0; i < this._rejectedEntities.Keys.Count; i++)
                {
                    IOfflineEntity server = serverVersions[i];
                    IOfflineEntity client = this._rejectedEntities.Keys.ElementAt(i);

                    if (server == null)
                    {
                        // This means the server didnt contain a version for the entity. Need to send a tombstone back then
                        // create a new object and copy the values over
                        ConstructorInfo constructorInfo = client.GetType().GetConstructor(Type.EmptyTypes);
                        server = (IOfflineEntity)constructorInfo.Invoke(null);
                        // Copy the primary key values over
                        foreach (PropertyInfo info in ReflectionUtility.GetPrimaryKeysPropertyInfoMapping(client.GetType()))
                        {
                            info.SetValue(server, info.GetValue(client, null), null);
                        }
                        server.ServiceMetadata.IsTombstone = true;
                    }

                    SyncError error = new SyncError()
                    {
                        ErrorEntity = client,
                        LiveEntity = server,
                        Description = this._rejectedEntities[client]
                    };

                    _applyChangesResponse.Errors.Add(error);
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Error in reading server row values. " + e.Message);
            }
        }

    
        // This method ensures that we have a valid feed through the formatters. The BodyWriter delegate in WCF
        // seems to not recover from an unhandled exception caused by the formatters and sends out an empty response to the caller. 
        // This is a workaround for this issue until we find a better solution.
        private SyncWriter GetSyncWriterWithContents()
        {
            var conflictEntryKeys = new List<string>();
            var errorEntryKeys = new List<string>();
            var primaryKeyToIncomingEntitiesMapping = new Dictionary<string, IOfflineEntity>();
           
            // Save the mapping between entity PK string -> entity 
            foreach (var entity in _incomingEntities)
            {
                string primaryKey = ReflectionUtility.GetPrimaryKeyString(entity);
                string entityType = entity.GetType().ToString();
                string key = entityType + ":" + primaryKey;
                if (primaryKeyToIncomingEntitiesMapping.ContainsKey(key))
                {
                    throw SyncServiceException.CreateInternalServerError(
                        Strings.MultipleEntriesWithSamePrimaryKeyInIncomingRequest);
                }

                primaryKeyToIncomingEntitiesMapping.Add(key, entity);
            }
          
            if (_rejectedEntities != null)
            {
                foreach (var entity in _rejectedEntities.Keys)
                {
                    string primaryKey = ReflectionUtility.GetPrimaryKeyString(entity);
                    string entityType = entity.GetType().ToString();
                    string key = entityType + ":" + primaryKey;
                    if (primaryKeyToIncomingEntitiesMapping.ContainsKey(key))
                    {
                        throw SyncServiceException.CreateInternalServerError(
                            Strings.MultipleEntriesWithSamePrimaryKeyInIncomingRequest);
                    }

                    primaryKeyToIncomingEntitiesMapping.Add(key, entity);
                }
            }

            // Get the appropriate SyncWriter instance based on the serialization format.
            var oDataWriter = WebUtil.GetSyncWriter(_responseSerializationFormat, _baseUri);

            oDataWriter.StartFeed(_applyChangesResponse.IsLastBatch, _applyChangesResponse.ServerBlob);

            // Write conflict entities.
            foreach (var entity in _applyChangesResponse.Conflicts)
            {
                // Add the primary key string to the conflictEntryKey list. 
                // The primary keys are the same for both Live and Losing entities.
                conflictEntryKeys.Add(ReflectionUtility.GetPrimaryKeyString(entity.LiveEntity));

                string tempId;

                // If the client change lost, then we need to set the Id property
                // only if the property was not null/empty in the incoming request.
                string entityId = WebUtil.GenerateOfflineEntityId(entity.LiveEntity);

                // Set the Id property of the Live entity (server's copy).
                entity.LiveEntity.ServiceMetadata.Id = entityId;

                // Set the Id property of the Losing entity to the incoming entity's Id value
                entity.LosingEntity.ServiceMetadata.Id = entityId;

                // get the original tempId. Null value is ok.
                _idToTempIdMapping.TryGetValue(entityId, out tempId);

                if (entity.Resolution == SyncConflictResolution.ServerWins)
                {
                    // The losing entity is the client's copy.

                    // When resolution is ServerWins, we only need to set the losing change tempId.
                    oDataWriter.AddConflictItem(entity.LiveEntity, null /*tempId*/, entity.LosingEntity, tempId, entity.Resolution);
                }
                // If the client change won, then just set the Id property since an insert would have succeeded.
                else
                {
                    // When resolution is ClientWins, we only need to set the LiveEntity tempId.
                    oDataWriter.AddConflictItem(entity.LiveEntity, tempId, entity.LosingEntity, null /* tempId */, entity.Resolution);
                }
            }

            // Write error entities.
            foreach (var syncError in _applyChangesResponse.Errors)
            {
                Debug.Assert(null != syncError.LiveEntity);
                Debug.Assert(null != syncError.ErrorEntity);

                string entityId = WebUtil.GenerateOfflineEntityId(syncError.LiveEntity);

                // Set the Id for Live and Losing entity.
                syncError.LiveEntity.ServiceMetadata.Id = entityId;
                syncError.ErrorEntity.ServiceMetadata.Id = entityId;

                string primaryKeyString = ReflectionUtility.GetPrimaryKeyString(syncError.ErrorEntity);

                // Add the string to the error key list.
                errorEntryKeys.Add(primaryKeyString);

                string tempId;

                _idToTempIdMapping.TryGetValue(entityId, out tempId);

                oDataWriter.AddErrorItem(syncError.LiveEntity, syncError.ErrorEntity, tempId, syncError.Description);
            }

            // Write all the inserted records here by iterating over the _incomingNewInsertEntities list
            foreach (var entity in _incomingNewInsertEntities)
            {
                string entityTempId;

                // Get the tempId of the entity.
                _idToTempIdMapping.TryGetValue(WebUtil.GenerateOfflineEntityId(entity), out entityTempId);

                // Write the output to the SyncWriter.
                oDataWriter.AddItem(entity, entityTempId);
            }

            return oDataWriter;
        }

        /// <summary>
        /// This is a common place where a real id is assigned to all client inserts.
        /// </summary>
        /// <param name="clientUploads">Collection of entities to check</param>
        private static void AssignRealIdsForClientInserts(IList<IOfflineEntity> clientUploads)
        {
            // Iterate over entities that dont have a ID which means its an client insert
            foreach (IOfflineEntity entity in clientUploads.Where(e => string.IsNullOrEmpty(e.ServiceMetadata.Id)))
            {
                entity.ServiceMetadata.Id = WebUtil.GenerateOfflineEntityId(entity);
            }
        }

        /// <summary>
        /// Get the entity from the incoming request that matches the primary key string of the entity passed as a parameter.
        /// </summary>
        /// <param name="primaryKeyToIncomingEntitiesMapping">Dictionary of mapping between primary key and the actual entities from incoming request.</param>
        /// <param name="entity">Entity for which to search a match in the incoming request.</param>
        /// <param name="isConflict">Indicates if this is called during conflict processing. Used to select appropriate error messages.</param>
        /// <returns>Entity from the incoming request.</returns>
        private static IOfflineEntity GetEntityFromIncomingRequest(Dictionary<string, IOfflineEntity> primaryKeyToIncomingEntitiesMapping,
                                                                   IOfflineEntity entity,
                                                                   bool isConflict)
        {
            // find the actual entity from the input list.
            var entityListInRequest = primaryKeyToIncomingEntitiesMapping.Where(e =>
                e.Key.Equals(ReflectionUtility.GetPrimaryKeyString(entity), StringComparison.InvariantCultureIgnoreCase)).ToList();

            // If no match is found, then throw an error.
            if (0 == entityListInRequest.Count)
            {
                if (isConflict)
                {
                    throw SyncServiceException.CreateInternalServerError(Strings.ConflictEntityMissingInIncomingRequest);
                }

                throw SyncServiceException.CreateInternalServerError(Strings.ErrorEntityMissingInIncomingRequest);
            }

            // If the entity corresponding to the key is null, then throw an error.
            if (null == entityListInRequest[0].Value)
            {
                if (isConflict)
                {
                    throw SyncServiceException.CreateInternalServerError(Strings.ConflictEntityMissingInIncomingRequest);
                }

                throw SyncServiceException.CreateInternalServerError(Strings.ErrorEntityMissingInIncomingRequest);
            }

            return entityListInRequest[0].Value;
        }

        #endregion
    }
}
