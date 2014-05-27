
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Synchronization.ClientServices.Common;
using Windows.Storage;

namespace Microsoft.Synchronization.ClientServices.IsolatedStorage
{
    /// <summary>
    /// IsolatedStorageOfflineContext
    /// </summary>
    public class WinEightContext : OfflineSyncProvider, IDisposable
    {
        #region  Members

        /// <summary>
        /// This member stores the in-memory data for the c.  It is returned by the
        /// _storageHandler.Load method when Load is executed
        /// </summary>
        protected CacheData cacheData = null;

        /// <summary>
        /// Specifies whether or not the c is loaded.  It is set when the c has been
        /// successfully loaded. It is guared by the _loadLock
        /// </summary>
        private volatile bool loaded;

        /// <summary>
        /// Essentially guards the _cacheData object.  Prevents multiple accesses that result in
        /// modification of the _cacheData object.  Also used to prevent save during sync.
        /// </summary>
        private AutoResetLock saveSyncLock;

        /// <summary>
        /// The scope uri for the c.  Passed in to the constructor.
        /// </summary>
        private readonly Uri scopeUri;

        /// <summary>
        /// The scope name for the c. Passed in to the constructor.
        /// </summary>
        private readonly string scopeName;

        /// <summary>
        /// The cache path for the c. Passed in to the constructor.
        /// </summary>
        private readonly string cachePath;

        /// <summary>
        /// Cache controller generated as a convenience for the user.  Created
        /// in the constructor.
        /// </summary>
        private CacheController cacheController;

        /// <summary>
        /// Schema passed in to the constructor.  Passed to the storage handler
        /// so that the appropriate collections can be created.
        /// </summary>
        private readonly OfflineSchema schema;

        /// <summary>
        /// List of store conflicts. Created during the SaveChanges call, and
        /// cleaned up as the conflicts are resolved or the changes are cancelled.
        /// </summary>
        private IList<OfflineConflict> offlineConflicts = new List<OfflineConflict>();

        /// <summary>
        /// Specifies that sync is active.  It is set to true in BeginSession and 
        /// set to false in EndSession.
        /// </summary>
        private volatile bool syncActive;

        /// <summary>
        /// Specifies that the c has been disposed.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Used to detect if this is the first sync to the server.
        /// This is used to send notification to the databinder only once
        /// instead of each item.
        /// </summary>
        private bool isFirstSync;

        /// <summary>
        /// Event called when the LoadAsync is completed.  This will be called even if the cache is already loaded.
        /// </summary>
        public event EventHandler<LoadCompletedEventArgs> LoadCompleted;

        /// <summary>
        /// StorageHandler used to store and serialize entities
        /// </summary>
        internal StorageHandler StorageHandler { get; private set; }

        /// <summary>
        /// Returns the cache path of the c.
        /// </summary>
        public string CachePath
        {
            get
            {
                ThrowIfDisposed();
                return cachePath;
            }
        }

        /// <summary>
        /// Returns the isolatedStorageSchema used by the c.  Any changes to the isolatedStorageSchema after instantiation of the c
        /// will be ignored.
        /// </summary>
        public OfflineSchema Schema
        {
            get
            {
                ThrowIfDisposed();
                return schema;
            }
        }

        /// <summary>
        /// The Uri of the scope used for sync.
        /// </summary>
        public Uri ScopeUri
        {
            get
            {
                ThrowIfDisposed();
                return scopeUri;
            }
        }

        /// <summary>
        /// A preinitialized CacheController which can be used to synchronize the c with the uri specified
        /// in the constructor.
        /// </summary>
        public CacheController CacheController
        {
            get
            {
                ThrowIfDisposed();
                return cacheController;
            }
        }

        /// <summary>
        /// Returns the symmetric encryption algorithm specified in the constructor in order to encrypt files on disk.
        /// If this property is null, the data will not be encrypted.
        /// </summary>
        public bool IsEncrypted
        {
            get
            {
                ThrowIfDisposed();
                return false;
            }
        }

        
        #endregion

