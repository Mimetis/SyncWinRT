// Copyright 2010 Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License"); 
// You may not use this file except in compliance with the License. 
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 

// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, 
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR 
// CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, 
// MERCHANTABLITY OR NON-INFRINGEMENT. 

// See the Apache 2 License for the specific language governing 
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
#if SERVER
using Microsoft.Synchronization.Services;
#elif CLIENT
using Microsoft.Synchronization.ClientServices;
#endif

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
#if SERVER
            // For server the serverBlob should never be empty
            if (serverBlob == null)
            {
                throw new ArgumentNullException("serverBlob");
            }
#endif
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

            if(string.IsNullOrEmpty(entry.ServiceMetadata.Id) && entry.ServiceMetadata.IsTombstone)
            {
                // Skip sending tombstones that dont have an Id as these were local create + delete.
                return;
            }
            WriteItemInternal(entry, tempId, null /*conflicting*/, null/*conflictingTempId*/, null /*desc*/, false /*isconflict*/, emitMetadataOnly);
        }

#if SERVER
        /// <summary>
        /// Called to add a Sync conflict item
        /// </summary>
        /// <param name="winningEntry">the winning entity</param>
        /// <param name="winningEntryTempId">the winning entity's tempId</param>
        /// <param name="losingEntry">The losing entity</param>
        /// <param name="losingEntryTempId">The losing entity's tempId</param>
        /// <param name="resolution">The conflict resolution aplied by the server</param>
        public virtual void AddConflictItem(IOfflineEntity winningEntry, string winningEntryTempId, IOfflineEntity losingEntry, string losingEntryTempId, SyncConflictResolution resolution)
        {
            if (winningEntry == null)
            {
                throw new ArgumentNullException("winningEntry");
            }

            if (losingEntry == null)
            {
                throw new ArgumentNullException("losingEntry");
            }
            WriteItemInternal(winningEntry, winningEntryTempId, losingEntry/*conflicting*/, losingEntryTempId, resolution.ToString() /*desc*/, true/*isconflict*/, false/*emitMetadataOnly*/);
        }

        /// <summary>
        /// Called to add a Sync Error item
        /// </summary>
        /// <param name="liveEntry">Live version of the entity</param>
        /// <param name="errorEntry">Version of the entity that caused the error.</param>
        /// <param name="errorEntryTempId">TempIf for the entity that caused the error.</param>
        /// <param name="errorDescription">Description of error.</param>
        public virtual void AddErrorItem(IOfflineEntity liveEntry, IOfflineEntity errorEntry, string errorEntryTempId, string errorDescription)
        {
            if (liveEntry == null)
            {
                throw new ArgumentNullException("liveEntry");
            }

            if (errorEntry == null)
            {
                throw new ArgumentNullException("errorEntry");
            }
            WriteItemInternal(liveEntry, null/*liveEntryTempId*/, errorEntry/*conflicting*/, errorEntryTempId, errorDescription/*desc*/, false /*isconflict*/, false/*emitMetadataOnly*/);
        }
#endif
      
        public abstract void WriteItemInternal(IOfflineEntity live, string liveTempId, IOfflineEntity conflicting, string conflictingTempId, string desc, bool isConflict, bool emitMetadataOnly);
        public abstract void WriteFeed(XmlWriter writer);     
    }
}
