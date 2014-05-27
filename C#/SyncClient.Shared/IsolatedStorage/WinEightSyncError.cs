using System;

namespace Microsoft.Synchronization.ClientServices.Common
{
    internal class WinEightSyncError : SyncError
    {
        public WinEightSyncError(SyncError error)
        {
            this.LiveEntity = error.LiveEntity;
            this.ErrorEntity = error.ErrorEntity;
            this.Description = error.Description;
        }

        internal string FileName
        {
            get;
            set;
        }
    }
}