        /// <summary>
        /// Constructor for the WinEightStorageOfflineContext
        /// </summary>
        /// <param name="schema">The WinEightStorageSchema that specifies the set of the collections for the c.</param>
        /// <param name="scopeName">The scope name used to identify the scope on the service.</param>
        /// <param name="cachePath">Path in isolated storage where the data will be stored.</param>
        /// <param name="uri">Uri of the scope.  Used to intialize the CacheController.</param>
        /// <remarks>
        /// If the Uri specified is different from the one that is stored in the cache path, the
        /// Load method will throw an InvalidOperationException.
        /// </remarks>
        public WinEightContext(OfflineSchema schema, string scopeName, string cachePath,
                                             Uri uri)
        {
            if (schema == null)
                throw new ArgumentNullException("OfflineSchema");

            if (string.IsNullOrEmpty(scopeName))
                throw new ArgumentNullException("scopeName");

            if (string.IsNullOrEmpty(cachePath))
                throw new ArgumentNullException("cachePath");

            if (uri == null)
                throw new ArgumentNullException("uri");

            this.isDisposed = false;
            this.schema = schema;
            this.scopeUri = uri;
            this.scopeName = scopeName;
            this.cachePath = cachePath;
            this.StorageHandler = new StorageHandler(schema, cachePath);
            this.saveSyncLock = new AutoResetLock();
            this.CreateCacheController();
        }


        /// <summary>
        /// Loads the data from the cache into memory asynchronously.
        /// </summary>
        /// <remarks>
        /// Performing any method on the cache will implicitly load it if this method is not called. This
        /// method allows better control over when data is loaded.
        /// If the cache is already loaded, this method will do nothing.
        /// </remarks>
        public async Task LoadAsync()
        {
            ThrowIfDisposed();

            // Use the ThreadPool to queue our load.  This will happen regardless of whether the cache is already loaded
            if (loaded)
                return;
           
            Exception exception = null;

            try
            {
                if (!loaded)
                {
                    // Verify the isolatedStorageSchema and uri match was was previously used for the cache path.
                    await CheckSchemaAndUri(cachePath, schema, scopeUri, scopeName);

                    // Load the data.
                    // cacheData may be not null if we made a sync after a clearcache action
                    if (cacheData == null)
                        cacheData = await StorageHandler.Load(this);

                    loaded = true;
                }
            }
            catch (Exception e)
            {
                if (ExceptionUtility.IsFatal(e))
                    throw;

                // Catch the exception and store it.
                exception = e;
            }

            // Pass the event args (including the exception to the callback).
            EventHandler<LoadCompletedEventArgs> loadCompleted = LoadCompleted;
            
            if (loadCompleted != null)
                loadCompleted(this, new LoadCompletedEventArgs(exception));
        }


        /// <summary>
        /// Get the collection of entities corresponding to the desired type.  This method will load if the cache is not already
        /// loaded.
        /// </summary>
        /// <typeparam name="T">Type of entity to return</typeparam>
        /// <returns>An IEnumerable of the entities requested</returns>
        public async Task<OfflineCollection<Object, T>> GetCollection<T>()
            where T : OfflineEntity
        {
            ThrowIfDisposed();
            await LoadAsync();

            return (OfflineCollection<Object, T>)cacheData.Collections[typeof(T)];
        }


        /// <summary>
        /// Saves any outstanding changes made by the application.  This method will throw if a sync is currently 
        /// active.
        /// </summary>
        /// <exception cref="SyncActiveException">
        /// Thrown if sync is active when the Save is attempted</exception>
        /// <exception cref="SaveFailedException">
        /// Thrown if there is a modified item changed during sync and a save for that item is attempted.</exception>
        public async Task SaveChangesAsync()
        {
            ThrowIfDisposed();

            // If the cache is not loaded, this is a no-op
            if (!loaded)
                return;

            if (syncActive)
                throw new SyncActiveException("SaveChanges is not permitted while sync is active");

            using (saveSyncLock.LockObject())
            {
                // Don't allow SaveChanges to execute if there are unhandled conflicts from a previous
                // save attempt
                if (offlineConflicts != null && offlineConflicts.Count != 0)
                {
                    throw new SaveFailedException(offlineConflicts,
                                                  "Existing store conflicts must be resolved or have items rejected before " +
                                                  "SaveChanges can be called");
                }

                // Determine if there are any items that can't be saved
                ICollection<OfflineEntity> failures = cacheData.GetSaveFailures();

                if (failures.Count != 0)
                {
                    // Generate store conflicts for the items.
                    offlineConflicts = (from f in failures
                                      select new OfflineConflict(this)
                                                 {
                                                     ModifiedEntity = f,
                                                     LiveEntity = f.GetOriginal()
                                                 }).ToList();

                    // Make sure the modified entities point to their store conflicts
                    foreach (OfflineConflict sc in offlineConflicts)
                    {
                        sc.ModifiedEntity.OfflineConflict = sc;
                    }

                    // Throw an exception and let the user know which items are in conflict
                    // They will also be able to be retrieved from the c later.
                    throw new SaveFailedException(offlineConflicts,
                                                  "One or more modified items has had an update received from the service. The conflicts must be resolved before SaveChanges can complete successfully.");
                }

                // Everything is ok, so actually save changes.
                IEnumerable<OfflineEntity> changes = cacheData.CommitChanges();

                if (changes != null && changes.Any())
                    await StorageHandler.SaveChanges(changes);
            }
        }

