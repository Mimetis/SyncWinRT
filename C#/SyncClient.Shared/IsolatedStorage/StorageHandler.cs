
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Threading;
using System.Text;
using Windows.UI.Xaml;
using Windows.Storage;
using System.Threading.Tasks;
using Windows.Storage.Search;
using Microsoft.Synchronization.ClientServices.Common;


namespace Microsoft.Synchronization.ClientServices.IsolatedStorage
{
    /// <summary>
    /// This is an internal class which manages all reads and writes from disk.  When sync attempts to send entitiesChanges
    /// this method will supply the entitiesChanges to send, rather than using the in-memory copies.
    /// </summary>
    internal class StorageHandler : IDisposable
    {

        /// <summary>
        /// The schema for the collection.  Passed in during construction.
        /// </summary>
        private OfflineSchema schema;

        /// <summary>
        /// The cache path used to store files.  Passed in during constrution.
        /// </summary>
        private string cachePath;

        /// <summary>
        /// List of known types generated during construction and passed to serializers
        /// as they are created in order to speed up serialization and ensure that all
        /// types passed in can be serialized.
        /// </summary>
        private List<Type> knownTypes;

        /// <summary>
        /// Monotonically increasing tick count for files. Helps ensure that files
        /// can be sorted in the order that they are written.  Updated each time
        /// a new file is written.
        /// </summary>
        private int fileCount;

        /// <summary>
        /// Set of entitiesChanges that have been saved but not sent the server.  A dictionary is
        /// used to ensure that there are no duplicates.
        /// </summary>
        private Dictionary<OfflineEntityKey, OfflineEntity> changes;

        /// <summary>
        /// The set of entitiesChanges that have been sent to the server but for which there has been
        /// no response received yet.  The guid is the unique identifier passed in to the GetChanges
        /// method and is used in the event that there are multiple batches uploaded before a
        /// success response is received (in case queued upload is ever implemented).
        /// </summary>
        private Dictionary<Guid, IEnumerable<OfflineEntity>> sentChangesAwaitingResponse;

        /// <summary>
        /// File opened when the context is first created.  It is used to guard access to the cache path
        /// and ensure that there is only one instance of a context working on a cache path at any one time.
        /// </summary>
        private Stream lockFile;

        /// <summary>
        /// Whether or not the storage handler is disposed. Set to true in the dispose method.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Timer started when load is complete to enable archiving of files on disk in order to remove
        /// redundant data.
        /// </summary>
        private DispatcherTimer cleanupTimer;

        /// <summary>
        /// Keeps track of the number of files since archive.  It is incremented every time a file is written.
        /// It is also incremented during loading for ever file that occurs from the beginning or after reading
        /// and archive file.
        /// </summary>
        private int filesSinceArchive;

        /// <summary>
        /// The number of files to be written before archive will attempt to run.
        /// </summary>
        private const int ARCHIVE_FILE_THRESHOLD = 1;

        /// <summary>
        /// Used to sync the _filesSinceArchive during Save, Download and Archiving files
        /// </summary>
        private readonly object syncRoot = new object();

        /// <summary>
        /// Used to sync multiple archive threads (timer call backs)
        /// </summary>
        private readonly object archiveLock = new object();
        /// <summary>
        /// Constructor which initializes the handler given the schema and the cache path
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="cachePath"></param>
        public StorageHandler(OfflineSchema schema, string cachePath)
        {
            Initialize(schema, cachePath);
        }

        private void Initialize(OfflineSchema isolatedStorageSchema, string path)
        {
            isDisposed = false;

            this.schema = isolatedStorageSchema;
            this.cachePath = path;
            this.changes = new Dictionary<OfflineEntityKey, OfflineEntity>();

            knownTypes = new List<Type>
            {
                typeof (SyncConflict),
                typeof (SyncError)
            };

            AddKnownTypes();

        }

        /// <summary>
        /// Loads the files from disk and returns all of the data
        /// </summary>
        /// <returns>Data read from disk.</returns>
        public async Task<CacheData> Load(WinEightContext context)
        {
            ThrowIfDisposed();

            StorageFolder isoFolder = ApplicationData.Current.LocalFolder;

            // Vérification que le folder exist
            var exist = await isoFolder.FolderExistsAsync(cachePath);

            // Création
            if (!exist)
                await isoFolder.CreateFolderAsync(cachePath);

            // Création du cache data
            CacheData cacheData = new CacheData(schema);

            // Lecture des fichiers
            await ReadFiles(cacheData, context);

            // Récupération de l'ancre

            // Start the cleanup timer
            cleanupTimer = new DispatcherTimer { Interval = new TimeSpan(0, Constants.TIMER_MINUTES_INTERVAL, 0) };
            cleanupTimer.Tick += async (sender, o) => await CleanupTimerCallback(sender, o);
            cleanupTimer.Start();

            return cacheData;
        }



