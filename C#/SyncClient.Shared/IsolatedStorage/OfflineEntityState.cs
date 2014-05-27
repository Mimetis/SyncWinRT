namespace Microsoft.Synchronization.ClientServices.IsolatedStorage
{
    /// <summary>
    /// The various states an entity can have.  They are as follows:
    ///     Detached - The entity has been created outside of the context and has not been added.
    ///     Unmodified - The entity has been received or sent during sync and has not been modified since
    ///     Modified - The entity has been updated, created, or deleted, but not saved.
    ///     Saved - The entity has been modified and saved.
    /// </summary>
    public enum OfflineEntityState
    {
        /// <summary>
        /// State when the entity is deleted. The instance referred by the app is set to Detached
        /// </summary>
        Detached,
        /// <summary>
        /// Represents the state of the entity before its modified by the app
        /// </summary>
        Unmodified,
        /// <summary>
        /// Represents the state of the entity when its modified
        /// </summary>
        Modified,
        /// <summary>
        /// Represents the state of the entity after it is saved
        /// </summary>
        Saved
    }
}
