using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Synchronization.ClientServices.Common;


namespace Microsoft.Synchronization.ClientServices.IsolatedStorage
{
    /// <summary>
    /// This is an internal class that manages the in-memory view of the data.
    /// </summary>
    public class CacheData
    {
        /// <summary>
        /// Constructor which initializes the data.
        /// </summary>
        /// <param name="schema">Schema for the data</param>
        public CacheData(OfflineSchema schema)
        {
            Collections = new Dictionary<Type, OfflineCollection>();
            SyncConflicts = new List<SyncConflict>();
            SyncErrors = new List<SyncError>();

            CreateCollections(schema);
        }

        /// <summary>
        /// Method called when changes have been downloaded from sync and are being added to the cache. 
        /// </summary>
        /// <param name="anchor">Anchor downloaded from sync</param>
        /// <param name="entities">Entities received from sync</param>
        public void DownloadedChanges(byte[] anchor, IEnumerable<OfflineEntity> entities)
        {
            //Its First sync if AnchorBlob is null
            bool isFirstSync = AnchorBlob == null;

            // Store the anchor
            AnchorBlob = anchor;

            foreach (OfflineEntity entity in entities)
            {
                // Switch the state to turn on change tracking.
                entity.EntityState = OfflineEntityState.Unmodified;

                // Pass the entity to the collection                
                Collections[entity.GetType()].AddOrUpdateSyncEntity(entity, isFirstSync);
            }
        }

        /// <summary>
        /// Method called when an upload changes response is received.
        /// </summary>
        /// <param name="anchor">Anchor returned from upload response</param>
        /// <param name="conflicts">List of conflicts that occurred</param>
        /// <param name="updatedItems">List of items that were updated on upload</param>
        /// <param name="context">Storage context associated with the cache data</param>
        public void AddUploadChanges(byte[] anchor, IEnumerable<Conflict> conflicts, IEnumerable<OfflineEntity> updatedItems, WinEightContext context)
        {
            // For each of the uploaded entities, reset the state.  No save can happen
            // once sync is started, so this is ok.
            foreach (OfflineCollection collection in Collections.Values)
            {
                collection.ResetSavedEntitiesToUnmodified();
            }

            // Grab the sync conflicts and errors
            List<SyncConflict> syncConflicts = new List<SyncConflict>();
            List<SyncError> syncErrors = new List<SyncError>();

            foreach (Conflict conflict in conflicts)
            {
                if (conflict is SyncConflict)
                {
                    syncConflicts.Add((SyncConflict)conflict);
                }
                else if (conflict is SyncError)
                {
                    syncErrors.Add((SyncError)conflict);
                }
                else
                {
                    throw new ArgumentException("Unexpected conflict type returned in upload response");
                }
            }

            // Add the conflicts and errors.
            AddConflicts(syncConflicts, context);
            AddErrors(syncErrors, context);

            DownloadedChanges(anchor, updatedItems);
        }

        /// <summary>
        /// Called when a download response file is deserialized.  This is a separate method from the one used for sync
        /// in order to handle different assumptions that can be made (one example is that when deserializing changes,
        /// threading issues aren't a worry because the application won't have a reference to any data).
        /// </summary>
        /// <param name="anchor">Anchor read from file</param>
        /// <param name="entities">Entities</param>
        public void AddSerializedDownloadResponse(byte[] anchor, IEnumerable<OfflineEntity> entities)
        {
            DownloadedChanges(anchor, entities);
        }

        /// <summary>
        /// Called when an upload response is deserialized.  It must reset the state of any saved entities to unmodified
        /// and then apply the entities and anchor that are passed in as if they were downloaded
        /// </summary>
        /// <param name="anchor">Anchor read rom the file</param>
        /// <param name="entities">Entities which were live entities of conflicts and errors which were returned (these can
        /// be applied like downloaded changes).</param>
        public void AddSerializedUploadResponse(byte[] anchor, IEnumerable<OfflineEntity> entities)
        {
            foreach (OfflineCollection collection in Collections.Values)
            {
                collection.ResetSavedEntitiesToUnmodified();
            }

            DownloadedChanges(anchor, entities);
        }



        /// <summary>
        /// Adds local changes that have been serialized to disk
        /// </summary>
        /// <param name="entities">List of entities with changes</param>
        public void AddSerializedLocalChanges(IEnumerable<OfflineEntity> entities)
        {
            foreach (OfflineEntity entity in entities)
            {
                entity.EntityState = OfflineEntityState.Saved;
                Collections[entity.GetType()].AddSerializedEntity(entity);
            }
        }