        /// <summary>
        /// Returns any conflicts that ocurred when trying to call SaveChanges.  This collection must
        /// be empty before SaveChanges can be called again.  
        /// 
        /// Applications have 3 ways to retrieve store conflicts:
        ///  1. In the exception that is thrown when they are first detected.
        ///  2. From this collection.
        ///  3. From the entities that have conflicts.
        /// </summary>
        public ReadOnlyCollection<OfflineConflict> OfflineConflicts
        {
            get
            {
                ThrowIfDisposed();
                return new ReadOnlyCollection<OfflineConflict>(offlineConflicts);
            }
        }

        /// <summary>
        /// Cancels and rolls back any unsaved changes.  This will block while any other operation is
        /// happening on the c.
        /// </summary>
        public void CancelChanges()
        {
            ThrowIfDisposed();

            if (!loaded) return;

            // Lock so nothing changes while adding the entity
            if (syncActive)
                throw new SyncActiveException("Cancel changes is not permitted while sync is active");

            using (saveSyncLock.LockObject())
                cacheData.Rollback();
        }


        /// <summary>
        /// Method that synchronize the Cache by uploading all modified changes and then downloading the
        /// server changes.
        /// </summary>
        public async Task<CacheRefreshStatistics> SynchronizeAsync()
        {
            if (!loaded)
                await LoadAsync();

            return await this.cacheController.SynchronizeAsync();
        }

        /// <summary>
        /// Method that synchronize the Cache by uploading all modified changes and then downloading the
        /// server changes.
        /// </summary>
        public async Task<CacheRefreshStatistics> SynchronizeAsync(CancellationToken cancellationToken)
        {
            if (!loaded)
                await LoadAsync();

            return await this.cacheController.SynchronizeAsync(cancellationToken);
        }


        /// <summary>
        /// Removes the requested item.  This entity will not be included for synchronization until SaveChanges is
        /// called.  If the c has not been loaded, this method will load the c.
        /// </summary>
        /// <typeparam name="T">Type of the entity to remove</typeparam>
        /// <param name="entity">The entity to remove</param>
        /// <exception cref="System.InvalidOperationException">Thrown if the c has never been synchronized with
        /// the service</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if the entity is null</exception>
        public async Task DeleteItem<T>(T entity) where T : OfflineEntity
        {
            ThrowIfDisposed();
            if (entity == null)
                throw new ArgumentNullException("entity");

            // Call load before doing any work.  This is a no-op if the data is already loaded.
            await LoadAsync();


            // Lock so nothing changes while adding the entity
            if (syncActive)
                throw new SyncActiveException("Deleting changes is not permitted while sync is active");

            using (saveSyncLock.LockObject())
            {
                // Make sure the c has been synchronized once
                if (cacheData.AnchorBlob == null)
                {
                    throw new InvalidOperationException(
                        "Anchor is null.  Items cannot be deleted before an initial sync has occurred");
                }

                // Find the corresponding collection and throw
                ((OfflineCollection<Object, T>)cacheData.Collections[typeof(T)]).DeleteItem(entity);
            }
        }

        /// <summary>
        /// Adds the requested item to the collection corresponding to the type passed specified by T.  This entity
        /// will not be included in synchronization until SaveChanges is called.  If the c has not been loaded,
        /// calling this method will load the c.
        /// </summary>
        /// <typeparam name="T">Type of the entity being passed in.  Must be a type specified in the isolatedStorageSchema.</typeparam>
        /// <param name="entity">Entity to add</param>
        public async Task AddItem<T>(T entity) where T : OfflineEntity
        {
            ThrowIfDisposed();
            
            if (entity == null)
                throw new ArgumentNullException("entity");

            // Make sure the c is loaded.
            await LoadAsync();

            // Lock so nothing changes while adding the entity
            if (syncActive)
                throw new SyncActiveException("Adding changes is not permitted while sync is active");

            using (saveSyncLock.LockObject())
            {
                // Make sure sync has happened.
                if (cacheData.AnchorBlob == null)
                {
                    throw new InvalidOperationException(
                        "Anchor is null.  Items cannot be added before an initial sync has occurred");
                }

                // Add the item
                ((OfflineCollection<Object, T>)cacheData.Collections[typeof(T)]).Add(entity);
            }
        }