        /// <summary>
        /// Receives the entitiesChanges passed in and stores a save file for them.  It will also keep the entitiesChanges
        /// in memory for fast access during sync.
        /// </summary>
        /// <param name="entitiesChanges"></param>
        public async Task SaveChanges(IEnumerable<OfflineEntity> entitiesChanges)
        {
            ThrowIfDisposed();

            // Add entitiesChanges to list
            AddChanges(entitiesChanges, false);

            string fileName = GetFileName(CacheFileType.SaveChanges);

            var isoFolder = ApplicationData.Current.LocalFolder;

            using (Stream fileStream = await OpenWriteFile(isoFolder, fileName))
            using (Stream writeStream = OpenWriteCryptoStream(fileStream))
            {
                var serializer = GetSerializer(typeof(IEnumerable<OfflineEntity>));
                serializer.WriteObject(writeStream, entitiesChanges);
            }

            filesSinceArchive++;

        }

        /// <summary>
        /// Returns the set of entitiesChanges cached in memory and sets them aside with the specified state.
        /// </summary>
        /// <param name="state">Unique identifier for the set of entitiesChanges</param>
        /// <returns>The set of entitiesChanges.</returns>
        public IEnumerable<OfflineEntity> GetChanges(Guid state)
        {
            lock (syncRoot)
            {
                ThrowIfDisposed();

                IEnumerable<OfflineEntity> getChanges = this.changes.Values;

                if (sentChangesAwaitingResponse == null)
                    sentChangesAwaitingResponse = new Dictionary<Guid, IEnumerable<OfflineEntity>>();

                sentChangesAwaitingResponse[state] = getChanges;

                this.changes = new Dictionary<OfflineEntityKey, OfflineEntity>();
                return getChanges;
            }
        }

        /// <summary>
        /// If the upload fails, this method is called to return the entitiesChanges corresponding to the state to the overall
        /// list of entitiesChanges.
        /// </summary>
        /// <param name="state">State for which the send failed.</param>
        public void UploadFailed(Guid state)
        {
            lock (syncRoot)
            {
                ThrowIfDisposed();

                IEnumerable<OfflineEntity> getChanges = sentChangesAwaitingResponse[state];
                sentChangesAwaitingResponse.Remove(state);

                AddChanges(getChanges, true);
            }
        }

        /// <summary>
        /// Called when the entitiesChanges returned above were successfully uploaded.  This method will write an updated file
        /// for the entitiesChanges and then drop the in-memory copies.
        /// </summary>
        /// <param name="state">State for which the upload succeeded</param>
        /// <param name="bAnchor">New anchor to store</param>
        /// <param name="conflicts">Conflicts which were resolved on upload.</param>
        /// <param name="entities">Entities for which an OData property was updated</param>
        public async Task<IEnumerable<Conflict>> UploadSucceeded(Guid state, byte[] bAnchor, 
                            IEnumerable<Conflict> conflicts, IEnumerable<OfflineEntity> entities)
        {
            List<Conflict> returnConflicts = new List<Conflict>();


            // Don't need the sent entitiesChanges anymore
            sentChangesAwaitingResponse.Remove(state);

            string fileName = GetFileName(CacheFileType.UploadResponse);

            var isoFolder = ApplicationData.Current.LocalFolder;


            ResponseData responseData = new ResponseData();
            responseData.Anchor = bAnchor;

            // This approach assumes that there are not duplicates between the conflicts and the updated entities (there shouldn't be)
            responseData.Entities = (from c in conflicts
                                     select (OfflineEntity)c.LiveEntity).Concat(entities);

            using (Stream fileStream = await OpenWriteFile(isoFolder, fileName))
            using (Stream writeStream = OpenWriteCryptoStream(fileStream))
            {
                var serializer = GetSerializer(typeof(ResponseData));

                serializer.WriteObject(writeStream, responseData);
            }

            foreach (Conflict conflict in conflicts)
                returnConflicts.Add(await WriteConflictFile(isoFolder, conflict));

            // Increment creation of a new recent file since the last Archive
            filesSinceArchive++;

            return returnConflicts;
        }


        /// <summary>
        /// Saves the entitiesChanges received when downloading to disk.
        /// </summary>
        /// <param name="bAnchor">New anchor for the store</param>
        /// <param name="entities">The received entities.</param>
        public async Task SaveDownloadedChanges(byte[] bAnchor, IEnumerable<OfflineEntity> entities)
        {
            ThrowIfDisposed();

            ResponseData downloadData = new ResponseData();
            downloadData.Anchor = bAnchor;
            downloadData.Entities = entities;
            string fileName = GetFileName(CacheFileType.DownloadResponse);

            var isoFolder = ApplicationData.Current.LocalFolder;
            using (Stream fileStream = await OpenWriteFile(isoFolder, fileName))
            using (Stream writeStream = OpenWriteCryptoStream(fileStream))
            {
                var serializer = GetSerializer(typeof(ResponseData));

                serializer.WriteObject(writeStream, downloadData);
            }

            // Increment creation of a new recent file since the last Archive
            filesSinceArchive++;
        }

