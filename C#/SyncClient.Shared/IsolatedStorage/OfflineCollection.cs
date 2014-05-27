using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.Synchronization.ClientServices.Common;
using Microsoft.Synchronization.ClientServices.IsolatedStorage;
using Windows.Foundation.Collections;

namespace Microsoft.Synchronization.ClientServices.IsolatedStorage
{
    /// <summary>
    /// Abstract class that serves as the base class for all collections synchronized from the server.
    /// </summary>
    public abstract class OfflineCollection
    {
        public abstract void AddSerializedEntity(OfflineEntity entity);

        public abstract OfflineEntity AddOrUpdateSyncEntity(OfflineEntity entity);

        public abstract OfflineEntity AddOrUpdateSyncEntity(OfflineEntity entity,
                                                                           bool delayNotification);

        public abstract void ResetSavedEntitiesToUnmodified();

        public abstract SyncConflict MapSyncConflict(OfflineEntity entity, SyncConflict conflict,
                                                     WinEightContext context);

        public abstract SyncError MapSyncError(OfflineEntity entity, SyncError error,
                                               WinEightContext context);

        public abstract void Clear();
        public abstract IEnumerable<OfflineEntity> GetSaveFailures();
        public abstract IEnumerable<OfflineEntity> CommitChanges();
        public abstract void Rollback();
        public abstract void ResolveConflictByRollback(OfflineEntity entity);
        public abstract void Notify();
        public abstract void ClearSyncConflict(OfflineEntity entity);
        public abstract void ClearSyncError(OfflineEntity entity);
    }


