using System;
using Microsoft.Synchronization.ClientServices.Common;

namespace Microsoft.Synchronization.ClientServices.IsolatedStorage
{
    internal class WinEightSyncConflict : SyncConflict
    {
        public WinEightSyncConflict(SyncConflict conflict)
        {
            this.LiveEntity = conflict.LiveEntity;
            this.LosingEntity = conflict.LosingEntity;
            this.Resolution = conflict.Resolution;
        }

        internal string FileName
        {
            get;
            set;
        }
    }
}
