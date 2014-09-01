using System;
using System.Net;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Synchronization.ClientServices.Common;


namespace Microsoft.Synchronization.ClientServices
{
    /// <summary>
    /// Class used for synchronizing an offline cache with a remote sync service.
    /// </summary>
    public class CacheController
    {
        private OfflineSyncProvider localProvider;
        private Uri serviceUri;
        private CacheControllerBehavior controllerBehavior;
        private HttpCacheRequestHandler cacheRequestHandler;
        private Guid changeSetId;
        private object lockObject = new object(); // Object used for locking access to the cancelled flag
        private bool beginSessionComplete;


        public static void DebugMemory(string categoryName)
        {
#if WINDOWS_PHONE
            Debug.WriteLine("--------------------------------------------------");
            Debug.WriteLine("\t" + categoryName);
            Debug.WriteLine("--------------------------------------------------");
            Debug.WriteLine("App Cur Mem : " + Microsoft.Phone.Info.DeviceStatus.ApplicationCurrentMemoryUsage / 1024 / 1024);
            Debug.WriteLine("App Mem Lim : " + Microsoft.Phone.Info.DeviceStatus.ApplicationMemoryUsageLimit / 1024 / 1024);
            Debug.WriteLine("Device Mem  : " + Microsoft.Phone.Info.DeviceStatus.DeviceTotalMemory / 1024 / 1024);
            Debug.WriteLine("--------------------------------------------------");
#endif
        }

        /// <summary>
        /// Returns the reference to the CacheControllerBehavior object that can be used to 
        /// customize the CacheController's settings.
        /// </summary>
        public CacheControllerBehavior ControllerBehavior
        {
            get { return this.controllerBehavior; }
        }



        /// <summary>
        /// Constructor for CacheController
        /// </summary>
        /// <param name="serviceUri">Remote sync service Uri with a trailing "/" parameter.</param>
        /// <param name="scopeName">The scope name being synchronized</param>
        /// <param name="localProvider">The OfflineSyncProvider instance for the local store.</param>
        public CacheController(Uri serviceUri, string scopeName, OfflineSyncProvider localProvider)
        {
            if (serviceUri == null)
                throw new ArgumentNullException("serviceUri");

            if (string.IsNullOrEmpty(scopeName))
                throw new ArgumentNullException("scopeName");

            if (!serviceUri.Scheme.Equals("http", StringComparison.CurrentCultureIgnoreCase) &&
                !serviceUri.Scheme.Equals("https", StringComparison.CurrentCultureIgnoreCase))
                throw new ArgumentException("Uri must be http or https schema", "serviceUri");

            if (localProvider == null)
                throw new ArgumentNullException("localProvider");

            this.serviceUri = serviceUri;
            this.localProvider = localProvider;

            this.controllerBehavior = new CacheControllerBehavior();
            this.controllerBehavior.ScopeName = scopeName;
        }

        /// <summary>
        /// Method that synchronize the Cache by uploading all modified changes and then downloading the
        /// server changes.
        /// </summary>
        internal async Task<CacheRefreshStatistics> SynchronizeAsync()
        {
            return await SynchronizeAsync(CancellationToken.None);
        }

        /// <summary>
        /// Method that synchronize the Cache by uploading all modified changes and then downloading the
        /// server changes.
        /// </summary>
        internal async Task<CacheRefreshStatistics> SynchronizeAsync(CancellationToken cancellationToken,
                                                                     IProgress<SyncProgressEvent> progress = null)
        {
            CacheRefreshStatistics statistics = new CacheRefreshStatistics();

            // Check if cancellation has occured
            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();

            // set start time
            statistics.StartTime = DateTime.Now;

            // Reporting progress 
            if (progress != null)
                progress.Report(new SyncProgressEvent(SyncStage.StartingSync, TimeSpan.Zero));

            try
            {

                // First create the CacheRequestHandler
                this.cacheRequestHandler = new HttpCacheRequestHandler(this.serviceUri, this.controllerBehavior);

                // Then fire the BeginSession call on the local provider.
                await this.localProvider.BeginSession();

                // Set the flag to indicate BeginSession was successful
                this.beginSessionComplete = true;

                // Check if cancellation has occured
                if (cancellationToken.IsCancellationRequested)
                    cancellationToken.ThrowIfCancellationRequested();

                // Do uploads first (no batch mode on Upload Request)
                statistics = await this.EnqueueUploadRequest(statistics, cancellationToken, progress);

                // If there is an error during Upload request, dont want to donwload
                if (statistics.Error != null)
                    throw new Exception("Error occured during Upload request.", statistics.Error);

                // Check if cancellation has occured
                if (cancellationToken.IsCancellationRequested)
                    cancellationToken.ThrowIfCancellationRequested();

                // then Download (be careful, could be in batch mode)
                statistics = await this.EnqueueDownloadRequest(statistics, cancellationToken, progress);

                // Set end time
                statistics.EndTime = DateTime.Now;

                // Call EndSession only if BeginSession was successful.
                if (this.beginSessionComplete)
                    this.localProvider.EndSession();


            }
            catch (OperationCanceledException ex)
            {
                statistics.EndTime = DateTime.Now;
                statistics.Cancelled = true;
                statistics.Error = ex;

                if (this.beginSessionComplete)
                    this.localProvider.EndSession();
            }
            catch (Exception ex)
            {
                statistics.EndTime = DateTime.Now;
                statistics.Error = ex;

                if (this.beginSessionComplete)
                    this.localProvider.EndSession();
            }
            finally
            {
                // Reset the state
                this.ResetAsyncWorkerManager();
            }

            // Reporting progress 
            if (progress != null)
                progress.Report(new SyncProgressEvent(SyncStage.EndingSync, statistics.EndTime.Subtract(statistics.StartTime)));

            return statistics;

        }

