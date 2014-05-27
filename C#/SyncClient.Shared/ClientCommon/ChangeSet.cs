using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Synchronization.ClientServices.Common;

namespace Microsoft.Synchronization.ClientServices
{
    /// <summary>
    /// Denotes a list of changes that is either to be uploaded or downloaded.
    /// </summary>
    public class ChangeSet
    {
        /// <summary>
        /// The Server blob (locally stored for an upload and server version for an Download)
        /// </summary>
        public byte[] ServerBlob { get; set; }

        /// <summary>
        /// An collection of IOfflineEntity objects which depicts the actual data being uploaded or downloaded.
        /// </summary>
        public ICollection<IOfflineEntity> Data { get; set; }

        /// <summary>
        /// Flag depicting whether or not this is a last batch or not.
        /// </summary>
        public bool IsLastBatch { get; set; }

        /// <summary>
        /// Public constructor for ChangeSet object. Instantiates with an empty collection for Data and default values of null for 
        /// serverBlob and true for IsLastBatch
        /// </summary>
        public ChangeSet()
        {
            this.ServerBlob = null;
            this.Data = new List<IOfflineEntity>();
            this.IsLastBatch = true;
        }
        internal void AddItem(IOfflineEntity iOfflineEntity)
        {
            if (Data == null)
            {
                Data = new List<IOfflineEntity>();
            }
            Data.Add(iOfflineEntity);
        }


    }
}