        /// <summary>
        /// Sync conflicts that occurred during sync.  The application is not required to handle these, other than
        /// to clear them to save space.  Calling this method will cause the c to be loaded from disk if it
        /// is not already.
        /// </summary>
        public ReadOnlyCollection<SyncConflict> SyncConflicts
        {
            get
            {
                ThrowIfDisposed();

                if (!loaded)
                    Task.Run(async () => await LoadAsync()).Wait();

                return new ReadOnlyCollection<SyncConflict>(cacheData.SyncConflicts);
            }
        }

        /// <summary>
        /// Sync errors that occurred during sync.  The application is not required to handle these, other than
        /// to clear them to save space.  Calling this method will cause the c to be loaded from disk if it
        /// is not already.
        /// </summary>
        public ReadOnlyCollection<SyncError> SyncErrors
        {
            get
            {
                ThrowIfDisposed();


                if (!loaded)
                    Task.Run(async () => await LoadAsync()).Wait();

                return new ReadOnlyCollection<SyncError>(cacheData.SyncErrors);
            }
        }

        /// <summary>
        /// Clears sync conflicts from memory and from disk.
        /// </summary>
        public async Task ClearSyncConflicts()
        {
            ThrowIfDisposed();

            await LoadAsync();

            using (saveSyncLock.LockObject())
            {
                cacheData.ClearSyncConflicts();
                await StorageHandler.ClearConflicts();
            }
        }

        /// <summary>
        /// Clears sync errors from memory and from disk.
        /// </summary>
        public async Task ClearSyncErrors()
        {
            ThrowIfDisposed();

            await LoadAsync();

            using (saveSyncLock.LockObject())
            {
                cacheData.ClearSyncErrors();
                await StorageHandler.ClearErrors();
            }
        }

        /// <summary>
        /// Clears all data from the disk and memory.
        /// </summary>
        public async Task ClearCache()
        {
            ThrowIfDisposed();


            // If loaded, clear the in-memory data.
            if (loaded)
                cacheData.Clear();

            // Delete storage internal changes cache and the files.
            await StorageHandler.ClearCache();

            // Make loaded to false to refresh Schema on last Sync
            this.loaded = false;
        }


        #region OfflineSyncProvider

        /// <summary>
        /// OfflineSyncProvider method called when the controller is about to start a sync session.
        /// </summary>
        public override async Task BeginSession()
        {
            ThrowIfDisposed();

            // Don't start a second session if sync is already active.
            if (syncActive)
                throw new InvalidOperationException("Sync session already active for c");

            //Reset IsFirst Sync. This will be set only when the server blob is null;
            isFirstSync = false;

            // Load the cache if it is not already loaded.
            await LoadAsync();

            // Lock everything else out while sync is happening.
            saveSyncLock.Lock();
            syncActive = true;
        }

        /// <summary>
        /// OfflineSyncProvider method implementation to return a set of sync changes.
        /// </summary>
        /// <param name="state">A unique identifier for the changes that are uploaded</param>
        /// <returns>The set of incremental changes to send to the service</returns>
        public override async Task<ChangeSet> GetChangeSet(Guid state)
        {
            ThrowIfDisposed();

            if (!syncActive)
                throw new InvalidOperationException("GetChangeSet cannot be called without calling BeginSession");

            var changeSet = new ChangeSet();

            // Get the changes from the storage layer (not the in-memory data that can change)
            IEnumerable<OfflineEntity> changes = StorageHandler.GetChanges(state);

            // Fill the change list.
            changeSet.Data = (from change in changes select (IOfflineEntity)change).ToList();
            changeSet.IsLastBatch = true;
            changeSet.ServerBlob = cacheData.AnchorBlob;

            return changeSet;
        }

