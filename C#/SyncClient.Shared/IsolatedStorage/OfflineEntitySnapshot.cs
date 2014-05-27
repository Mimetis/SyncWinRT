using System;
using System.Collections.Generic;
using Microsoft.Synchronization.ClientServices.Common;

namespace Microsoft.Synchronization.ClientServices.IsolatedStorage
{
    /// <summary>
    /// Class which stores the snapshot of an entity.
    /// </summary>
    internal class OfflineEntitySnapshot
    {
        public OfflineEntitySnapshot()
        {
            this._properties = new Dictionary<string, object>();
        }

        /// <summary>
        /// Tick count of the snapshot
        /// </summary>
        public ulong TickCount
        {
            get; set;
        }

        /// <summary>
        /// State of the entity for which this is a snapshot
        /// </summary>
        public OfflineEntityState EntityState
        {
            get; set; 
        }

        /// <summary>
        /// Whether or not the snapshotted entity is a tombstone.
        /// </summary>
        public bool IsTombstone
        {
            get; set;
        }

        /// <summary>
        /// Mapping of property names to values representing the properties of the entity.
        /// </summary>
        public IDictionary<string, object> Properties
        {
            get
            {
                return _properties;
            }
        }

        /// <summary>
        /// Copy of the metadata the entity had before a snapshot was created.  This is important
        /// in the case of rolling back tombstones.
        /// </summary>
        public OfflineEntityMetadata Metadata
        {
            get;
            set;
        }

        /// <summary>
        /// Mapping of property names to values generated when a snapshot is created.
        /// </summary>
        Dictionary<string, object> _properties;
    }
}
