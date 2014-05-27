using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using Microsoft.Synchronization.ClientServices.Common;
using Windows.UI.Core;
using System.Threading.Tasks;

namespace Microsoft.Synchronization.ClientServices.IsolatedStorage

{
    /// <summary>
    /// This class is the base entity from which all entities used by the isolated
    /// storage offline c must inherit
    /// </summary>
    [DataContract]
    public abstract class OfflineEntity : IOfflineEntity, INotifyPropertyChanged
    {

        /// <summary>
        /// Stores the snapshot of the item.  Original is probably a bad name as it is really
        /// the last known committed version of the entity, whether it was saved or received
        /// from the service.
        /// 
        /// This member is set when a property is modified (OnPropertyChanged).
        /// It is cleared when the changes are either cancelled or accepted.
        /// It is updated when it exists (the entity is modified) and a version for the entity
        /// is received from the service.
        /// </summary>
        OfflineEntitySnapshot original;

        /// <summary>
        /// Specifies the current state of the entity.  It is updated based on various actions
        /// performed on the entity.  See the OfflineEntityState enum definition for descriptions
        /// of the states.
        /// </summary>
        OfflineEntityState state;

        /// <summary>
        /// Specifies whether or not the entity is read-only.  It is not enforced, but is mainly
        /// used as information for clients.  When GetOriginal is called and the object is constructed
        /// and returned, _isReadOnly is set to true.
        /// </summary>
        bool isReadOnly;

        /// <summary>
        /// Stores the information that must be persisted for OData.  The most important attributes
        /// are tombstone and id.
        /// </summary>
        OfflineEntityMetadata entityMetadata;

        /// <summary>
        /// Stores the last sync error and sync conflict that was seen for the entity.  It is updated when
        /// conflicts and errors are received from the store, and when sync conflicts and errors are cleared
        /// by the app.
        /// </summary>
        SyncErrorInfo syncInfo;

        /// <summary>
        /// Specifies whether or not changes should be track and a snapshot is created when properties are modified
        /// Setting this to false ensures that a snapshot will not be created and the state will not be set
        /// to modified if a property is updated.  This is used when applying updates from the service, as they
        /// should not result in state changes or a snapshot being created.
        /// </summary>
        bool trackChanges = true;

        /// <summary>
        /// Used to synchronize the lifetime of the _original object.  This is how concurrent access to the entity
        /// is managed, since it is more performant that managing access through every property set, particularly
        /// when the entity is being deserialized.
        /// </summary>
        object syncRoot = new object();

        /// <summary>
        /// The current store conflict for the item. It is set when the application attempts to save changes
        /// for which there are store conflicts.
        /// </summary>
        OfflineConflict offlineConflict;

        /// <summary>
        /// Need to raise propertyChanged
        /// </summary>
        CoreDispatcher UIDispatcher { get; set; }

        /// <summary>
        /// Protected constructor because class is private.  Initial state of created
        /// entities will be Detached.
        /// </summary>
        protected OfflineEntity()
        {
            this.state = OfflineEntityState.Detached;
            //this.syncInfo = new SyncErrorInfo();
            this.entityMetadata = new OfflineEntityMetadata();
        }



        /// <summary>
        /// Whether the entity is a tombstone.
        /// </summary>
        /// <remarks>
        /// The setter can only be called if the state is detached.  Otherwise,
        /// the tombstone state should be managed by the c.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        //[Display(AutoGenerateField = false)]
        [DataMember]
        public OfflineEntityMetadata ServiceMetadata
        {
            get
            {
                return entityMetadata;
            }

            set
            {
                if (EntityState == OfflineEntityState.Detached)
                {
                    SetServiceMetadata(value);
                }
                else
                {
                    throw new InvalidOperationException("EntityMetadata can only be set when the entity state is Detached.");
                }
            }
        }

        /// <summary>
        /// Event raised whenever a Microsoft.Synchronization.ClientServices.IsolatedStorage.IsolatedStorageOfflineEntity
        /// has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Called from a property setter to notify the framework that a property
        /// is about to be changed.  This method will perform change-tracking related
        /// operations.
        /// </summary>
        /// <param name="propertyName">The name of the property that is changing</param>
        protected void OnPropertyChanging(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentException("propertyName");
            }