        /// <summary>
        ///  Reset the state of the objects
        /// </summary>
        private void ResetAsyncWorkerManager()
        {
            lock (this.lockObject)
            {
                this.cacheRequestHandler = null;
                this.controllerBehavior.Locked = false;
                this.beginSessionComplete = false;
            }
        }

        /// <summary>
        /// Method that performs an upload. It gets the ChangeSet from the local provider and then creates an
        /// CacheRequest object for that ChangeSet and then passed the processing asynchronously to the underlying
        /// CacheRequestHandler.
        /// </summary>
        private async Task<CacheRefreshStatistics> EnqueueUploadRequest(CacheRefreshStatistics statistics,
                                                                        CancellationToken cancellationToken,
                                                                        IProgress<SyncProgressEvent> progress = null)
        {
            this.changeSetId = Guid.NewGuid();

            try
            {
                // Check if cancellation has occured
                if (cancellationToken.IsCancellationRequested)
                    cancellationToken.ThrowIfCancellationRequested();

                // Get Changes
                DateTime durationStartDate = DateTime.Now;
                ChangeSet changeSet = await this.localProvider.GetChangeSet(this.changeSetId);

                // Reporting progress after get changes from local store
                if (progress != null)
                    progress.Report(new SyncProgressEvent(SyncStage.GetChanges, DateTime.Now.Subtract(durationStartDate), true, (changeSet != null ? changeSet.Data : null)));


                // No data to upload. Skip upload phase.
                if (changeSet == null || changeSet.Data == null || changeSet.Data.Count == 0)
                    return statistics;

                // Create a SyncRequest out of this.
                CacheRequest request = new CacheRequest
                {
                    RequestId = this.changeSetId,
                    Format = this.ControllerBehavior.SerializationFormat,
                    RequestType = CacheRequestType.UploadChanges,
                    Changes = changeSet.Data,
                    KnowledgeBlob = changeSet.ServerBlob,
                    IsLastBatch = changeSet.IsLastBatch
                };


                // Upload changes to server
                durationStartDate = DateTime.Now;
                var requestResult = await this.cacheRequestHandler.ProcessCacheRequestAsync(
                    request, changeSet.IsLastBatch, cancellationToken);

                // Get response from server if mb any conflicts or updated items
                statistics = await this.ProcessCacheRequestResults(statistics, requestResult, cancellationToken);

                // Reporting progress after uploading changes, and mb get back Conflicts and new Id from insterted items
                if (progress != null)
                    progress.Report(new SyncProgressEvent(SyncStage.UploadingChanges, DateTime.Now.Subtract(durationStartDate), true,
                                                            changeSet.Data, requestResult.ChangeSetResponse.Conflicts, requestResult.ChangeSetResponse.UpdatedItems));

            }
            catch (OperationCanceledException)
            {
                // Re throw the operation cancelled
                throw;
            }
            catch (Exception e)
            {
                if (ExceptionUtility.IsFatal(e))
                    throw;

                statistics.Error = e;
            }


            return statistics;
        }

        /// <summary>
        /// Method that performs a download. It gets the server blob anchor from the local provider and then creates an 
        /// CacheRequest object for that download request. It then passes the processing asynchronously to the underlying
        /// CacheRequestHandler.
        /// </summary>
        private async Task<CacheRefreshStatistics> EnqueueDownloadRequest(CacheRefreshStatistics statistics,
                                                                          CancellationToken cancellationToken,
                                                                          IProgress<SyncProgressEvent> progress = null)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                    cancellationToken.ThrowIfCancellationRequested();