    public class OfflineCollection<U, T> : OfflineCollection, IObservableVector<U>,
                                                   INotifyPropertyChanged, INotifyCollectionChanged
        where T : OfflineEntity
        where U : class
    {
        /// <summary>
        /// Synchronizes access for the collection generated for enumeration.
        /// </summary>
        private readonly object _syncRoot = new object();

        /// <summary>
        /// Collection that allows linear and random access to entities.  It contains the same entities as the _entityMap.  It is
        /// mainly beneficial for providing accurate collection notification change events.  But it is also helpful for providing
        /// enumeration and will be beneficial should random access to the collection be exposed in the future.  It is always
        /// updated when the _entityMap is updated.
        /// </summary>
        private readonly List<T> entityList;

        /// <summary>
        /// Collection that allows lookup of entities that are part of the collection by primary key.  It is updated whenever
        /// an entity is added or removed from the collection, either by sync or by the application.
        /// </summary>
        private readonly Dictionary<object, T> entityMap;

        /// <summary>
        /// The collection of unsaved tombstones.  They are added whenever the application requests the deletion of an item.
        /// The collectoin is cleared when changes are saved or when the changes are cancelled.
        /// </summary>
        private readonly List<T> tombstones;

        public OfflineCollection()
        {
            entityMap = new Dictionary<object, T>();
            entityList = new List<T>();
            tombstones = new List<T>();
        }

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region IObservableVector<U> Members

        public event VectorChangedEventHandler<U> VectorChanged;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<U> IEnumerable<U>.GetEnumerator()
        {
            List<T> enumList;
            lock (_syncRoot)
            {
                enumList = new List<T>(entityList);
            }
            return ((IEnumerable<U>) enumList).GetEnumerator();
        }

        /// <summary>
        /// Clears the in-memory data.
        /// </summary>
        public override void Clear()
        {
            lock (_syncRoot)
            {
                entityMap.Clear();
                entityList.Clear();
                tombstones.Clear();

                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                OnVectorChanged(new VectorChangedEventArgs(CollectionChange.Reset, 0));
                OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            }
        }

        public int IndexOf(U item)
        {
            // Have to do a search in the entity list to get the item anyway,
            // so get the index to avoid searching twice and allow better collection changed notification.
            return entityList.IndexOf(item as T);
        }


        public void RemoveAt(int index)
        {
            T item = entityList[index];

            Remove(item);
        }

        public U this[int index]
        {
            get { return entityList[index] as U; }
            set { entityList[index] = value as T; }
        }


        public void Add(U item)
        {
            Add(item as T);
        }

        public bool Contains(U item)
        {
            return Contains(item as T);
        }

        public void CopyTo(U[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return entityList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }


        public bool Remove(U item)
        {
            return item != null && Remove(item as T);
        }

        public void Insert(int index, U item)
        {
            Insert(index, item as T);
        }

        #endregion

        public IEnumerator<U> GetEnumerator()
        {
            List<T> enumList;
            lock (_syncRoot)
            {
                enumList = new List<T>(entityList);
            }

            return ((IEnumerable<U>) enumList).GetEnumerator();
        }

        /// <summary>
        /// Adds an item that was serialized from a download response.  This item will be
        /// blindly added, rather than updating the entity that is already there.
        /// </summary>
        /// <param name="entity">The entity to add to the collection.  If the entity with the same keys already exists,
        /// that entity will be updated.  Otherwise, the passed-in entity will be returned</param>
        public override void AddSerializedEntity(OfflineEntity entity)
        {
            // Retrieve the key for the item
            object key = entity.GetIdentity();

            // If the incoming entity is a tombstone
            if (entity.IsTombstone)
            {
                // If the item exists, remove it
                T t;
                if (entityMap.TryGetValue(key, out t))
                {
                    entityMap.Remove(key);
                    entityList.Remove(t);
                }

                // Don't need to worry about adding a Saved entity to tombstones because
                // the disk layer manages saved tombstones.
            }
            else
            {
                T current;

                // If the entity exists, update its properties
                if (entityMap.TryGetValue(key, out current))
                {
                    current.UpdateFromSync(entity);
                }

                    // Otherwise add the entity to the collection (no notification because its a serialized item
                    // which means that the app doesn't have a reference
                else
                {
                    entityList.Add((T) entity);
                    entityMap.Add(key, (T) entity);
                }
            }
        }

        /// <summary>
        /// Handles downloaded changes from sync.
        /// </summary>
        /// <param name="entity">Entity whose values should get added.</param>
        /// <returns>The entity that was updated (the one in the collection if it is not a tombstone)</returns>
        public override OfflineEntity AddOrUpdateSyncEntity(OfflineEntity entity)
        {
            return AddOrUpdateSyncEntity(entity, false);
        }

        /// <summary>
        /// Handles downloaded changes from sync. During first sync we delay notifying the client or databinder until
        /// EndSession. In EndSession the NotifyAllCollections is called notifying the entire collection to the databinder instead of per item
        /// </summary>
        /// <param name="entity">Entity whose values should get added.</param>
        /// <param name="delayNotification">notify the databinder only at the end of the session instead of per entity</param>
        /// <returns>The entity that was updated (the one in the collection if it is not a tombstone)</returns>
        public override OfflineEntity AddOrUpdateSyncEntity(OfflineEntity entity,
                                                                           bool delayNotification)
        {
            T localItem = null;
            NotifyCollectionChangedEventArgs eventArgs = null;
            VectorChangedEventArgs vectorChangedEventArgs = null;
            PropertyChangedEventArgs propertyChangedEventArgs = null;
            int index = -1;
            bool foundInTombstoneList = false;

            string atomId = entity.ServiceMetadata.Id;

            // Find the local copy of the item
            if (entity.IsTombstone && !string.IsNullOrEmpty(atomId))
            {
                // If the incoming entity is a tombstone and has an atom id, we need to search by atom id
                for (int i = 0; i < entityList.Count; ++i)
                {
                    if (atomId.Equals(entityList[i].ServiceMetadata.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        index = i;
                        localItem = entityList[i];
                        break;
                    }
                }

                // If it's not live, see if it's an unsaved tombstone
                if (localItem == null)
                {
                    for (int i = 0; i < tombstones.Count; ++i)
                    {
                        if (tombstones[i].ServiceMetadata.Id == atomId)
                        {
                            index = i;
                            localItem = tombstones[i];
                            foundInTombstoneList = true;
                            break;
                        }
                    }
                }
            }

            // if the local item is still null at this point, the incoming entity is either a live item
            // or it could be a tombstone for which there was a local insert and there was an insert-delete
            // conflict, so search by key
            if (localItem == null)
            {
                object key = entity.GetIdentity();
                // If there's no atom id, this may be an insert that failed, so get entity based on key
                if (entityMap.TryGetValue(key, out localItem))
                {
                    for (int i = 0; i < entityList.Count; ++i)
                    {
                        if (entityList[i].GetIdentity().Equals(key))
                        {
                            index = i;
                            foundInTombstoneList = false;
                            break;
                        }
                    }

                    Debug.Assert(index >= 0);
                }
                else
                {
                    // If it's not in the live list, see if it's an unsaved tomstone
                    for (int i = 0; i < tombstones.Count; ++i)
                    {
                        if (tombstones[i].GetIdentity().Equals(key))
                        {
                            index = i;
                            localItem = tombstones[i];
                            foundInTombstoneList = true;
                            break;
                        }
                    }
                }
            }

            if (entity.IsTombstone)
            {
                // If the local item was found somewhere, update it
                if (localItem != null)
                {
                    lock (localItem.SyncRoot)
                    {
                        if (!localItem.IsTombstone)
                        {
                            // If the local item is live and unmodified, remove it
                            if (localItem.EntityState == OfflineEntityState.Unmodified)
                            {
                                object key = localItem.GetIdentity();

                                entityMap.Remove(key);
                                Debug.Assert(!foundInTombstoneList && index >= 0);
                                entityList.RemoveAt(index);

                                localItem.ApplyDelete();

                                if (!delayNotification)
                                {
                                    eventArgs =
                                        new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                                                                             localItem, index);
                                    vectorChangedEventArgs = new VectorChangedEventArgs(CollectionChange.ItemRemoved,
                                                                                        (uint) index);
                                    propertyChangedEventArgs = new PropertyChangedEventArgs("Count");
                                }
                            }
                            else
                            {
                                // If it is modified, call UpdateFromSync to update the snapshot (will cause
                                // a store conflict when SaveChanges is called)
                                localItem.UpdateFromSync(entity);
                            }
                        }
                        else
                        {
                            // Remove it from the tombstones.  Technically this is a store conflict, but either
                            // resolution will result in the same thing, and doing a CancelChanges will be a no-op
                            // so just remove it now
                            Debug.Assert(foundInTombstoneList && index >= 0);
                            tombstones.RemoveAt(index);
                        }
                    }
                }
            }
            else
            {
                // If the local item is not null, update it from sync
                if (localItem != null)
                {
                    lock (localItem.SyncRoot)
                    {
                        localItem.UpdateFromSync(entity);

                        if (!delayNotification)
                            vectorChangedEventArgs = new VectorChangedEventArgs(CollectionChange.ItemChanged,
                                                                                (uint) (entityList.IndexOf(localItem)));
                    }
                }
                else
                {
                    // Handle an incoming live item
                    object key = entity.GetIdentity();
                    // There is no local item so insert it
                    var entityT = (T) entity;
                    // Insert item
                    entity.EntityState = OfflineEntityState.Unmodified;
                    entityMap.Add(key, entityT);
                    entityList.Add(entityT);
                    localItem = entityT;
                    if (!delayNotification)
                    {
                        eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, entity,
                                                                         entityList.Count - 1);
                        vectorChangedEventArgs = new VectorChangedEventArgs(CollectionChange.ItemInserted,
                                                                            (uint) (entityList.Count - 1));
                        propertyChangedEventArgs = new PropertyChangedEventArgs("Count");
                    }
                }
            }

            // If the local entity is modified, notify that it was changed
            //if (localItem != null && localItem.EntityState == OfflineOfflineEntityState.Modified)
            //{
            //    OnModifiedItemChanged(localItem);
            //}

            // If an entity was added or removed, trigger the notification
            if (eventArgs != null)
                OnCollectionChanged(eventArgs);

            if (vectorChangedEventArgs != null)
                OnVectorChanged(vectorChangedEventArgs);

            if (propertyChangedEventArgs != null)
                OnPropertyChanged(propertyChangedEventArgs);


            return localItem;
        }

        /// <summary>
        /// Called from CacheData.NotifyAllCollections to notify the databinder only once
        /// during initial sync rather than for every item
        /// </summary>
        public override void Notify()
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            OnVectorChanged(new VectorChangedEventArgs(CollectionChange.Reset, 0));
            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
        }