        public async Task ClearSyncConflict(WinEightSyncConflict conflict)
        {
            ThrowIfDisposed();

            var isoFolder = ApplicationData.Current.LocalFolder;

            if (await isoFolder.FileExistsAsync(conflict.FileName))
            {
                var fileToDelete = await isoFolder.GetFileAsync(conflict.FileName);

                await DeleteFile(fileToDelete);
            }

        }

        public async Task ClearSyncError(WinEightSyncError error)
        {
            ThrowIfDisposed();

            var isoFolder = ApplicationData.Current.LocalFolder;
            if (await isoFolder.FileExistsAsync(error.FileName))
            {

                var fileToDelete = await isoFolder.GetFileAsync(error.FileName);

                await DeleteFile(fileToDelete);
            }

        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (archiveLock)
            {
                lock (syncRoot)
                {
                    if (isDisposed) return;

                    if (disposing)
                    {
                        lockFile.Dispose();
                        lockFile = null;

                        if (cleanupTimer != null)
                        {
                            if (cleanupTimer.IsEnabled)
                                cleanupTimer.Stop();
                            cleanupTimer = null;
                        }
                    }

                    isDisposed = true;
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException("Cannot access disposed StorageHandler");
        }

        #endregion

        /// <summary>
        /// Adds the types from the collection to the list of known types for serialization
        /// </summary>
        private void AddKnownTypes()
        {
            foreach (Type t in schema.Collections)
                knownTypes.Add(t);
        }

        /// <summary>
        /// Reads through the files on the cache path and files the cache data.
        /// </summary>
        /// <param name="cacheData">Object which will be filled with data from disk.</param>
        /// <param name="context">IsolatedStorageOfflineContext</param>
        private async Task ReadFiles(CacheData cacheData, WinEightContext context)
        {
            StorageFolder isoFolder = ApplicationData.Current.LocalFolder;

            var cacheFolder = await isoFolder.GetFolderAsync(cachePath);

            var files = await cacheFolder.GetFilesAsync();

            files.OrderBy(sf => sf.Name);

            var arrayFiles = files.ToArray();

            bool exceptionCaught = false;

            List<FileInfo> conflictFiles = new List<FileInfo>();

            foreach (StorageFile file in arrayFiles)
            {
                try
                {
                    if (Constants.SpecialFile(file.Name) || !Constants.IsCacheFile(file.Name)) continue;

                    if (exceptionCaught)
                    {
                        await DeleteFile(file);
                    }
                    else
                    {
                        CacheFileType fileType = GetFileType(file.Name);
                        var fc = GetFileCount(file.Name);

                        fileCount = fc >= fileCount ? fc : fileCount;

                        switch (fileType)
                        {
                            case CacheFileType.DownloadResponse:
                                await ReadDownloadResponseFile(file.Name, cachePath, cacheData);
                                filesSinceArchive++;
                                break;

                            case CacheFileType.SaveChanges:
                                await ReadSaveChangesFile(file.Name, cachePath, cacheData);
                                filesSinceArchive++;
                                break;

                            case CacheFileType.UploadResponse:
                                await ReadUploadResponseFile(file.Name, cachePath, cacheData);
                                filesSinceArchive++;
                                break;

                            case CacheFileType.Conflicts:
                            case CacheFileType.Errors:

                                conflictFiles.Add(new FileInfo
                                {
                                    FileName = file.Name,
                                    FileType = fileType
                                });

                                break;

                            case CacheFileType.Archive:
                                await ReadArchiveFile(file.Name, cacheData);
                                filesSinceArchive = 0;
                                break;
                        }
                    }
                }
                catch (SerializationException)
                {
                    // if there's a serialization exception set a flag to remove the subsequent files
                    exceptionCaught = true;

                    DeleteFile(file);

                }
                catch (Exception)
                {
                    // this can happen for a variety of reasons.  The
                    exceptionCaught = true;
                }
            }

            foreach (FileInfo fi in conflictFiles)
            {
                try
                {
                    int count = GetFileCount(fi.FileName);

                    if (exceptionCaught && count > this.fileCount)
                    {
                        var deleteFile = await isoFolder.GetFileAsync(fi.FileName);

                        await DeleteFile(deleteFile);
                    }
                    else
                    {
                        if (fi.FileType == CacheFileType.Conflicts)
                            await ReadConflictFile(fi.FileName, cachePath, cacheData, context);
                        else if (fi.FileType == CacheFileType.Errors)
                            await ReadErrorFile(fi.FileName, cachePath, cacheData, context);
                    }
                }
                catch (SerializationException)
                {
                    // Drop this exception...if reading a conflict fails, it's not the worst thing.
                }
                catch (Exception)
                {
                    // Drop this exception...this will likely happen if a file can't be deleted.
                }
            }

        }

        /// <summary>
        /// Returns the type of the file based on the file name.
        /// </summary>
        /// <param name="fileName">File name.</param>
        /// <returns>Type of data stored in the file</returns>
        private CacheFileType GetFileType(string fileName)
        {
            int index = fileName.LastIndexOf('.');

            CacheFileType fileType = CacheFileType.Unknown;

            if (index == fileName.Length - 2)
            {
                switch (fileName[index + 1])
                {
                    case 'D':
                        fileType = CacheFileType.DownloadResponse;
                        break;

                    case 'S':
                        fileType = CacheFileType.SaveChanges;
                        break;

                    case 'U':
                        fileType = CacheFileType.UploadResponse;
                        break;

                    case 'A':
                        fileType = CacheFileType.Archive;
                        break;

                    case 'C':
                        fileType = CacheFileType.Conflicts;
                        break;

                    case 'E':
                        fileType = CacheFileType.Errors;
                        break;

                    default:
                        fileType = CacheFileType.Unknown;
                        break;
                }
            }

            return fileType;
        }

        /// <summary>
        /// Returns the count of the file.  This is used for storing files in order.
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns>File count</returns>
        /// <remarks>
        /// Throughout the cache, we assume that file names are of the form>
        /// (File Count In Hex)[.Intermediate].(File Type), where the part within [] is
        /// optional.
        /// </remarks>
        private int GetFileCount(string fileName)
        {
            int index = fileName.IndexOf('.');
            string tickCount = fileName.Substring(0, index);

            return Int32.Parse(tickCount, System.Globalization.NumberStyles.Number);
        }

        /// <summary>
        /// Creates a new file name based on the current file count and the specified type.
        /// </summary>
        /// <param name="fileType">type for which to generate a file name</param>
        /// <param name="fileName">File name to inject to the file</param>
        /// <returns>File name.</returns>
        private string GetFileName(CacheFileType fileType, string fileName = null)
        {
            return GetFileName(fileType, fileName, GetNextFileCount());
        }

        private string GetFileName(CacheFileType fileType, string fileName, int tickCount)
        {
            string suffix;
            switch (fileType)
            {
                case CacheFileType.DownloadResponse:
                    suffix = "D";
                    break;

                case CacheFileType.SaveChanges:
                    suffix = "S";
                    break;

                case CacheFileType.UploadResponse:
                    suffix = "U";
                    break;

                case CacheFileType.Conflicts:
                    suffix = "C";
                    break;

                case CacheFileType.Errors:
                    suffix = "E";
                    break;

                case CacheFileType.Archive:
                    suffix = "A";
                    break;

                default:
                    // users should never see this exception
                    throw new InvalidOperationException("Unexpected value for file type");
            }

            string format = "{0:D8}.{1}";

            if (fileName != null)
                format = "{0:D8}.{2}.{1}";

            return Path.Combine(cachePath, string.Format(format, tickCount, suffix, fileName));
        }

        /// <summary>
        /// Instantiates the serializer.  This method is used so that serializers can be changed easily.
        /// </summary>
        /// <param name="type">Base type for the serializer</param>
        /// <returns>The serializer</returns>
        private DataContractJsonSerializer GetSerializer(Type type)
        {
            return new DataContractJsonSerializer(type, knownTypes);
        }

        private async Task ReadDownloadResponseFile(string fileName, string folderName, CacheData cacheData)
        {
            ResponseData downloadResponse = await ReadFile<ResponseData>(fileName, folderName);

            ;

            cacheData.AddSerializedDownloadResponse(downloadResponse.Anchor, downloadResponse.Entities.Cast<OfflineEntity>());
        }

        private async Task ReadSaveChangesFile(string fileName, string folderName, CacheData cacheData)
        {
            OfflineEntity[] entities = await ReadFile<OfflineEntity[]>(fileName, folderName);
            if (entities != null)
            {
                cacheData.AddSerializedLocalChanges(entities);
                AddChanges(entities, false);
            }
        }

        private async Task ReadUploadResponseFile(string fileName, String folderName, CacheData cacheData)
        {
            ResponseData uploadResponse = await ReadFile<ResponseData>(fileName, folderName);

            cacheData.AddSerializedUploadResponse(uploadResponse.Anchor, uploadResponse.Entities.Cast<OfflineEntity>());
            changes.Clear();
        }

        private async Task ReadConflictFile(string fileName, string folderName, CacheData cacheData, WinEightContext context)
        {
            WinEightSyncConflict conflict = await ReadFile<WinEightSyncConflict>(fileName, folderName);
            WinEightSyncConflict syncConflict = new WinEightSyncConflict(conflict)
            {
                FileName = fileName
            };

            cacheData.AddSerializedConflict(syncConflict, context);
        }

        private async Task ReadErrorFile(string fileName, string folderName, CacheData cacheData, WinEightContext context)
        {
            WinEightSyncError error = await ReadFile<WinEightSyncError>(fileName, folderName);

            WinEightSyncError syncError = new WinEightSyncError(error)
            {
                FileName = fileName
            };

            cacheData.AddSerializedError(syncError, context);
        }

        private async Task<T> ReadFile<T>(string fileName, string directory)
        {
            var folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(directory);

            return await ReadFile<T>(fileName, folder);
        }

        private async Task<T> ReadFile<T>(string fileName, StorageFolder isoFolder)
        {
            T t;
            using (var readStream = await isoFolder.OpenStreamForReadAsync(fileName))
            {
                t = ReadObject<T>(readStream);
            }

            return t;
        }

        private T ReadObject<T>(Stream stream)
        {
            var serializer = GetSerializer(typeof(T));
            return (T)serializer.ReadObject(stream);
        }

        private async Task<Conflict> WriteConflictFile(StorageFolder isoFolder, Conflict conflict)
        {
            Conflict returnConflict;

            OfflineEntityKey key = (OfflineEntityKey)((OfflineEntity)conflict.LiveEntity).GetIdentity();
            // Use the type name so it's included in the hash code.
            key.TypeName = conflict.LiveEntity.GetType().FullName;


            string fileName;
            if (conflict is WinEightSyncConflict)
            {
                fileName = GetFileName(CacheFileType.Conflicts, string.Format("{0}", key.GetHashCode()));

                returnConflict = new WinEightSyncConflict((SyncConflict)conflict)
                {
                    FileName = fileName
                };
            }
            else if (conflict is WinEightSyncError)
            {
                fileName = GetFileName(CacheFileType.Errors, string.Format("{0}", key.GetHashCode()));
                returnConflict = new WinEightSyncError((SyncError)conflict)
                {
                    FileName = fileName
                };

            }
            else
            {
                // This should never happen, but we need to keep the compiler happy.
                throw new ArgumentException("Unknown conflict type: " + conflict.GetType().FullName);
            }

            using (Stream fileStream = await OpenWriteFile(isoFolder, fileName))
            using (Stream writeStream = OpenWriteCryptoStream(fileStream))
            {
                WriteObject(writeStream, conflict);
            }

            return returnConflict;
        }

        private void WriteObject(Stream stream, object t)
        {
            var serializer = GetSerializer(t.GetType());

            serializer.WriteObject(stream, t);
        }

        /// <summary>
        /// Deletes all conflict files
        /// </summary>
        public async Task ClearConflicts()
        {
            await DeleteFiles(".C");
        }

        /// <summary>
        /// Deletes all error files
        /// </summary>
        public async Task ClearErrors()
        {
            await DeleteFiles(".E");
        }

        /// <summary>
        /// Deletes all files
        /// </summary>
        public async Task ClearCacheFiles()
        {
            await DeleteFiles("*");
        }

        /// <summary>
        /// Clears internal entitiesChanges cache and also deletes files
        /// </summary>
        public async Task ClearCache()
        {
            changes.Clear();
            await ClearCacheFiles();
        }


        /// <summary>
        /// Deletes the files that match the specified search pattern.
        /// </summary>
        /// <param name="searchPattern">Search pattern for which to delete files.</param>
        private async Task DeleteFiles(string searchPattern)
        {
            var isoFolder = ApplicationData.Current.LocalFolder;

            var cacheFolder = await isoFolder.GetFolderAsync(cachePath);
            IReadOnlyList<StorageFile> files= null;

            if (searchPattern == "*")
            {
                files = await cacheFolder.GetFilesAsync();
            }
            else
            {
                var options = new QueryOptions(CommonFileQuery.DefaultQuery, new[] { searchPattern });

                if (isoFolder.AreQueryOptionsSupported(options))
                {
                    var query = cacheFolder.CreateFileQueryWithOptions(options);
                    files = await query.GetFilesAsync();
                }
            }

            if (files != null)
                foreach (var file in files)
                    if (file.Name != Constants.LOCKFILE)
                        await DeleteFile(file);
        }


        /// <summary>
        /// Deletes the specified file
        /// </summary>
        private static async Task DeleteFile(string fileName, StorageFolder folder)
        {
            if (await folder.FileExistsAsync(fileName))
            {
                var isoFile = await folder.GetFileAsync(fileName);
                await DeleteFile(isoFile);
            }

        }

        /// <summary>
        /// Deletes the specified file
        /// </summary>
        private static async Task DeleteFile(StorageFile isoFile)
        {
            int failCount = 0;
            bool retry;


            do
            {
                retry = false;
                try
                {
                    await isoFile.DeleteAsync(StorageDeleteOption.PermanentDelete);

                }
                catch (Exception)
                {
                    failCount++;

                    if (failCount <= 1)
                        retry = true;
                }
            } while (retry);
        }

        //private async Task OpenLockFile(string cachePath)
        //{
        //    var folderExist = await ApplicationData.Current.LocalFolder.FolderExistsAsync(cachePath);

        //    if (!folderExist)
        //        await ApplicationData.Current.LocalFolder.CreateFolderAsync(cachePath);

        //    var folder = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFolderAsync(cachePath);

        //    try
        //    {
        //        lockFile = await folder.OpenStreamForWriteAsync(Constants.LOCKFILE, CreationCollisionOption.ReplaceExisting);
        //    }
        //    catch (Exception)
        //    {
        //        throw new InvalidOperationException("Another context is open with the same cache path");
        //    }

        //}

        private int GetNextFileCount()
        {
            return Interlocked.Increment(ref fileCount);
        }

        /// <summary>
        /// Add the offline entity to the entitiesChanges dictionary
        /// </summary>
        /// <param name="chgs"></param>
        /// <param name="ignoreDuplicates">duplicates are ignored only when the uploaded item has failed, because the item in the _changes 
        /// collection might be newer.</param>
        private void AddChanges(IEnumerable<OfflineEntity> chgs, bool ignoreDuplicates)
        {
            foreach (OfflineEntity entity in chgs)
            {
                OfflineEntityKey key = (OfflineEntityKey)entity.GetIdentity();
                key.TypeName = entity.GetType().FullName;

                if (!this.changes.ContainsKey(key))
                {
                    this.changes.Add(key, entity);
                }
                else if (!ignoreDuplicates)
                {
                    this.changes[key] = entity;
                }
            }
        }


        #region Cache Coalesce code

        private async Task CleanupTimerCallback(Object sender, object o)
        {
            int tickCount;

            int originalFilesSyncArchive;

            // The point of lock here is to let any other write operation clear out
            // once that is done, we'll have the tick count and will only be dealing
            // with previously written files, so we don't need the lock anymore, and
            // we want to allow other operations to continue.
            // Releasing the lock as soon as we get the filesSinceArchive is Ok, 
            // since the other operations are not dependant on the Archive file to get written.
            // Anyways if another archive thread kicks in it will be blocked by the _archiveLock.
            lock (syncRoot)
            {
                // Make sure enough files were written so we don't just keep copying
                // archive files
                if (filesSinceArchive < ARCHIVE_FILE_THRESHOLD)
                    return;

                originalFilesSyncArchive = filesSinceArchive;
                filesSinceArchive = 0;
                tickCount = GetNextFileCount();
            }


            var isoFolder = ApplicationData.Current.LocalFolder;
            StorageFolder folder = null;
            bool caughtException = false;

            // The actual files we can do something about
            List<FileInfo> actualFiles = new List<FileInfo>();
            string fileName = null;

            try
            {
                byte[] archiveAnchor = null;
                // Get all the files under the cache path

                folder = await isoFolder.GetFolderAsync(cachePath);

                var fileList = await folder.GetFilesAsync();

                // Tri
                var listOrdered = fileList.OrderByDescending(file => file.Name);

                // reverse so that we can avoid duplicates better
                //fileList.Reverse();

                // Id manager for items that have been saved
                ArchiveIdManager serializedItems = new ArchiveIdManager();

                bool encounteredUpload = false;

                // Preprocess the list files to pick the ones we want.
                foreach (StorageFile file in listOrdered)
                {
                    if (Constants.IsCacheFile(file.Name))
                    {
                        int getFileCount = GetFileCount(file.Name);
                        if (getFileCount > tickCount)
                            continue;

                        CacheFileType fileType = GetFileType(file.Name);

                        if (fileType != CacheFileType.Conflicts && fileType != CacheFileType.Errors)
                        {
                            FileInfo fileInfo = new FileInfo
                            {
                                FileType = fileType,
                                FileName = file.Name,
                                HasUploadFile = false
                            };

                            actualFiles.Add(fileInfo);

                            // If the file is a SaveChanges file, we want to see if we should put
                            // a dirty flag in the archive file or not
                            if (fileType == CacheFileType.SaveChanges && encounteredUpload)
                            {
                                fileInfo.HasUploadFile = true;
                            }
                            // if there's an upload file, make sure we note it so that we can mark future
                            // save entitiesChanges files correctly.
                            else if (fileType == CacheFileType.UploadResponse)
                            {
                                encounteredUpload = true;
                            }
                        }
                    }
                }

                fileName = GetFileName(CacheFileType.Archive, null, tickCount);

                // Go through the files we parsed and handle correctly.
                using (Stream fileStream = await OpenWriteFile(isoFolder, fileName))
                using (Stream writeStream = OpenWriteCryptoStream(fileStream))
                {
                    bool encounteredArchive = false;
                    foreach (FileInfo fi in actualFiles)
                    {
                        byte[] currentAnchor = null;
                        switch (fi.FileType)
                        {
                            case CacheFileType.DownloadResponse:
                                ResponseData drd = await ReadFile<ResponseData>(fi.FileName, folder);
                                currentAnchor = drd.Anchor;
                                WriteArchiveEntities(drd.Entities.Cast<OfflineEntity>(), false, writeStream, serializedItems);
                                break;

                            case CacheFileType.UploadResponse:
                                ResponseData responseData = await ReadFile<ResponseData>(fi.FileName, folder);
                                currentAnchor = responseData.Anchor;
                                WriteArchiveEntities(responseData.Entities.Cast<OfflineEntity>(), false, writeStream, serializedItems);
                                break;

                            case CacheFileType.SaveChanges:
                                OfflineEntity[] entities = await ReadFile<OfflineEntity[]>(fi.FileName, folder);
                                WriteArchiveEntities(entities.Cast<OfflineEntity>(), !fi.HasUploadFile, writeStream, serializedItems);
                                break;

                            case CacheFileType.Archive:
                                currentAnchor = await TransferArchiveFile(folder, fi.FileName, writeStream, serializedItems);
                                encounteredArchive = true;
                                break;

                        }

                        // Since reading is happening from the end, only need to set the anchor if the
                        // last oe was null
                        if (archiveAnchor == null)
                            archiveAnchor = currentAnchor;

                        // Since reading is happening from the end, once an archive file is read, we
                        // can skip everything else.
                        if (encounteredArchive)
                            break;
                    }

                    // At the end write the anchor
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(byte[]));
                    serializer.WriteObject(writeStream, archiveAnchor);

                }
            }
            catch (Exception e)
            {
                if (e is SerializationException)
                {
                    caughtException = true;

                    // delete the archive file
                    if (fileName != null)
                        DeleteFile(fileName, folder);

                    // if something failed, restore the files synce archive count
                    lock (syncRoot)
                    {
                        // Do an add here because it could have been incremented.
                        filesSinceArchive += originalFilesSyncArchive;
                    }
                }
                else
                {
                    throw;
                }
            }

            if (caughtException) return;

            // If all of this completed successfully, delete the files. This is outside of the try-catch above
            // because we want to avoid doing a rearchive if the only failure was deleting a file.
            foreach (FileInfo fi in actualFiles)
            {
                await DeleteFile(fi.FileName, folder);
            }

        }

        /// <summary>
        /// Writes the specified entities to the stream.  This approach is different than the others because it serializes one entity at a time.
        /// </summary>
        /// <param name="entities">Entities to serialize</param>
        /// <param name="dirty">whether or not they should be marked as dirty</param>
        /// <param name="stream">Stream to which to write</param>
        /// <param name="serializedEntities">List of entities already serialized, used to prevent duplicates</param>
        private void WriteArchiveEntities(IEnumerable<OfflineEntity> entities, bool dirty, Stream stream, ArchiveIdManager serializedEntities)
        {
            // Used to delimit lines
            byte[] buffer = Encoding.UTF8.GetBytes("\r\n");

            // Create the serializer
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ArchiveEntity), knownTypes);