                Boolean isLastBatch = false;

                while (!isLastBatch)
                {
                    if (cancellationToken.IsCancellationRequested)
                        cancellationToken.ThrowIfCancellationRequested();

                    // Create a SyncRequest for download.
                    CacheRequest request = new CacheRequest
                    {
                        Format = this.ControllerBehavior.SerializationFormat,
                        RequestType = CacheRequestType.DownloadChanges,
                        KnowledgeBlob = this.localProvider.GetServerBlob()
                    };

                    // Get Changes
                    DateTime durationStartDate = DateTime.Now;
                    var requestResult = await this.cacheRequestHandler.ProcessCacheRequestAsync(
                        request, null, cancellationToken);

                    statistics = await this.ProcessCacheRequestResults(statistics, requestResult, cancellationToken);

                    // Check if we are at the end 
                    if (requestResult.ChangeSet == null || requestResult.ChangeSet.IsLastBatch)
                        isLastBatch = true;

                    // Reporting progress after get changes from local store
                    if (progress != null)
                        progress.Report(new SyncProgressEvent(SyncStage.DownloadingChanges, DateTime.Now.Subtract(durationStartDate), true, (requestResult.ChangeSet != null ? requestResult.ChangeSet.Data : null)));
                }
            }
            catch (OperationCanceledException)
            {
                // Re throw the operation cancelled
                throw;
            }
            catch (Exception e)
            {
                if (ExceptionUtility.IsFatal(e))
                    throw;

                statistics.Error = e;
            }

            return statistics;
        }

        /// <summary>
        /// Called whenever the CacheRequestHandler proceeses an upload/download request. It is also responsible for
        /// issuing another request if it wasnt the last batch. In case of receiving an Upload response it calls the
        /// underlying provider with the status of the upload. In case of Download it notifies the local provider of the
        /// changes that it needs to save.
        /// </summary>
        private async Task<CacheRefreshStatistics> ProcessCacheRequestResults(
            CacheRefreshStatistics statistics, CacheRequestResult cacheRequestResult, CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                    cancellationToken.ThrowIfCancellationRequested();

                #region Error
                if (cacheRequestResult.Error != null)
                {

                    // We have an error but we have a ChangeSetResponse with reading the upload respose
                    // So we can serialize results and update dirty bits 
                    if (cacheRequestResult.ChangeSetResponse != null
                        && cacheRequestResult.HttpStep == HttpState.End)
                        await this.localProvider.OnChangeSetUploaded(cacheRequestResult.Id, cacheRequestResult.ChangeSetResponse);

                    // Finally complete Refresh with error.
                    statistics.Error = cacheRequestResult.Error;

                    return statistics;
                }
                #endregion

                #region Upload response

                if (cacheRequestResult.ChangeSetResponse != null)
                {
                    if (cacheRequestResult.ChangeSetResponse.Error == null 
                        && cacheRequestResult.HttpStep == HttpState.End)
                        await this.localProvider.OnChangeSetUploaded(cacheRequestResult.Id, cacheRequestResult.ChangeSetResponse);

                    if (cacheRequestResult.ChangeSetResponse.Error != null)
                    {
                        statistics.Error = cacheRequestResult.ChangeSetResponse.Error;
                        return statistics;
                    }

                    // Increment the ChangeSets uploaded count
                    statistics.TotalChangeSetsUploaded++;
                    statistics.TotalUploads += cacheRequestResult.BatchUploadCount;

                    // Update refresh stats
                    foreach (var e1 in cacheRequestResult.ChangeSetResponse.ConflictsInternal)
                    {
                        if (e1 is SyncConflict)
                            statistics.TotalSyncConflicts++;
                        else
                            statistics.TotalSyncErrors++;
                    }

                    return statistics;

                }
                #endregion

                #region Download Response

                // it's a response to download
                Debug.Assert(cacheRequestResult.ChangeSet != null, "Completion is not for a download request.");

                // Increment the refresh stats
                if (cacheRequestResult.ChangeSet != null && cacheRequestResult.ChangeSet.Data != null && cacheRequestResult.ChangeSet.Data.Count > 0)
                {
                    statistics.TotalChangeSetsDownloaded++;
                    statistics.TotalDownloads += (uint)cacheRequestResult.ChangeSet.Data.Count;

                    await this.localProvider.SaveChangeSet(cacheRequestResult.ChangeSet);
                }

                return statistics;
                #endregion

            }

            catch (OperationCanceledException)
            {
                // Re throw the operation cancelled
                throw;
            }
            catch (Exception exp)
            {
                if (ExceptionUtility.IsFatal(exp))
                    throw;
                statistics.Error = exp;
            }

            return statistics;
        }




    }
}