            CreateSnapshot();
        }

        /// <summary>
        /// Called from a property setter to notify the framework that a property
        /// has changed.  This method will raise the PropertyChanged event and change
        /// the state to Modified if its current state is Unmodified or Submitted.
        /// </summary>
        /// <param name="propertyName"></param>
        protected void OnPropertyChanged(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException("propertyName");
            }

            RaisePropertyChanged(propertyName);

            if ((EntityState == OfflineEntityState.Unmodified ||
                EntityState == OfflineEntityState.Saved) && trackChanges)
            {
                EntityState = OfflineEntityState.Modified;
            }
        }

        /// <summary>
        /// Gets the original state of the entity at the time it was last Submitted.
        /// </summary>
        /// <returns>The original entity, if the entity has been modified, null otherwise.</returns>
        public OfflineEntity GetOriginal()
        {
            OfflineEntity org = null;

            lock (syncRoot)
            {
                if (this.EntityState == OfflineEntityState.Modified &&
                    this.original != null)
                {
                    org = (OfflineEntity)Activator.CreateInstance(this.GetType());

                    FillEntityFromSnapshot(org);
                    org.IsReadOnly = true;
                }
            }

            return org;
        }

        /// <summary>
        /// Reverts all changes made to the entity since the last time it was Submitted and restores
        /// it to its original state.  If the entity has a store conflict, it will be treated as though
        /// the conflict is resolved with the AcceptStoreEntity resolution.
        /// </summary>
        public void RejectChanges()
        {
            lock (syncRoot)
            {
                if (this.offlineConflict == null)
                {
                    if (state != OfflineEntityState.Modified)
                        throw new InvalidOperationException("Cannot reject changes to unmodified entity");

                    if (entityMetadata.IsTombstone)
                        throw new InvalidOperationException(
                            "Tombstone changes can only be rejected by calling CancelChanges on the c");

                    if (original == null)
                        throw new InvalidOperationException(
                            "Added items can only be rejected by calling CancelChanges on the c");

                    if (original.IsTombstone)
                        throw new InvalidOperationException(
                            "The item snapshot is a tombstone, so the change can only be rejected by calling CancelChanges on the c");

                    FillEntityFromSnapshot(this);
                    original = null;
                }
                else
                {
                    this.offlineConflict.Resolve(SyncConflictResolutionAction.AcceptStoreEntity);
                }
            }
        }

        /// <summary>
        /// Method called when CancelChanges is called on the c.  This method is used so that the
        /// ResolveInternal method can be called on the conflict, which helps avoid a dead lock on the
        /// SaveSyncLock on the c.
        /// </summary>
        internal void RejectChangesInternal()
        {
            lock (syncRoot)
            {
                if (this.offlineConflict == null)
                {
                    FillEntityFromSnapshot(this);
                    original = null;
                }
                else
                {
                    this.offlineConflict.Resolve(SyncConflictResolutionAction.AcceptStoreEntity);
                }
            }

        }

        /// <summary>
        /// Returns whether or not the changes made to the item can be saved.  It will return false if
        /// the snapshot changed as a result of sync.
        /// </summary>
        internal bool CanSaveChanges
        {
            get
            {
                if (state != OfflineEntityState.Modified)
                {
                    // Since this is only called by the c, this should never happen
                    throw new InvalidOperationException("Entity is not modified");
                }

                lock (syncRoot)
                {
                    if (original != null)
                    {
                        if (TickCount != original.TickCount)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        internal bool IsTombstone
        {
            get
            {
                return entityMetadata.IsTombstone;
            }

            set
            {
                if (entityMetadata != null && value != entityMetadata.IsTombstone)
                    entityMetadata.IsTombstone = value;
            }
        }

        /// <summary>
        /// Called when the c is submitting items.  It will throw if the item cannot be accepted.
        /// </summary>
        internal void AcceptChanges()
        {
            if (state != OfflineEntityState.Modified)
            {
                // Since this is only called by the c, this should never happen
                throw new InvalidOperationException("Entity is not modified");
            }

            lock (syncRoot)
            {

                if (original != null)
                {
                    if (TickCount != original.TickCount)
                    {
                        throw new InvalidOperationException("Snapshot has changed since the item was last submitted");
                    }
                }

                state = OfflineEntityState.Saved;
                original = null;
            }
        }

        /// <summary>
        /// Returns whether or not the entity is read-only.
        /// </summary>
        //[Display(AutoGenerateField = false)]

        public bool IsReadOnly
        {
            get
            {
                return isReadOnly;
            }

            internal set
            {
                isReadOnly = value;
            }
        }

        /// <summary>
        /// Returns the current state of the entity.
        /// </summary>
        //[Display(AutoGenerateField = false)]
        public OfflineEntityState EntityState
        {
            get
            {
                return state;
            }

            internal set
            {
                if (state != value)
                {
                    state = value;
                    RaisePropertyChanged("EntityState");
                }
            }
        }

        /// <summary>
        /// Returns whether or not the entity is modified
        /// </summary>
        //[Display(AutoGenerateField = false)]
        public bool HasChanges
        {
            get
            {
                return EntityState == OfflineEntityState.Modified;
            }
        }

        /// <summary>
        /// Returns the class that contains information about any sync conflicts
        /// that may have occurred
        /// </summary>
        [Display(AutoGenerateField = false)]
        public SyncErrorInfo SyncErrorInfo
        {
            get
            {
                return syncInfo;
            }
        }

        /// <summary>
        /// The store conflict that occurred the last time SaveChanges was attempted.
        /// </summary>
        //[Display(AutoGenerateField = false)]
        public OfflineConflict OfflineConflict
        {
            get
            {
                return offlineConflict;
            }

            internal set
            {
                if (value == offlineConflict) return;
                
                offlineConflict = value;
                RaisePropertyChanged("StoreConflict");
            }
        }

        /// <summary>
        /// Returns whether or not the entity has a store conflict 
        /// </summary>
        //[Display(AutoGenerateField = false)]
        public bool HasStoreConflict
        {
            get
            {
                return offlineConflict != null;
            }
        }

        /// <summary>
        /// Tick count used to determine if there are conflicts.
        /// </summary>
        internal ulong TickCount { get; set; }

        /// <summary>
        /// Updates an entity from sync.
        /// </summary>
        /// <param name="entity">Entity with changes to update</param>
        /// <returns>Whether or not the entity is already modified</returns>
        internal void UpdateFromSync(OfflineEntity entity)
        {
            lock (syncRoot)
            {
                if (this.EntityState == OfflineEntityState.Modified)
                {
                    OfflineEntitySnapshot snapshot = GetSnapshotFromEntity(entity);
                    snapshot.IsTombstone = entity.IsTombstone;
                    snapshot.TickCount = this.TickCount + 1;

                    this.original = snapshot;
                }
                else
                {
                    trackChanges = false;
                    // Update properties
                    CopyEntityToThis(entity);
                    trackChanges = true;
                }
            }
        }

        /// <summary>
        /// Does reflection to copy the properties from another entity to this entity
        /// </summary>
        /// <param name="entity">Entity from which to copy properties</param>
        private void CopyEntityToThis(OfflineEntity entity)
        {
            IEnumerable<PropertyInfo> propInfos = GetEntityProperties();

            object[] parameters = new object[] { };
            foreach (PropertyInfo propInfo in propInfos)
            {
                propInfo.SetMethod.Invoke(this, new[] { propInfo.GetMethod.Invoke(entity, parameters) });
            }

            CopyODataPropertiesToThis(entity);
            this.entityMetadata.IsTombstone = entity.entityMetadata.IsTombstone;
        }

        /// <summary>
        /// Copies OData properties that don't need to be snapshoted
        /// </summary>
        /// <param name="entity">Entity from which to copy properties</param>
        private void CopyODataPropertiesToThis(OfflineEntity entity)
        {
            this.entityMetadata.Id = entity.entityMetadata.Id;
            this.entityMetadata.EditUri = entity.entityMetadata.EditUri;
            this.entityMetadata.ETag = entity.entityMetadata.ETag;
        }

        /// <summary>
        /// Creates a snapshot of an entity.
        /// </summary>
        private void CreateSnapshot()
        {
            if (EntityState == OfflineEntityState.Unmodified ||
                EntityState == OfflineEntityState.Saved)
            {
                if (original == null && trackChanges)
                {
                    lock (syncRoot)
                    {
                        if (original == null && trackChanges)
                        {
                            OfflineEntitySnapshot snapshot = GetSnapshotFromEntity(this);
                            original = snapshot;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Uses the snapshot to fill the entity's properties
        /// </summary>
        /// <param name="entity">Entity to fill</param>
        internal void FillEntityFromSnapshot(OfflineEntity entity)
        {
            Type type = entity.GetType();
            foreach (var property in original.Properties)
            {
                PropertyInfo propInfo = type.GetTypeInfo().GetDeclaredProperty(property.Key);

                // If propInfo is null, it's an internal property
                // that will be handled later
                if (propInfo != null)
                {
                    propInfo.SetMethod.Invoke(entity, new[] { property.Value });
                }
            }

            // Get the internal properties
            entity.entityMetadata = original.Metadata;
            entity.EntityState = original.EntityState;
            entity.TickCount = original.TickCount;
        }

        /// <summary>
        /// Sets the metadata for the entity and does any notification.
        /// The property setter asserts on whether or not the entity is attached, but this
        /// method does not
        /// </summary>
        /// <param name="metadata">Metadata to set</param>
        public void SetServiceMetadata(OfflineEntityMetadata metadata)
        {
            if (metadata != entityMetadata)
            {
                entityMetadata = metadata;
                RaisePropertyChanged("EntityMetadata");
            }
        }

        public OfflineEntityMetadata GetServiceMetadata()
        {
            return entityMetadata;
        }

        /// <summary>
        /// Copies the properties from the entity to the snapshot
        /// </summary>
        /// <param name="entity">Entity from which to copy properties</param>        
        private OfflineEntitySnapshot GetSnapshotFromEntity(OfflineEntity entity)
        {
            OfflineEntitySnapshot snapshot = new OfflineEntitySnapshot();
            IEnumerable<PropertyInfo> properties = GetEntityProperties();

            // Copy data properties
            foreach (PropertyInfo property in properties)
            {
                object val = property.GetMethod.Invoke(entity, null);
                snapshot.Properties[property.Name] = val;
            }

            snapshot.TickCount = entity.TickCount;
            snapshot.EntityState = entity.EntityState;
            snapshot.Metadata = entity.ServiceMetadata.Clone();

            return snapshot;
        }

        /// <summary>
        /// Converts an entity to a tombstone and tracks the change.  It also converts all properties which aren't
        /// part of the key to null/default.
        /// </summary>
        internal void DeleteItem()
        {
            lock (syncRoot)
            {
                CreateSnapshot();

                entityMetadata.IsTombstone = true;
                state = OfflineEntityState.Modified;

                // Clear out all properties
                IEnumerable<PropertyInfo> properties = GetEntityNonKeyProperties();
                foreach (PropertyInfo property in properties)
                {
                    property.SetMethod.Invoke(this, new object[] { null });
                }
            }

            // Notify in case there was any binding
            RaisePropertyChanged("ServiceMetadata");
        }

        internal void ApplyDelete()
        {
            lock (syncRoot)
            {
                EntityState = OfflineEntityState.Detached;
                IsTombstone = true;

                // Clear out all properties
                IEnumerable<PropertyInfo> properties = GetEntityNonKeyProperties();
                foreach (PropertyInfo property in properties)
                {
                    property.SetMethod.Invoke(this, new object[] { null });
                }
            }
        }

        /// <summary>
        /// Returns the entity state for the snapshot.
        /// </summary>
        internal OfflineEntityState SnapshotState
        {
            get
            {
                lock (syncRoot)
                {
                    if (original != null)
                    {
                        return original.EntityState;
                    }
                }

                return OfflineEntityState.Detached;
            }

            set
            {
                original.EntityState = value;
            }
        }

        /// <summary>
        /// Creates a new entity by copying the properties.
        /// </summary>
        /// <returns>The cloned entity</returns>
        internal OfflineEntity Clone()
        {
            return (OfflineEntity)this.MemberwiseClone();
        }

        /// <summary>
        /// Returns all properties which are not keys
        /// </summary>
        /// <returns>Array of properties which are not keys</returns>
        private IEnumerable<PropertyInfo> GetEntityNonKeyProperties()
        {
            return (from p in GetEntityProperties()
                    where !p.GetCustomAttributes(typeof(KeyAttribute), false).Any()
                    select p).ToArray();
        }

        /// <summary>
        /// Returns all properties which are keys for the entity
        /// </summary>
        /// <returns>Array of properties which are keys</returns>
        private IEnumerable<PropertyInfo> GetEntityKeyProperties()
        {
            return GetEntityKeyProperties(base.GetType());
        }

        /// <summary>
        /// Returns all properties of the entity which are passed for sync (all properties which have 
        /// getters and setters).
        /// </summary>
        /// <returns>Array of entity properties</returns>
        private IEnumerable<PropertyInfo> GetEntityProperties()
        {
            return GetEntityProperties(base.GetType());
        }

        /// <summary>
        /// Returns all properties of the specified type which are passed for sync (all properties which have 
        /// getters and setters).
        /// </summary>
        /// <param name="t">Type from which to retrieve properties</param>
        /// <returns>Properties for the type</returns>
        internal static PropertyInfo[] GetEntityProperties(Type t)
        {
            return (from p in t.GetTypeInfo().DeclaredProperties
                    where p.GetMethod != null && p.SetMethod != null && p.DeclaringType == t
                    select p).ToArray();
        }

        /// <summary>
        /// Returns all properties of the specified type which are keys for the type
        /// </summary>
        /// <param name="t">Type from which to retrieve properties</param>
        /// <returns>Key propeties for the type.</returns>
        internal static PropertyInfo[] GetEntityKeyProperties(Type t)
        {
            return (from p in GetEntityProperties(t)
                    where p.GetCustomAttributes(typeof(KeyAttribute), false).Count() != 0
                    select p).ToArray();
        }

        /// <summary>
        /// Notifies the PropertyChanged event if it is registered.
        /// </summary>
        /// <param name="propertyName">Name of the property for which the event is being raised.</param>
        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler == null) return;

            // TODO : How to set this in Windows Phone .
            //if (UIDispatcher == null && Window.Current != null)
            //    UIDispatcher = Window.Current.Dispatcher;

            if (UIDispatcher == null)
                return;
                
            if (UIDispatcher.HasThreadAccess)
                handler(this, new PropertyChangedEventArgs(propertyName));
            else
                UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => handler(this, new PropertyChangedEventArgs(propertyName)));
        }

        /// <summary>
        /// Reflects on the keys for the entity and returns a representation of its key.
        /// </summary>
        /// <returns>The object which is the key for the entity</returns>
        internal object GetIdentity()
        {
            OfflineEntityKey key = new OfflineEntityKey();

            IEnumerable<PropertyInfo> propInfos = GetEntityKeyProperties();

            foreach (PropertyInfo propInfo in propInfos)
            {
                key.AddKey(propInfo.Name, propInfo.GetMethod.Invoke(this, new object[] { }));
            }

            return key;
        }

        /// <summary>
        /// When resolving store conflicts in favor of the modified entity, this is called to update
        /// tick counts
        /// </summary>
        internal void UpdateModifiedTickCount()
        {
            lock (syncRoot)
            {
                if (original != null)
                {
                    this.TickCount = original.TickCount;
                }
            }
        }

        /// <summary>
        /// Returns whether or not the entity has a snapshot.
        /// </summary>
        internal bool HasSnapshot
        {
            get
            {
                lock (syncRoot)
                {
                    return original != null;
                }
            }
        }

        /// <summary>
        /// Returns the object used to lock the entity
        /// </summary>
        internal object SyncRoot
        {
            get
            {
                return syncRoot;
            }
        }



      
    }
}
