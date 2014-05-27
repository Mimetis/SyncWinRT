namespace Microsoft.Synchronization.ClientServices.Common
{
    /// <summary>
    /// Represents a Synchronization related backend store error that was raised and handled on the server.
    /// </summary>
    public class SyncError : Conflict
    {
        /// <summary>
        /// Represents a copy of the Client Entity that raised the error on the server.
        /// </summary>
        public IOfflineEntity ErrorEntity { get; set; }

        /// <summary>
        /// The description as sent by the sync service explaining the reason for the error.
        /// </summary>
        public string Description { get; internal set; }
    }
}