        /// <summary>
        /// OfflineSyncProvider method implementation called when a change set returned from GetChangeSet has been
        /// successfully uploaded.
        /// </summary>
        /// <param name="state">The unique identifier passed in to the GetChangeSet call.</param>
        /// <param name="response">ChangeSetResponse that contains an updated server blob and any conflicts or errors that
        /// happened on the service.</param>
        public override async Task OnChangeSetUploaded(Guid state, ChangeSetResponse response)
        {
            ThrowIfDisposed();

            if (response == null)
                throw new ArgumentNullException("response");

            if (!syncActive)
                throw new InvalidOperationException("OnChangeSetUploaded cannot be called without calling BeginSession");

            if (response.Error == null)
            {
                IEnumerable<OfflineEntity> updatedItems =
                    response.UpdatedItems.Cast<OfflineEntity>();

                // Notify the disk management that changes uploaded successfully.
                IEnumerable<Conflict> conflicts =
                    await StorageHandler.UploadSucceeded(state, response.ServerBlob, response.Conflicts, updatedItems);

                // Update the in-memory representation.
                cacheData.AddUploadChanges(response.ServerBlob, conflicts, updatedItems, this);
            }
            else
            {
                StorageHandler.UploadFailed(state);
            }
        }

        /// <summary>
        /// Returns the last server blob that the c received during sync
        /// </summary>
        /// <returns>The server blob.  This will be null if the c has not synchronized with the service</returns>
        public override byte[] GetServerBlob()
        {
            ThrowIfDisposed();

            if (!syncActive)
                throw new InvalidOperationException("GetServerBlob cannot be called without calling BeginSession");

            byte[] serverBlob = cacheData.AnchorBlob;

            if (serverBlob == null)
                isFirstSync = true;

            return serverBlob;
        }

        /// <summary>
        /// OfflineSyncProvider method called to save changes retrieved from the sync service.
        /// </summary>
        /// <param name="changeSet">The set of changes from the service to save. Also contains an updated server
        /// blob.</param>
        public override async Task SaveChangeSet(ChangeSet changeSet)
        {
            ThrowIfDisposed();

            if (changeSet == null)
                throw new ArgumentNullException("changeSet");

            if (!syncActive)
                throw new InvalidOperationException("SaveChangeSet cannot be called without calling BeginSession");

            if (changeSet.Data.Count == 0 && !isFirstSync)
                return;

            //// Because i cant be async and must be void (override base class method)
            //// Use of Task.Run(async () => ....
            ////
            //Task task = Task.Run(async () => { await SaveChangeSetInternal(changeSet); });

            //// Wait for task to finish
            //task.Wait();

            await SaveChangeSetInternal(changeSet);
        }

        private async Task SaveChangeSetInternal(ChangeSet changeSet)
        {
            // Cast to the isolated storage-specific entity.
            IEnumerable<OfflineEntity> entities = changeSet.Data.Cast<OfflineEntity>();

            // Store the downloaded changes to disk.
            if (entities == null) return;

            await StorageHandler.SaveDownloadedChanges(changeSet.ServerBlob, entities);

            // Update in-memory representation.
            cacheData.DownloadedChanges(changeSet.ServerBlob, entities);
        }

        /// <summary>
        /// OfflineSyncProvider method called when sync is completed.  This method will unlock so that SaveChanges
        /// and other operations can be called.
        /// </summary>
        public override void EndSession()
        {
            ThrowIfDisposed();

            // If sync is not active, throw.  The c doesn't need to worry about exiting the lock if this throws
            // because it can only be set to false outside of the lock.
            if (!syncActive)
                throw new InvalidOperationException("Sync session not active for c");

            //If this is first sync then call notfication as a reset instead of Add for every item.
            if (isFirstSync)
                cacheData.NotifyAllCollections();

            // Sync is no longer active
            syncActive = false;

            // Unlock.
            saveSyncLock.Unlock();
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Closes the c and releases the lock on the cache path
        /// </summary>
        public void Close()
        {
            Dispose();
        }

        /// <summary>
        /// Dispose the c and releases the lock on the cache path
        /// </summary>
        public void Dispose()
        {
            // This is the standard dispose pattern
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose internal references
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    if (StorageHandler != null)
                    {
                        StorageHandler.Dispose();
                        StorageHandler = null;
                    }

                    if (saveSyncLock != null)
                    {
                        saveSyncLock.Dispose();
                        saveSyncLock = null;
                    }
                }

                isDisposed = true;
            }
        }


