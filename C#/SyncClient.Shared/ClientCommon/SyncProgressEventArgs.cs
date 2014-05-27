using Microsoft.Synchronization.ClientServices.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Synchronization.ClientServices
{
    public class SyncProgressEvent
    {

        /// <summary>
        /// Duration of the current sync stage
        /// </summary>
        public TimeSpan Duration { get; private set; }

        /// <summary>
        /// Sync progress stage
        /// </summary>
        public SyncStage SyncStage { get; private set; }

        /// <summary>
        /// An collection of IOfflineEntity objects being uploaded or downloaded.
        /// </summary>
        public ICollection<IOfflineEntity> Changes { get; private set; }

        /// <summary>
        /// An collection of conflict objects returning from Server after an uploading stage
        /// </summary>
        public ICollection<Conflict> Conflicts { get; private set; }

        /// <summary>
        /// A read only collection of Insert entities uploaded by clients that have been issued
        /// permanent Id's by the service
        /// </summary>
        public ICollection<IOfflineEntity> UpdatedItemsAfterInsertOnServer { get; private set; }

        /// <summary>
        /// Is last batch is only used for Downloading stage
        /// </summary>
        public Boolean IsLastBatch { get; set; }

        /// <summary>
        /// During SyncStage creating tables
        /// </summary>
        public Type CreatedTable { get; set; }

        public SyncProgressEvent(SyncStage stage, TimeSpan duration, Boolean isLastBatch = true,
                                 ICollection<IOfflineEntity> changes = null,
                                 ICollection<Conflict> conflicts = null,
                                 ICollection<IOfflineEntity> updatedItems = null)
        {
            this.SyncStage = stage;
            this.Duration = duration;
            this.IsLastBatch = IsLastBatch;
            this.Changes = changes;
            this.Conflicts = conflicts;
            this.UpdatedItemsAfterInsertOnServer = updatedItems;

        }
    }
}
