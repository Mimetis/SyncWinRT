using System;
using Microsoft.Synchronization.ClientServices.Common;

namespace Microsoft.Synchronization.ClientServices.IsolatedStorage
{
    /// <summary>
    /// Conflict that happens when SaveChanges is attempted but a modified item has been
    /// changed during sync.  CurrentEntity is the entity which is represented by the store.
    /// Modified entity is the currently modified one.
    /// </summary>
    public class OfflineConflict : Conflict
    {
        private WinEightContext context;

        /// <summary>
        /// Constructor which intializes the StoreConflict with the specified context.
        /// </summary>
        /// <param name="context"></param>
        internal OfflineConflict(WinEightContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Returns the entity that the user has modified and which cannot currently be saved.
        /// </summary>
        public OfflineEntity ModifiedEntity { get; internal set; }

        /// <summary>
        /// Calls the context to resolve it.
        /// </summary>
        /// <param name="resolutionAction"></param>
        public void Resolve(SyncConflictResolutionAction resolutionAction)
        {
            if (context == null)
                throw new InvalidOperationException("Conflict has already been resolved");

            context.ResolveOfflineConflict(this, resolutionAction);
        }

        /// <summary>
        /// Clears out the context so that conflicts cannot be resolved multiple times.
        /// </summary>
        internal void ClearContext()
        {
            context = null;
        }
    }
}