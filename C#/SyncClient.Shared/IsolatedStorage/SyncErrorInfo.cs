using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Synchronization.ClientServices.Common;

namespace Microsoft.Synchronization.ClientServices.IsolatedStorage
{
    /// <summary>
    /// Class which contains the last conflict and error that occurred during sync
    /// for a given entity.
    /// </summary>
    public class SyncErrorInfo : INotifyPropertyChanged
    {
        private WinEightContext context;
        private SyncConflict syncConflict;
        private SyncError syncError;

        /// <summary>
        /// Whether or not the error info has a sync conflict.
        /// </summary>
        public bool HasSyncConflict
        {
            get { return syncConflict != null; }
        }

        /// <summary>
        /// Whether or not the error info has a sync error.
        /// </summary>
        public bool HasSyncError
        {
            get { return syncError != null; }
        }

        /// <summary>
        /// The sync error.
        /// </summary>
        public SyncError SyncError
        {
            get { return syncError; }
        }

        /// <summary>
        /// The sync conflict.
        /// </summary>
        public SyncConflict SyncConflict
        {
            get { return syncConflict; }
        }

        /// <summary>
        /// Clears the sync conflict and removes it from the conflict list on the c.
        /// </summary>
        public async Task ClearSyncConflict()
        {
            if (syncConflict != null)
            {
                await context.ClearSyncConflict(syncConflict);
                syncConflict = null;

                OnPropertyChanged("SyncConflict");
                OnPropertyChanged("HasSyncConflict");
            }
        }

        /// <summary>
        /// Sets the sync conflict to null. This method is not thread safe.    
        /// </summary>
        internal void UnsafeClearSyncConflict()
        {
            syncConflict = null;

            OnPropertyChanged("SyncConflict");
            OnPropertyChanged("HasSyncConflict");
        }

        /// <summary>
        ///  Sets the sync error to null. This method is not thread safe.
        /// </summary>
        internal void UnsafeClearSyncError()
        {
            syncConflict = null;

            OnPropertyChanged("SyncConflict");
            OnPropertyChanged("HasSyncConflict");
        }

        /// <summary>
        /// Clears the sync error and removes it from the error list on the c.
        /// </summary>
        public async Task ClearSyncError()
        {
            if (syncError != null)
            {
                await context.ClearSyncError(syncError);
                syncError = null;

                OnPropertyChanged("SyncError");
                OnPropertyChanged("HasSyncError");
            }
        }

        /// <summary>
        /// Sets the sync conflict, providing the cache data so that the conflict can be removed if ClearSyncConflict
        /// is called.
        /// </summary>
        ///<param name="c">IsolatedStorageOfflineContext</param>
        /// <param name="sc">conflict to set.</param>
        internal void SetSyncConflict(WinEightContext c, SyncConflict sc)
        {
            SyncConflict oldConflict = this.syncConflict;

            this.context = c;
            this.syncConflict = sc;

            OnPropertyChanged("SyncConflict");

            if (oldConflict == null)
            {
                OnPropertyChanged("HasSyncConflict");
            }
        }

        /// <summary>
        /// Sets the sync error, providing the cache data so that the error can be removed if ClearSyncerror
        /// is called.
        /// </summary>
        ///<param name="c">IsolatedStorageOfflineContext</param>
        /// <param name="se">error to set.</param>
        internal void SetSyncError(WinEightContext c, SyncError se)
        {
            SyncError oldError = this.syncError;

            this.context = c;
            this.syncError = se;

            OnPropertyChanged("SyncError");

            if (oldError == null)
            {
                OnPropertyChanged("HasSyncError");
            }
        }

        #region INotifyPropertyChanged

        /// <summary>
        /// Event raised when a property is changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Private method which handles raising the property changed event.
        /// </summary>
        /// <param name="propertyName">Name of the property for which the event is being raised</param>
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}