        /// <summary>
        /// This method should only be used when reading at the beginning.  It retrieves an existing entity or adds it if it is
        /// not found
        /// </summary>
        /// <param name="entity">entity to look up</param>
        /// <returns>Entity which is in the collection</returns>
        private T GetEntity(T entity)
        {
            T t;

            object key = entity.GetIdentity();

            // Try to get the entity
            if (!entityMap.TryGetValue(key, out t))
            {
                // If it doesn't exist, check the tombstones
                t = (from tombstone in tombstones
                     where tombstone.GetIdentity().Equals(key)
                     select tombstone).FirstOrDefault();
            }

            return t;
        }

        /// <summary>
        /// Sets the entity state from Saved to Modified.  This should only ever be
        /// called during deserialization.
        /// </summary>       
        public override void ResetSavedEntitiesToUnmodified()
        {
            IEnumerable<T> query = from s in entityList
                                   where (s.EntityState == OfflineEntityState.Saved) ||
                                         (s.EntityState == OfflineEntityState.Modified &&
                                          s.SnapshotState == OfflineEntityState.Saved)
                                   select s;

            foreach (T entity in query)
            {
                if (entity.EntityState != OfflineEntityState.Saved &&
                    (entity.EntityState != OfflineEntityState.Modified ||
                     entity.SnapshotState != OfflineEntityState.Saved))
                {
                    throw new InvalidOperationException("Entity state should be Saved");
                }

                if (entity.EntityState == OfflineEntityState.Saved)
                {
                    entity.EntityState = OfflineEntityState.Unmodified;
                }
                else
                {
                    entity.SnapshotState = OfflineEntityState.Unmodified;
                }
            }
        }