        public void AddSerializedLocalChange(OfflineEntity entity)
        {
            entity.EntityState = OfflineEntityState.Saved;
            Collections[entity.GetType()].AddSerializedEntity(entity);
        }

        public void AddSerializedDownloadItem(OfflineEntity entity)
        {
            entity.EntityState = OfflineEntityState.Unmodified;
            // Pass the entity to the collection
            Collections[entity.GetType()].AddOrUpdateSyncEntity(entity);
        }

        /// <summary>
        /// Commits outstanding changes and returns clones of the items that where changed.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<OfflineEntity> CommitChanges()
        {
            List<OfflineEntity> changes = new List<OfflineEntity>();

            // Loop over each collection and have it commit the changes in it
            foreach (var collection in Collections.Values)
            {
                changes.AddRange(collection.CommitChanges());
            }

            return changes;
        }

        /// <summary>
        /// Creates the collections for the types in the schema
        /// </summary>
        /// <param name="schema">Schema for which to create collections</param>
        private void CreateCollections(OfflineSchema schema)
        {
            Type collectionType = typeof(OfflineCollection<,>);
            foreach (Type t in schema.Collections)
            {
                // Create the generic type for the type in the collection.
                Type objectType = typeof(Object);
                Type generic = collectionType.MakeGenericType(objectType, t);
                OfflineCollection collection = (OfflineCollection)Activator.CreateInstance(generic);
                Collections[t] = collection;
            }
        }

        /// <summary>
        /// Returns any failures that will be generated during save.
        /// </summary>
        /// <returns>The collection of items that will fail if a save is attempted</returns>
        public ICollection<OfflineEntity> GetSaveFailures()
        {
            List<OfflineEntity> failures = new List<OfflineEntity>();
            foreach (var collection in Collections.Values)
                failures.AddRange(collection.GetSaveFailures());

            return failures;
        }

        /// <summary>
        /// Goes through each collection and rolls back changes.
        /// </summary>
        public void Rollback()
        {
            foreach (var collection in Collections.Values)
            {
                collection.Rollback();
            }
        }

        /// <summary>
        /// Method called by the context to resolve a conflict with StoreItemWins
        /// </summary>
        /// <param name="entity"></param>
        public void ResolveStoreConflictByRollback(OfflineEntity entity)
        {
            Type type = entity.GetType();

            Collections[type].ResolveConflictByRollback(entity);            
        }

        /// <summary>
        /// Adds sync conflicts to the data.
        /// </summary>
        /// <param name="conflicts">List of SyncConflict objects</param>
        /// <param name="context">IsolatedStorageOfflineSyncProvider</param>
        public void AddConflicts(IEnumerable<SyncConflict> conflicts, WinEightContext context)
        {
            foreach (SyncConflict conflict in conflicts)
            {
                AddSyncConflict(conflict, context);
            }
        }

        /// <summary>
        /// Adds a conflict to the list of in-memory
        /// </summary>
        /// <param name="conflict">Conflict to add</param>
        /// <param name="context">Context for which the conflict is being added</param>
        public void AddSyncConflict(SyncConflict conflict, WinEightContext context)
        {
            OfflineEntity entity = Collections[conflict.LiveEntity.GetType()].AddOrUpdateSyncEntity((OfflineEntity)conflict.LiveEntity);
            SyncConflict oldConflict = Collections[conflict.LiveEntity.GetType()].MapSyncConflict(entity, conflict, context);

            SyncConflicts.Add(conflict);

            if (oldConflict != null)
                ClearSyncConflict(oldConflict, context);
        }

        public void AddSerializedConflict(SyncConflict conflict, WinEightContext context)
        {
            Type entityType = conflict.LiveEntity.GetType();
            OfflineEntity entity = Collections[entityType].AddOrUpdateSyncEntity((OfflineEntity)conflict.LiveEntity);
            SyncConflict oldConflict = (SyncConflict)Collections[entityType].MapSyncConflict(entity, conflict, context);

            SyncConflicts.Add(conflict);

            if (oldConflict != null)
                ClearSyncConflict(oldConflict, context);
        }

        /// <summary>
        /// Adds sync errors to the data.
        /// </summary>
        /// <param name="errors">List of errors to add</param>
        /// <param name="context">Context used to clear conflicts</param>
        public void AddErrors(IEnumerable<SyncError> errors, WinEightContext context)
        {
            foreach (SyncError error in errors)
            {
                AddSyncError(error, context);
            }
        }

