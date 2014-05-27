using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using Microsoft.Synchronization.ClientServices;
using Microsoft.Synchronization.ClientServices.Common;

namespace Microsoft.Synchronization.Services.Formatters
{

  

    /// <summary>
    /// Abstract class for SyncWriter that individual format writers needs to extend
    /// </summary>
    abstract class SyncWriter 
    {
        Uri _baseUri;

        protected Uri BaseUri
        {
            get
            {
                return this._baseUri;
            }
        }

        protected SyncWriter(Uri serviceUri)
        {
            if (serviceUri == null)
            {
                throw new ArgumentNullException("serviceUri");
            }

            this._baseUri = serviceUri;
        }

        public virtual void StartFeed(bool isLastBatch, byte[] serverBlob)
        {
        }

        /// <summary>
        /// Called to add a particular Entity
        /// </summary>
        /// <param name="entry">Entity to add to serialize to the stream</param>
        /// <param name="tempId">TempId for the Entity</param>
        public virtual void AddItem(IOfflineEntity entry, string tempId)
        {
            this.AddItem(entry, tempId, false);
        }

        /// <summary>
        /// Called to add a particular Entity
        /// </summary>
        /// <param name="entry">Entity to add to serialize to the stream</param>
        /// <param name="tempId">TempId for the Entity</param>
        /// <param name="emitMetadataOnly">Bool flag that denotes whether a partial metadata only entity is to be written</param>
        public virtual void AddItem(IOfflineEntity entry, string tempId, bool emitMetadataOnly)
        {
            if (entry == null)
            {
                
                throw new ArgumentNullException("entry");
            }

            if (string.IsNullOrEmpty(entry.GetServiceMetadata().Id) && entry.GetServiceMetadata().IsTombstone)
            {
                // Skip sending tombstones that dont have an Id as these were local create + delete.
                return;
            }
            WriteItemInternal(entry, tempId, null /*conflicting*/, null/*conflictingTempId*/, null /*desc*/, false /*isconflict*/, emitMetadataOnly);
        }

        public abstract void WriteItemInternal(IOfflineEntity live, string liveTempId, IOfflineEntity conflicting, string conflictingTempId, string desc, bool isConflict, bool emitMetadataOnly);
        public abstract void WriteFeed(XmlWriter writer);     
    }
}
