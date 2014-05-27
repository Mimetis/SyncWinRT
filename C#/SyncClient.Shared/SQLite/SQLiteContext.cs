using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Synchronization.ClientServices.Common;
#if ( WINDOWS_PHONE || NETFX_CORE)
using Windows.Storage;
#endif


namespace Microsoft.Synchronization.ClientServices.SQLite
{
    /// <summary>
    /// IsolatedStorageOfflineContext
    /// </summary>
    public class SQLiteContext : OfflineSyncProvider, IDisposable
    {
        #region  Members

        /// <summary>
        /// Specifies whether or not the c is loaded.  It is set when the c has been
        /// successfully loaded. It is guared by the _loadLock
        /// </summary>
        private volatile bool loaded;

        /// <summary>
        /// The scope uri for the c.  Passed in to the constructor.
        /// </summary>
        private readonly Uri scopeUri;

        /// <summary>
        /// The scope name for the c. Passed in to the constructor.
        /// </summary>
        private readonly string scopeName;

        /// <summary>
        /// The Offline database name
        /// </summary>
        private readonly string databaseName;

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
        // private bool isFirstSync;

        /// <summary>
        /// SQLiteManager used to manage entities in SQLite DB
        /// </summary>
        internal SQLiteManager Manager { get; private set; }

        /// <summary>
        /// Get the current Configuration (LastSyncDate, Registered Types, Blob, ServiceUri)
        /// </summary>
        public SQLiteConfiguration Configuration { get; set; }

        /// <summary>
        /// Get the latests conflicts occured after the last Synchronization
        /// </summary>
        public ReadOnlyCollection<Conflict> Conflicts
        {
            get;
            private set;
        }

        /// <summary>
        /// Get the SQLite DatabaseName name
        /// </summary>
        public string DatabaseName
        {
            get
            {
                ThrowIfDisposed();
                return databaseName;
            }
        }

        /// <summary>
        /// Get the schema used by the SQLiteContext. Any changes to the schema after instantiation of the SQLiteContext
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
        /// Get the Uri of the scope used for sync.
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
        /// Get the CacheController which can be used to synchronize the SQLiteContext with the uri specified
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



        #endregion

        /// <summary>
        /// Constructor for the SQLiteContext
        /// </summary>
        /// <param name="schema">The OfflineSchema that specifies the set of the collections.</param>
        /// <param name="scopeName">The scope name used to identify the scope on the service.</param>
        /// <param name="datbaseName">Name of the database used to store entities.</param>
        /// <param name="uri">Uri of the scope.  Used to intialize the CacheController.</param>
        /// <remarks>
        /// If the Uri specified is different from the one that is stored in the cache path, the
        /// Load method will throw an InvalidOperationException.
        /// </remarks>
        public SQLiteContext(OfflineSchema schema, string scopeName, string datbaseName, Uri uri)
        {
            if (schema == null)
                throw new ArgumentNullException("OfflineSchema");

            if (string.IsNullOrEmpty(scopeName))
                throw new ArgumentNullException("scopeName");

            if (string.IsNullOrEmpty(datbaseName))
                throw new ArgumentNullException("cachePath");

            if (uri == null)
                throw new ArgumentNullException("uri");

            this.isDisposed = false;
            this.schema = schema;
            this.scopeUri = uri;
            this.scopeName = scopeName;
            this.databaseName = datbaseName;

#if ( WINDOWS_PHONE || NETFX_CORE) 
            var localPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, databaseName);
#elif (__ANDROID__)
            var localPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), databaseName);
#elif (__IOS__)
            // we need to put in /Library/ on iOS5.1 to meet Apple's iCloud terms
            // (they don't want non-user-generated data in Documents)
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal); // Documents folder
            string libraryPath = Path.Combine(documentsPath, "..", "Library");
            var localPath = Path.Combine(libraryPath, databaseName);