        /// <summary>
        /// Ensures that the references between the conflict and the live entity are correct.
        /// </summary>
        /// <param name="mapEntity">IsolatedStorageOfflineEntity</param>
        /// <param name="conflict">The new conflict to set on the entity</param>
        /// <param name="context">The overall c so the conflict can be cleared</param>
        /// <returns>The old conflict that was on the item</returns>
        public override SyncConflict MapSyncConflict(OfflineEntity mapEntity, SyncConflict conflict,
                                                     WinEightContext context)
        {
            // If the map entity is null, it is likely the case that we have a serialized
            // conflict, so find it in the collection
            if (mapEntity == null)
            {
                mapEntity = GetEntity((T) conflict.LosingEntity);

                // If it's not in the collection, it was deleted by an upload response, so clone
                // the losing entity and turn it into a tombstone, since we need a representation
                // of the live entity
                if (mapEntity == null)
                {
                    mapEntity = ((OfflineEntity) conflict.LosingEntity).Clone();
                    mapEntity.ApplyDelete();
                }
            }

            SyncConflict oldConflict = mapEntity.SyncErrorInfo.SyncConflict;

            mapEntity.SyncErrorInfo.SetSyncConflict(context, conflict);
            conflict.LiveEntity = mapEntity;

            return oldConflict;
        }

        /// <summary>
        /// Ensures that the references between the error and the live entity are correct.
        /// </summary>
        /// <param name="mapEntity">IsolatedStorageOfflineEntity</param>
        /// <param name="error">The new error to set on the entity</param>
        /// <param name="context">The overall c so the error can be cleared</param>
        /// <returns>The old error that was on the item</returns>
        public override SyncError MapSyncError(OfflineEntity mapEntity, SyncError error,
                                               WinEightContext context)
        {
            // If the map entity is null, it is likely the case that we have a serialized
            // error, so find it in the collection
            if (mapEntity == null)
            {
                mapEntity = GetEntity((T) error.ErrorEntity);

                // If it's not in the collection, it was deleted by an upload response, so clone
                // the losing entity and turn it into a tombstone, since we need a representation
                // of the live entity
                if (mapEntity == null)
                {
                    mapEntity = ((OfflineEntity) error.ErrorEntity).Clone();
                    mapEntity.ApplyDelete();
                }
            }

            SyncError oldError = mapEntity.SyncErrorInfo.SyncError;

            mapEntity.SyncErrorInfo.SetSyncError(context, error);
            error.LiveEntity = mapEntity;

            return oldError;
        }

        /// <summary>
        /// Sets the SyncConflict from the Entity to null.
        /// </summary>
        /// <param name="entity"></param>
        public override void ClearSyncConflict(OfflineEntity entity)
        {
            lock (_syncRoot)
            {
                object key = entity.GetIdentity();
                T localItem;

                if (entityMap.TryGetValue(key, out localItem))
                {
                    //Note: We can call ClearSyncConflict on the SyncErrorInfo,but, that would
                    //Cause a deadlock. We would need to get the _cacheData.ClearSyncConflicts() ouside the _saveSyncLock in
                    //IsoContext.ClearConflicts(). This "might" expose to unsafe access.
                    localItem.SyncErrorInfo.UnsafeClearSyncConflict();
                }
            }
        }

