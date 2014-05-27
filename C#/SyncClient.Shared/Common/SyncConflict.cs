namespace Microsoft.Synchronization.ClientServices.Common
{
    /// <summary>
    /// Represents a Synchronization related Conflict that was raised and handled on the server.
    /// </summary>
    public class SyncConflict : Conflict
    {
        /// <summary>
        /// This represents the version of the Entity that lost in the conflict resolution.
        /// </summary>
        public IOfflineEntity LosingEntity { get; set; }

        /// <summary>
        /// This represents the Conflict resolution policy that was applied
        /// </summary>
        public SyncConflictResolution Resolution { get; set;}
    }
}