            // Loop through entities
            foreach (OfflineEntity entity in entities)
            {
                if (!serializedEntities.ContainsEntity(entity))
                {
                    // Write the object
                    serializer.WriteObject(stream, new ArchiveEntity
                    {
                        Entity = entity,
                        IsDirty = dirty
                    });

                    // Write the delimiter
                    stream.Write(buffer, 0, buffer.Length);
                }

                // Always record that we've processed the entities.  This can help
                // map atom id to property key in the event we have a tombstone come first
                // and only have an atom id the first time we encounter an entity.
                serializedEntities.ProcessedEntity(entity);
            }
        }

        /// <summary>
        /// Reads the archive file and stores it in the cache data
        /// </summary>
        /// <param name="file">file name to read</param>
        /// <param name="cacheData">in-memory data</param>
        private async Task ReadArchiveFile(string file, CacheData cacheData)
        {
            byte[] anchorBlob = null;
            List<ArchiveEntity> entities = new List<ArchiveEntity>();
            bool validFile = false;


            var isoFolder = ApplicationData.Current.LocalFolder;
            var cacheFolder = await isoFolder.GetFolderAsync(cachePath);

            using (Stream fileStream = await OpenReadFile(cacheFolder, file))
            using (Stream readStream = OpenReadCryptoStream(fileStream))
            using (StreamReader reader = new StreamReader(readStream))
            {
                // Create the serializer
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ArchiveEntity), knownTypes);