        /// <summary>
        /// Adds a error to the list of in-memory
        /// </summary>
        /// <param name="error"></param>
        /// <param name="context">Context for the conflict is being added</param>
        public void AddSyncError(SyncError error, WinEightContext context)
        {
            OfflineEntity entity = Collections[error.LiveEntity.GetType()].AddOrUpdateSyncEntity((OfflineEntity)error.LiveEntity);
            SyncError oldError = Collections[error.LiveEntity.GetType()].MapSyncError(entity, error, context);

            SyncErrors.Add(error);

            if (oldError != null)
            {
                ClearSyncError(oldError, context);                
            }
        }

        public void AddSerializedError(SyncError error, WinEightContext context)
        {
            SyncError oldError = (SyncError)Collections[error.ErrorEntity.GetType()].MapSyncError((OfflineEntity)error.LiveEntity, error, context);

            SyncErrors.Add(error);

            if (oldError != null)
            {                
                ClearSyncError(oldError,context);
            }
        }

        /// <summary>
        /// Removes the specified sync conflict
        /// </summary>
        /// <param name="conflict">Conflict to remove</param>
        public void RemoveSyncConflict(SyncConflict conflict)
        {
            SyncConflicts.Remove(conflict);
        }

        /// <summary>
        /// Removes the specified sync error
        /// </summary>
        /// <param name="error">Error to remove</param>
        public void RemoveSyncError(SyncError error)
        {
            SyncErrors.Remove(error);
        }

        /// <summary>
        /// Clears all collections and the anchor blob.
        /// </summary>
        public void Clear()
        {
            ClearCollections();
            SyncConflicts.Clear();
            SyncErrors.Clear();
            AnchorBlob = null;
        }

        /// <summary>
        /// Clears data and anchor but leaves the empty collections in place
        /// </summary>
        public void ClearCollections()
        {
            foreach (OfflineCollection collection in Collections.Values)
                collection.Clear();

            AnchorBlob = null;
        }

        private async void ClearSyncConflict(SyncConflict syncConflict, WinEightContext context)
        {
            RemoveSyncConflict(syncConflict);
            await context.ClearSyncConflict(syncConflict);
        }

        private async void ClearSyncError(SyncError syncError, WinEightContext context)
        {
            RemoveSyncError(syncError);
            await context.ClearSyncError(syncError);
        }

        /// <summary>
        /// Clears All SyncConflicts
        /// </summary>
        public void ClearSyncConflicts()
        {
            foreach (SyncConflict syncConflict in SyncConflicts)
            {
                Collections[syncConflict.LiveEntity.GetType()].ClearSyncConflict((OfflineEntity)syncConflict.LiveEntity);
            }

            SyncConflicts.Clear();
        }

        /// <summary>
        /// Clears All SyncErrors
        /// </summary>
        public void ClearSyncErrors()
        {
            foreach (SyncError syncError in SyncErrors)
            {
                Collections[syncError.LiveEntity.GetType()].ClearSyncError((OfflineEntity)syncError.LiveEntity);
            }

            SyncErrors.Clear();
        }


        internal void NotifyAllCollections()
        {
            foreach (OfflineCollection collection in Collections.Values)
                collection.Notify();
        }

        /// <summary>
        /// Set of collections mapped by the type contained in the collection.  This is generated
        /// once when the application is loaded.
        /// </summary>
        public Dictionary<Type, OfflineCollection> Collections { get; set; }

        /// <summary>
        /// The set of sync conflicts received from the service upon upload.  Currently conflicts
        /// for the same entity are replaced, so there is only ever one for a given entity.  Conflicts
        /// can be cleared by clearing all conflicts at the context level or by clearing individual
        /// conflicts
        /// </summary>
        public List<SyncConflict> SyncConflicts { get; set; }

        /// <summary>
        /// The set of sync errors received from the service upon upload.  Currently errors
        /// for the same entity are replaced, so there is only ever one for a given entity.  Errors
        /// can be cleared by clearing all errors at the context level or by clearing individual
        /// errors
        /// </summary>
        public List<SyncError> SyncErrors { get; set; }

        /// <summary>
        /// The last anchor blob received from the service.  Uploaded during every download and upload
        /// response.
        /// </summary>
        public byte[] AnchorBlob { get; set; }
    }
}