        /// <summary>
        /// Method which checks whether or not the object is disposed and throws if it is
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("Cannot access a disposed IsolatedStorageOfflineContext");
            }
        }

        #endregion

        /// <summary>
        /// Creates the cache controller to sync with the c.
        /// </summary>
        private void CreateCacheController()
        {
            cacheController = new CacheController(scopeUri, scopeName, this);

            CacheControllerBehavior behavior = cacheController.ControllerBehavior;

            // Because the AddType is AddType<T>, need to create a generic method
            // for each type in the isolatedStorageSchema.  This will only be done once, so it's
            // not too expensive.
            Type behaviorType = behavior.GetType();
            MethodInfo addType = behaviorType.GetTypeInfo().GetDeclaredMethod("AddType");

            // Loop over each collection in the isolatedStorageSchema.
            foreach (Type t in schema.Collections)
            {
                // Create the generic method for the type.
                MethodInfo addTypeT = addType.MakeGenericMethod(t);

                // Invoke the created method.
                addTypeT.Invoke(behavior, new object[] { });
            }
        }


        /// <summary>
        /// Method that verifies a previously cached isolatedStorageSchema and uri (if they exist) with the current isolatedStorageSchema and uri.
        /// </summary>
        /// <param name="path">Cache path for the c</param>
        /// <param name="isolatedStorageSchema">Schema to verify</param>
        /// <param name="uri">Uri to verify</param>
        /// <param name="scope">The scope name that the client will be accessing on the service</param>
        private async Task CheckSchemaAndUri(string path, OfflineSchema isolatedStorageSchema, Uri uri,
                                             string scope)
        {
            // Get the isolated storage file for the application.
            StorageFolder isoFolder = ApplicationData.Current.LocalFolder;

            bool cachePathExist = await isoFolder.FolderExistsAsync(path);

            if (!cachePathExist)
                await isoFolder.CreateFolderAsync(path);

            StorageFolder cacheFolder = await isoFolder.GetFolderAsync(path);

            // Generate the path to the scope info file.
            string infoPath = Constants.SCOPE_INFO;

            bool fileExist = await cacheFolder.FileExistsAsync(infoPath);

            // If the file exists, read it, otherwise, everything is fine.
            if (fileExist)
            {
                // Open the scope file.
                using (Stream stream = await cacheFolder.OpenStreamForReadAsync(infoPath))
                {
                    Stream readStream = stream;

                    try
                    {
                        List<string> fileTypes;
                        string fileUri;
                        string fileScopeName;

                        // Read the file types and uri from the file.
                        ReadSchemaAndUri(readStream, out fileUri, out fileScopeName, out fileTypes);

                        // Verify the scope uri.
                        if (fileUri != uri.AbsoluteUri)
                            throw new ArgumentException(
                                "Specified uri does not match uri previously used for the specified cache path");

                        if (fileScopeName != scope)
                            throw new ArgumentException(
                                "Specified scope name does not match scope name previously used for the specified cache path");

                        // Verify the types.
                        List<Type> userTypes = isolatedStorageSchema.Collections.ToList();

                        // Sort by name (the class Type isn't sortable)
                        userTypes.Sort((x, y) => String.Compare(x.FullName, y.FullName, StringComparison.Ordinal));

                        if (userTypes.Count != fileTypes.Count)
                            throw new ArgumentException(
                                "Specified isolatedStorageSchema does not match isolatedStorageSchema previously used for cache path");

                        if (userTypes.Where((t, i) => t.FullName != fileTypes[i]).Any())
                            throw new ArgumentException(
                                "Specified isolatedStorageSchema does not match isolatedStorageSchema previously used for cache path");
                    }
                    finally
                    {
                        readStream.Dispose();
                    }
                }
            }
            else
            {
                // If the file doesn't exist, write the new info.
                using (
                    Stream stream =
                        await cacheFolder.OpenStreamForWriteAsync(infoPath, CreationCollisionOption.ReplaceExisting))
                {
                    Stream writeStream = stream;

                    try
                    {
                        WriteSchemaFile(writeStream, uri, scope, isolatedStorageSchema);
                    }
                    finally
                    {
                        writeStream.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Reads the cached isolatedStorageSchema and uri information from the stream. Everything is stored in text, so just use a stream reader.
        /// </summary>
        /// <param name="stream">Stream to read from</param>
        /// <param name="uri">Uri for the scope</param>
        /// <param name="scope">Scope Name</param>
        /// <param name="types">List of types returned from the file</param>
        private void ReadSchemaAndUri(Stream stream,
                                      out string uri,
                                      out string scope,
                                      out List<string> types)
        {
            string schemaUri;
            string schemaScopeName;
            var schemaTypes = new List<string>();

            // Create the stream reader.
            using (var reader = new StreamReader(stream))
            {
                // First line of the file is the uri
                schemaUri = reader.ReadLine();
                schemaScopeName = reader.ReadLine();

                // Rest of the file are the types (Full type names including namespaces).
                while (!reader.EndOfStream)
                {
                    schemaTypes.Add(reader.ReadLine());
                }
            }

            // Fill the output parameters.
            types = schemaTypes;
            uri = schemaUri;
            scope = schemaScopeName;
        }

        /// <summary>
        /// Writes the file with isolatedStorageSchema information.
        /// </summary>
        /// <param name="stream">Stream to which to write</param>
        /// <param name="uri">Scope uri to be written</param>
        /// <param name="scope">Scope Name</param>
        /// <param name="storageSchema">Schema to be written</param>
        private void WriteSchemaFile(Stream stream,
                                     Uri uri,
                                     string scope,
                                     OfflineSchema storageSchema)
        {
            // Write data as text, so create the stream reader.
            using (var writer = new StreamWriter(stream))
            {
                // Write the text version of the Uri.
                writer.WriteLine(uri.AbsoluteUri);
                writer.WriteLine(scope);

                // Get the list of types as strings and sort to make comparison
                // faster when reading.
                List<string> types = (from type in storageSchema.Collections
                                      select type.FullName).ToList();
                types.Sort();

                // Write the types.
                foreach (string type in types)
                {
                    writer.WriteLine(type);
                }
            }
        }

        /// <summary>
        /// Internal method called by the StoreConflict class in order to resolve a store conflict.  This must be done
        /// because there must be some maintenance of the in-memory collections depending on the resolution of the conflict
        /// </summary>
        /// <param name="conflict">Conflict to resolve</param>
        /// <param name="resolutionAction">Resolution action.</param>
        internal void ResolveOfflineConflict(OfflineConflict conflict, SyncConflictResolutionAction resolutionAction)
        {
            using (saveSyncLock.LockObject())
            {
                // Cache the modified entity, which may disappear depending on the resolution
                OfflineEntity visibleEntity = conflict.ModifiedEntity;

                // Respond to the resolution
                switch (resolutionAction)
                {
                    case SyncConflictResolutionAction.AcceptModifiedEntity:
                        conflict.ModifiedEntity.UpdateModifiedTickCount();
                        break;
                    case SyncConflictResolutionAction.AcceptStoreEntity:
                        cacheData.ResolveStoreConflictByRollback(conflict.ModifiedEntity);
                        break;
                    default:
                        throw new ArgumentException("Invalid resolution action specified");
                }

                // Cleanup pointers to conflicts everywhere.
                visibleEntity.OfflineConflict = null;
                offlineConflicts.Remove(conflict);

                // Clearing the c will prevent the resolution from being triggered again.
                conflict.ClearContext();
            }
        }



        /// <summary>
        /// Called by SyncErrorInfo from the ClearSyncConflict method.  Used to remove the conflict
        /// from the collection and from the disk
        /// </summary>
        /// <param name="conflict">conflict to clear</param>
        internal  async Task ClearSyncConflict(SyncConflict conflict)
        {
            using (saveSyncLock.LockObject())
            {
                WinEightSyncConflict winEightSyncConflict = conflict as WinEightSyncConflict;

                if (winEightSyncConflict == null)
                    return;

                cacheData.RemoveSyncConflict(conflict);
                await StorageHandler.ClearSyncConflict(winEightSyncConflict);
            }
        }

        /// <summary>
        /// Called by SyncErrorInfo from the ClearSyncError method.  Used to remove the error
        /// from the collection and from the disk
        /// </summary>
        ///<param name="error">SyncError</param>
        internal async Task ClearSyncError(SyncError error)
        {
            using (saveSyncLock.LockObject())
            {
                WinEightSyncError winEightSyncError = error as WinEightSyncError;

                if (winEightSyncError == null)
                    return;

                cacheData.RemoveSyncError(error);
                await StorageHandler.ClearSyncError(winEightSyncError);
            }
        }



    }
}