#endif

            this.Manager = new SQLiteManager(schema, localPath);
            this.CreateCacheController();
        }

        /// <summary>
        /// Loads the schema and configuration
        /// </summary>
        public async Task LoadSchemaAsync()
        {
           await LoadSchemaAsync(CancellationToken.None, null);
        }

        public async Task LoadSchemaAsync(CancellationToken cancellationToken, IProgress<SyncProgressEvent> progress)
        {
            ThrowIfDisposed();

            if (loaded)
                return;

            await CheckSchemaAndUriAsync(schema, scopeUri, scopeName, cancellationToken, progress);

            loaded = true;
        }

        /// <summary>
        /// Method that synchronize the Cache by uploading all modified changes and then downloading the
        /// server changes.
        /// </summary>
        public async Task<CacheRefreshStatistics> SynchronizeAsync()
        {
            if (!loaded)
                await LoadSchemaAsync();

            return await this.cacheController.SynchronizeAsync();
        }

        /// <summary>
        /// Method that synchronize the Cache by uploading all modified changes and then downloading the
        /// server changes.
        /// </summary>
        public async Task<CacheRefreshStatistics> SynchronizeAsync(CancellationToken cancellationToken, IProgress<SyncProgressEvent> progress)
        {
            if (!loaded)
                await LoadSchemaAsync(cancellationToken, progress);

            return await this.cacheController.SynchronizeAsync(cancellationToken, progress);
        }

        #region OfflineSyncProvider

        /// <summary>
        /// SQLiteContext method called when the controller is about to start a sync session.
        /// </summary>
        public override async Task BeginSession()
        {
            ThrowIfDisposed();

            // Don't start a second session if sync is already active.
            if (syncActive)
                throw new InvalidOperationException("Sync session already active");

            await this.InternalBeginSessionAsync();
        }

        /// <summary>
        /// Internal Begin Session. Just check if Schema exist in SQLite DB
        /// </summary>
        private async Task InternalBeginSessionAsync()
        {
            syncActive = true;

            // Read Schema if not already readed.
            if (!loaded)
                await LoadSchemaAsync();

        }

        /// <summary>
        /// SQLiteContext method implementation to return a set of sync changes.
        /// </summary>
        /// <param name="state">A unique identifier for the changes that are uploaded</param>
        /// <returns>The set of incremental changes to send to the service</returns>
        public override async Task<ChangeSet> GetChangeSet(Guid state)
        {
            return await Task.Run<ChangeSet>(() =>
             {
                 ThrowIfDisposed();

                 if (!syncActive)
                     throw new InvalidOperationException("GetChangeSet cannot be called without calling BeginSession");

                 // Create the ChangeSet
                 var changeSet = new ChangeSet();

                 // Get the last date where Sync Occured
                 var lastSyncDate = Configuration.LastSyncDate;

                 // Get the changes from the storage layer (not the in-memory data that can change)
                 IEnumerable<IOfflineEntity> changes = Manager.GetChanges(state, lastSyncDate);

                 // Fill the change list.
                 changeSet.Data = changes.ToList();
                 changeSet.IsLastBatch = true;
                 changeSet.ServerBlob = this.Configuration.AnchorBlob;

                 return changeSet;
             });

        }

        /// <summary>
        /// SQLiteContext method implementation called when a change set returned from GetChangeSet has been
        /// successfully uploaded.
        /// </summary>
        /// <param name="state">The unique identifier passed in to the GetChangeSet call.</param>
        /// <param name="response">ChangeSetResponse that contains an updated server blob and any conflicts or errors that
        /// happened on the service.</param>
        public override async Task OnChangeSetUploaded(Guid state, ChangeSetResponse response)
        {
           await Task.Run(() =>
            {
                ThrowIfDisposed();

                if (response == null)
                    throw new ArgumentNullException("response");

                if (!syncActive)
                    throw new InvalidOperationException("OnChangeSetUploaded cannot be called without calling BeginSession");

                if (response.Error == null)
                {
                    IEnumerable<SQLiteOfflineEntity> conflictEntities = null;
                    IEnumerable<SQLiteOfflineEntity> updatedItems = null;

                    // Get conflicts and notify user
                    if (response.Conflicts != null)
                    {
                        // This approach assumes that there are not duplicates between the conflicts and the updated entities (there shouldn't be)
                        conflictEntities = (from c in response.Conflicts
                                            select (SQLiteOfflineEntity)c.LiveEntity);

                        this.Conflicts = response.Conflicts;

                    }

                    // Get the Updated Items from Server
                    // A read only collection of Insert entities uploaded by clients that have been issued
                    // permanent Id's by the service
                    if (response.UpdatedItems != null && response.UpdatedItems.Count > 0)
                    {
                        updatedItems = response.UpdatedItems.Cast<SQLiteOfflineEntity>();
                    }

                    IEnumerable<SQLiteOfflineEntity> allItems = updatedItems ?? new List<SQLiteOfflineEntity>();

                    // Add conflict entities
                    if (conflictEntities != null)
                        allItems = allItems.Concat(conflictEntities);

                    this.Manager.SaveDownloadedChanges(allItems);


                    // Notify the disk management that changes uploaded successfully.
                    // Update all Entities and Set IsDirty = 0
                    // Remvove tarcking tombstone
                    Manager.UploadSucceeded(state);


                    // Set the new Anchor
                    this.Configuration.AnchorBlob = response.ServerBlob;
                }
            });
        }

        /// <summary>
        /// Returns the last server blob that the SQLiteContext received during sync
        /// </summary>
        /// <returns>The server blob.  This will be null if the SQLiteContext has not synchronized with the service</returns>
        public override byte[] GetServerBlob()
        {
            ThrowIfDisposed();

            if (!syncActive)
                throw new InvalidOperationException("GetServerBlob cannot be called without calling BeginSession");

            byte[] serverBlob = Configuration.AnchorBlob;

            //if (serverBlob == null)
            //    isFirstSync = true;

            return serverBlob;
        }

        /// <summary>
        /// SQLiteContext method called to save changes retrieved from the sync service.
        /// </summary>
        /// <param name="changeSet">The set of changes from the service to save. Also contains an updated server
        /// blob.</param>
        public override async Task SaveChangeSet(ChangeSet changeSet)
        {
            await Task.Run(() =>
            {
                ThrowIfDisposed();

                if (changeSet == null)
                    throw new ArgumentNullException("changeSet");

                if (!syncActive)
                    throw new InvalidOperationException("SaveChangeSet cannot be called without calling BeginSession");

                // Cast to the specific entity.
                IEnumerable<SQLiteOfflineEntity> entities = changeSet.Data.Cast<SQLiteOfflineEntity>();

                Manager.SaveDownloadedChanges(entities);

                // Set the new Last Sync Date
                this.Configuration.LastSyncDate = DateTime.UtcNow;
                this.Configuration.AnchorBlob = changeSet.ServerBlob;

                Manager.SaveConfiguration(this.Configuration);
            });

        }

        /// <summary>
        /// SQLiteContext method called when sync is completed.  This method will unlock so that other operations can be called.
        /// </summary>
        public override void EndSession()
        {
            ThrowIfDisposed();

            // If sync is not active, throw.  The c doesn't need to worry about exiting the lock if this throws
            // because it can only be set to false outside of the lock.
            if (!syncActive)
                throw new InvalidOperationException("Sync session not active");

            // Sync is no longer active
            syncActive = false;

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
                    Manager = null;
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
        /// Function that users will use to add any custom scope level filter parameters and their values.
        /// </summary>
        /// <param name="key">parameter name as string</param>
        /// <param name="value">parameter value as string</param>
        public void AddScopeParameters(string key, string value)
        {
            this.CacheController.ControllerBehavior.AddScopeParameters(key, value);
        }

        /// <summary>
        /// Creates the cache controller to sync.
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
        /// <param name="offlineSchema">Schema to verify</param>
        /// <param name="uri">Uri to verify</param>
        /// <param name="scope">The scope name that the client will be accessing on the service</param>
        private async Task CheckSchemaAndUriAsync(OfflineSchema offlineSchema, Uri uri, string scope, CancellationToken cancellationToken, IProgress<SyncProgressEvent> progress)
        {
            await Task.Run(() =>
            {

                DateTime durationStartDate = DateTime.Now;

                this.Configuration = this.Manager.ReadConfiguration(scope);

                if (progress != null)
                    progress.Report(new SyncProgressEvent(SyncStage.ReadingConfiguration, DateTime.Now.Subtract(durationStartDate)));

                if (this.Configuration != null)
                {

                    // Verify the scope uri.
                    if (this.Configuration.ServiceUri.AbsoluteUri != uri.AbsoluteUri)
                        throw new ArgumentException(
                            "Specified uri does not match uri previously used for the specified database");

                    // Verify the types.
                    List<Type> userTypes = offlineSchema.Collections.ToList();

                    // Sort by name (the class Type isn't sortable)
                    userTypes.Sort((x, y) => String.Compare(x.FullName, y.FullName, StringComparison.Ordinal));

                    if (userTypes.Count != this.Configuration.Types.Count)
                        throw new ArgumentException(
                            "Specified offlineSchema does not match database Offline schema previously used for cache path");

                    // Fix
                   this.Configuration.Types.Sort((x, y) => String.Compare(x, y, StringComparison.Ordinal));

                    if (userTypes.Where(
                        (t, i) => t.FullName != this.Configuration.Types[i]).Any())
                        throw new ArgumentException(
                            "Specified offlineSchema does not match database Offline schema previously used for cache path");

                }
                else
                {
                    bool existScope = this.Manager.ScopeTableExist();

                    if (!existScope)
                    {
                        durationStartDate = DateTime.Now;

                        this.Manager.CreateScopeTable();

                        if (progress != null)
                            progress.Report(new SyncProgressEvent(SyncStage.CreatingScope, DateTime.Now.Subtract(durationStartDate)));
                    }

                    // Get the list of types as strings and sort to make comparison
                    // faster when reading.
                    List<string> types = (from type in offlineSchema.Collections
                                          select type.FullName).ToList();
                    types.Sort();

                    // Create the initial configuration
                    this.Configuration = new SQLiteConfiguration
                    {
                        AnchorBlob = null,
                        LastSyncDate = new DateTime(1900, 01, 01),
                        ScopeName = scope,
                        ServiceUri = uri,
                        Types = types
                    };

                    durationStartDate = DateTime.Now;

                    this.Manager.SaveConfiguration(this.Configuration);

                    if (progress != null)
                        progress.Report(new SyncProgressEvent(SyncStage.ApplyingConfiguration, DateTime.Now.Subtract(durationStartDate)));
                }

                // Try to save tables if not exists
                if (schema == null || schema.Collections == null || schema.Collections.Count == 0)
                    return;

                durationStartDate = DateTime.Now;
                foreach (var table in schema.Collections.Where(table => table.Name != SQLiteConstants.ScopeInfo))
                {
                    this.Manager.CreateTable(table);
                }

                if (progress != null)
                    progress.Report(new SyncProgressEvent(SyncStage.CheckingTables, DateTime.Now.Subtract(durationStartDate)));


            });
        }
    }
}