        /// <summary>
        /// Sets the SyncError from the Entity to null.
        /// </summary>
        /// <param name="entity"></param>
        public override void ClearSyncError(OfflineEntity entity)
        {
            lock (_syncRoot)
            {
                object key = entity.GetIdentity();
                T localItem;

                if (entityMap.TryGetValue(key, out localItem))
                {
                    //Note: We can call ClearSyncError on the SyncErrorInfo,but, that would
                    //Cause a deadlock. We would need to get the _cacheData.ClearSyncErrors() ouside the _saveSyncLock in
                    //IsoContext.ClearSyncErrors(). This "might" expose to unsafe access.
                    localItem.SyncErrorInfo.UnsafeClearSyncError();
                }
            }
        }

        /// <summary>
        /// Returns any failures that would happen if attempting to save changes.
        /// </summary>
        /// <returns>List of failures.</returns>
        public override IEnumerable<OfflineEntity> GetSaveFailures()
        {
            IEnumerable<OfflineEntity> liveFailures = from e in entityMap.Values
                                                                     where
                                                                         (e.EntityState == OfflineEntityState.Modified &&
                                                                          !e.CanSaveChanges)
                                                                     select (OfflineEntity) e;

            IEnumerable<OfflineEntity> tombstoneFailures = from e in tombstones
                                                                          where
                                                                              (e.EntityState ==
                                                                               OfflineEntityState.Modified &&
                                                                               !e.CanSaveChanges)
                                                                          select (OfflineEntity) e;

            // concat them to return
            return liveFailures.Concat(tombstoneFailures);
        }

        /// <summary>
        /// Commits any modified changes in the collection, 
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<OfflineEntity> CommitChanges()
        {
            var changes = new List<OfflineEntity>();

            IEnumerable<T> changeQuery = (from e in entityMap.Values
                                          where e.EntityState == OfflineEntityState.Modified
                                          select e).Concat(tombstones);

            foreach (T entity in changeQuery)
            {
                entity.AcceptChanges();
                changes.Add(entity.Clone());
            }

            tombstones.Clear();

            return changes;
        }

        /// <summary>
        /// Reverts any pending changes.
        /// </summary>
        public override void Rollback()
        {
            // Find the items that have changed
            List<T> changeQuery = (from e in entityMap.Values
                                   where e.EntityState == OfflineEntityState.Modified
                                   select e).ToList();

            var changesToRemove = new List<OfflineEntity>();

            foreach (T entity in changeQuery)
            {
                // If the entity has a snapshot, it's a change to an entity, so
                // just rollback.
                if (entity.HasSnapshot)
                {
                    entity.RejectChangesInternal();

                    // Check for tombstone at this point.  If this happens, a tombstone was
                    // brought in during sync for a modified entity.
                    if (entity.IsTombstone)
                    {
                        changesToRemove.Add(entity);
                    }
                }
                else
                {
                    // If there's no snapshot, the entity was a create, so remove it
                    changesToRemove.Add(entity);
                }
            }

            // remove the changes that need to be removed
            foreach (OfflineEntity entity in changesToRemove)
            {
                entityMap.Remove(entity.GetIdentity());
                entityList.Remove((T) entity);

                entity.EntityState = OfflineEntityState.Detached;
            }

            // now rollback tombstones
            foreach (T entity in tombstones.ToList())
            {
                if (entity.HasStoreConflict)
                {
                    entity.RejectChangesInternal();
                }
                else
                {
                    // Get the original
                    var original = (T) entity.GetOriginal();

                    // If the original is not a tombstone add it back.
                    if (original != null && !original.IsTombstone)
                    {
                        original.IsReadOnly = false;
                        entityMap.Add(original.GetIdentity(), original);
                        entityList.Add(original);
                    }
                }
            }

            // Since only non-saved tombstones exist here, revert them.
            tombstones.Clear();
        }

