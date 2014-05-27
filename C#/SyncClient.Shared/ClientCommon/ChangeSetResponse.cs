
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Microsoft.Synchronization.ClientServices.Common
{
    /// <summary>
    /// Denotes a response for the a ChangeSet that was uploaded.
    /// </summary>
    public class ChangeSetResponse
    {
        List<Conflict> _conflicts;
        List<IOfflineEntity> _updatedItems;

        /// <summary>
        /// Server knowledge from the server after applying the changes remotely.
        /// </summary>
        public byte[] ServerBlob { get; set; }

        /// <summary>
        /// Any fatal/protocol related error encountered while applying the upload
        /// </summary>
        public Exception Error { get; set; }

        /// <summary>
        /// An collection of conflict objects
        /// </summary>
        public ReadOnlyCollection<Conflict> Conflicts
        {
            get
            {
                return new ReadOnlyCollection<Conflict>(_conflicts);
            }
        }

        /// <summary>
        /// A read only collection of Insert entities uploaded by clients that have been issued
        /// permanent Id's by the service
        /// </summary>
        public ReadOnlyCollection<IOfflineEntity> UpdatedItems
        {
            get
            {
                return new ReadOnlyCollection<IOfflineEntity>(_updatedItems);
            }
        }

        internal ChangeSetResponse()
        {
            _conflicts = new List<Conflict>();
            _updatedItems = new List<IOfflineEntity>();
        }

        internal void AddConflict(Conflict conflict)
        {
            this._conflicts.Add(conflict);
        }

        internal void AddUpdatedItem(IOfflineEntity item)
        {
            this._updatedItems.Add(item);
        }

        internal List<Conflict> ConflictsInternal
        {
            get
            {
                return this._conflicts;
            }
        }

    }
}
