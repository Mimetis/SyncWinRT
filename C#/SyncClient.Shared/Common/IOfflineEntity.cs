namespace Microsoft.Synchronization.ClientServices.Common
{
    /// <summary>
    /// Represents the base interface that all offline cacheable object should derive from.
    /// </summary>
    public interface IOfflineEntity
    {
        /// <summary>
        /// Represents the sync and OData metadata used for the entity
        /// </summary>
        OfflineEntityMetadata GetServiceMetadata();

        void SetServiceMetadata(OfflineEntityMetadata value);
    }
}