                // The approach here is to use a stream reader to read out each line, then convert that line to bytes
                // and use the json serializer.

                // If it's a curly brace, we're reading an entity
                while (!reader.EndOfStream && reader.Peek() == '{')
                {
                    string line = reader.ReadLine();
                    using (MemoryStream memStream = new MemoryStream(Encoding.UTF8.GetBytes(line)))
                    {
                        ArchiveEntity entity = (ArchiveEntity)serializer.ReadObject(memStream);
                        entities.Add(entity);
                    }
                }

                // If it's a square bracket, we're reading the anchor
                if (!reader.EndOfStream && reader.Peek() == '[')
                {
                    string line = reader.ReadLine();
                    using (MemoryStream memStream = new MemoryStream(Encoding.UTF8.GetBytes(line)))
                    {
                        anchorBlob = (byte[])(new DataContractJsonSerializer(typeof(byte[]))).ReadObject(memStream);

                        // The file is only valid if the anchor is read successfully.
                        validFile = true;
                    }
                }
            }

            // If the file was valid, we can use the data
            if (validFile)
            {
                // Clear existing data out
                cacheData.ClearCollections();
                changes.Clear();

                foreach (ArchiveEntity archiveEntity in entities)
                {
                    OfflineEntity isoEntity = archiveEntity.Entity;

                    // Determine whether the change was local or downloaded
                    if (archiveEntity.IsDirty)
                    {
                        cacheData.AddSerializedLocalChange(isoEntity);
                        changes.Add((OfflineEntityKey)isoEntity.GetIdentity(), isoEntity);
                    }
                    else
                    {
                        cacheData.AddSerializedDownloadItem(isoEntity);
                    }
                }

                // Set the anchor with the one read from the file.
                cacheData.AnchorBlob = anchorBlob;
            }
        }

        private async Task<Stream> OpenReadFile(StorageFolder isoFolder, string path)
        {
            var stream = await isoFolder.OpenStreamForReadAsync(path);

            return stream;

        }

        private Stream OpenReadCryptoStream(Stream stream)
        {
            return stream;
        }

        private Stream OpenWriteCryptoStream(Stream stream)
        {
            return stream;
        }

        private async Task<Stream> OpenWriteFile(StorageFolder isoFolder, string path)
        {
            var stream = await isoFolder.OpenStreamForWriteAsync(path, CreationCollisionOption.ReplaceExisting);
            return stream;
        }

        private async Task<byte[]> TransferArchiveFile(StorageFolder isoFolder, string fileName, Stream stream, ArchiveIdManager serializedItems)
        {
            byte[] currentAnchor = null;

            // Flush the stream
            stream.Flush();

            // Record the position.  If reading the source archive file fails, we want to reset the length to the current position.
            long position = stream.Position;

            byte[] eolBuffer = Encoding.UTF8.GetBytes("\r\n");

            using (Stream inputStream = await OpenReadFile(isoFolder, fileName))
            using (Stream readStream = OpenReadCryptoStream(inputStream))
            {
                bool validFile = false;

                using (StreamReader reader = new StreamReader(readStream))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ArchiveEntity), knownTypes);
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();

                        if (line.StartsWith("{"))
                        {

                            byte[] lineBuffer = Encoding.UTF8.GetBytes(line);
                            using (MemoryStream memStream = new MemoryStream(lineBuffer))
                            {
                                ArchiveEntity entity = (ArchiveEntity)serializer.ReadObject(memStream);

                                if (!serializedItems.ContainsEntity(entity.Entity))
                                {
                                    stream.Write(lineBuffer, 0, lineBuffer.Length);
                                    stream.Write(eolBuffer, 0, eolBuffer.Length);
                                }

                                serializedItems.ProcessedEntity(entity.Entity);
                            }
                        }
                        else if (line.StartsWith("["))
                        {
                            using (MemoryStream memStream = new MemoryStream(Encoding.UTF8.GetBytes(line)))
                            {
                                currentAnchor = (byte[])(new DataContractJsonSerializer(typeof(byte[]))).ReadObject(memStream);
                                validFile = true;
                                break;
                            }
                        }

                    }

                }

                // if the file wasn't valid, undo everything we just wrote by setting the length back to our original position
                if (!validFile)
                {
                    stream.SetLength(position);

                    // Throw an exception so the anchor doesn't get used and the archive file is scrapped (previously read files
                    // will be worthless anyway
                    throw new SerializationException("Transferring archive file failed");
                }
            }


            return currentAnchor;
        }



        class FileInfo
        {
            public CacheFileType FileType;
            public string FileName;
            public bool HasUploadFile;
        }

        [DataContract]
        internal class ArchiveEntity
        {
            [DataMember]
            public OfflineEntity Entity
            {
                get;
                set;
            }

            [DataMember]
            public bool IsDirty
            {
                get;
                set;
            }
        }

#if SLUNITTEST
        /// <summary>
        /// This method is solely to enable unit testing
        /// </summary>
        /// <param name="interval">Timer interval in milliseconds</param>
        public void SetArchiveInterval(long interval)
        {
            _timerInterval = interval;            
        }

        public void SetArchiveInterval(TimeSpan interval)
        {
            _timerInterval = (long)interval.TotalMilliseconds;
        }
#endif

        #endregion

    }
}