        /// <summary>
        /// Resolves a conflict by rolling back a specific change.
        /// </summary>
        /// <param name="entity"></param>
        public override void ResolveConflictByRollback(OfflineEntity entity)
        {
            lock (entity.SyncRoot)
            {
                bool modifyIsTombstone = entity.IsTombstone;
                bool snapshotIsTombstone;
                var entityT = (T) entity;

                // Entity must have a snapshot for there to be a conflict
                Debug.Assert(entityT.HasSnapshot);

                // Fill the entity with its snapshot
                entityT.FillEntityFromSnapshot(entityT);

                snapshotIsTombstone = entityT.IsTombstone;

                // We should prevent delete-delete conflicts sooner, so check here to make
                // sure they're not happening
                Debug.Assert(!modifyIsTombstone || !snapshotIsTombstone);

                // If the modified entity was a tombstone, remove it from the tombstone and add it to the
                // entities
                if (modifyIsTombstone)
                {
                    // Remove it from the tombstone list
                    tombstones.Remove(entityT);

                    // Add the live snapshot back to the live items list
                    entityMap.Add(entity.GetIdentity(), entityT);
                    entityList.Add(entityT);

                    // Notify any databinding
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                                            NotifyCollectionChangedAction.Add, entity, entityList.Count - 1));

                    OnVectorChanged(new VectorChangedEventArgs(CollectionChange.ItemInserted,
                                                               (uint) (entityList.Count - 1)));

                    OnPropertyChanged(new PropertyChangedEventArgs("Count"));
                }
                    // If the snapshot is a tombstone, remove it from the live entities
                else if (snapshotIsTombstone)
                {
                    // Remove the item from the list of live items
                    entityMap.Remove(entity.GetIdentity());

                    int index = entityList.IndexOf(entityT);
                    entityList.RemoveAt(index);

                    // Note: We don't add to tombstones list here because that only contains items that are
                    // deleted locally, not deletes brought in by sync (which is how the tombstone would be a
                    // snapshot).

                    // Notify any databinding
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                                            NotifyCollectionChangedAction.Remove, entity, index));

                    OnVectorChanged(new VectorChangedEventArgs(CollectionChange.ItemRemoved, (uint) (index)));

                    OnPropertyChanged(new PropertyChangedEventArgs("Count"));
                }
            }
        }

        /// <summary>
        /// Removes the specified time and marks it as a tombstone
        /// </summary>
        /// <param name="item">Item to remove</param>
        public void DeleteItem(U item)
        {
            Remove(item as T);
        }


        /// <summary>
        /// Method that calls the CollectionChanged event.
        /// </summary>
        /// <param name="args"></param>
        private void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            NotifyCollectionChangedEventHandler handler = CollectionChanged;

            if (handler != null)
                handler(this, args);
        }

        private void OnVectorChanged(VectorChangedEventArgs args)
        {
            VectorChangedEventHandler<U> handler = VectorChanged;

            if (handler != null)
                handler(this, args);
        }

        private void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
                handler(this, args);
        }


        public void Add(T item)
        {
            object key = item.GetIdentity();

            if (entityMap.ContainsKey(key))
            {
                throw new ArgumentException(
                    String.Format("An item with the same primary keys already exists in the collection for type {0}",
                                  typeof (T).FullName));
            }

            item.EntityState = OfflineEntityState.Modified;
            entityMap.Add(key, item);
            entityList.Add(item);

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item,
                                                                     entityList.Count - 1));
            OnVectorChanged(new VectorChangedEventArgs(CollectionChange.ItemInserted, (uint) (entityList.Count - 1)));
            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
        }


        public bool Contains(T item)
        {
            object key = item.GetIdentity();

            T t;

            return entityMap.TryGetValue(key, out t);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            if (item == null)
                return false;

            object key = item.GetIdentity();

            T t;

            // Verify that this is an item that can be removed.
            if (!entityMap.TryGetValue(key, out t))
                return false;

            entityMap.Remove(key);

            // Have to do a search in the entity list to get the item anyway,
            // so get the index to avoid searching twice and allow better collection changed notification.
            int index = entityList.IndexOf(t);

            Debug.Assert(index >= 0 && index < entityList.Count);

            entityList.RemoveAt(index);

            t.DeleteItem();
            tombstones.Add(t);

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, t, index));
            OnVectorChanged(new VectorChangedEventArgs(CollectionChange.ItemRemoved, (uint) index));
            OnPropertyChanged(new PropertyChangedEventArgs("Count"));

            return true;
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }
    }

    public class VectorChangedEventArgs : IVectorChangedEventArgs
    {
        private readonly CollectionChange collectionChange;
        private readonly uint index;

        public VectorChangedEventArgs(CollectionChange collectionChange, uint index)
        {
            this.collectionChange = collectionChange;
            this.index = index;
        }

        #region IVectorChangedEventArgs Members

        public CollectionChange CollectionChange
        {
            get { return collectionChange; }
        }

        public uint Index
        {
            get { return index; }
        }

        #endregion
    }
}