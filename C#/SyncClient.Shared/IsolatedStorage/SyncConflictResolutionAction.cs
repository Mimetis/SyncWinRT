
using System;

namespace Microsoft.Synchronization.ClientServices.IsolatedStorage
{
    /// <summary>
    /// Enumerator which represents the resolution actions which are available for store conflicts.
    /// There are only two choices, AcceptStoreEntity or AcceptModifiedEntity.  If a merge resolution
    /// is desired, the ModifiedEntity of the StoreConflict should be changed, and the AcceptModifiedEntity
    /// should be called.
    /// </summary>
    public enum SyncConflictResolutionAction
    {
        /// <summary>
        /// Accept the store version of the entity, rolling back changes to the modified entity.
        /// </summary>
        AcceptStoreEntity,

        /// <summary>
        /// Accept the modified entity, allowing it to be saved.
        /// </summary>
        AcceptModifiedEntity
    }
}
