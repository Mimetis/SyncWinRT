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
using System.ComponentModel;


#if SERVER
namespace Microsoft.Synchronization.Services
#elif CLIENT
namespace Microsoft.Synchronization.ClientServices
#endif
    {
    /// <summary>
    /// Class that represents the metadata required for the sync protocol to work correctly.
    /// Applications should not change these properties except when required by the protocol
    /// (the Id will change for an item that is inserted for the first time).
    /// The exception to this is the IsTombstone property, which should be set when the application
    /// is using a custom store and an item is being deleted.  Applications using the
    /// IsolatedStorageOfflineContext should never set any properties.
    /// </summary>
    public class OfflineEntityMetadata : INotifyPropertyChanged
    {
        /// <summary>
        /// Public constructor for the metadata
        /// </summary>
        public OfflineEntityMetadata()
        {
            _isTombstone = false;
            _id = null;
            _etag = null;
            _editUri = null;
        }

        /// <summary>
        /// Public constructor for the metadata which takes parameters for the metadata.
        /// </summary>
        /// <param name="isTombstone">Whether or not the entity is a tombstone</param>
        /// <param name="id">Sync id for the item</param>
        /// <param name="etag">Item's OData ETag</param>
        /// <param name="editUri">Item's OData Edit Uri</param>
        public OfflineEntityMetadata(bool isTombstone, string id, string etag, Uri editUri)
        {
            this._isTombstone = isTombstone;
            this._id = id;
            this._etag = etag;
            this._editUri = editUri;
        }

        /// <summary>
        /// Whether or not the entity to which this item is attached is a tombstone
        /// </summary>
        public bool IsTombstone 
        { 
            get
            {
                return _isTombstone;
            }

            set
            {
                if (value != _isTombstone)
                {
                    _isTombstone = value;
                    RaisePropertyChanged("IsTombstone");
                }
            }
        }

        /// <summary>
        /// The Id for the entity used for synchronization.  This should not be set by applications
        /// and should be empty when an item is first uploaded.  It will be subsequently filled in 
        /// during the upload response.
        /// </summary>
        public string Id 
        {
            get
            {
                return _id;
            }

            set
            {
                if (value != _id)
                {
                    _id = value;
                    RaisePropertyChanged("Id");
                }
            }
        }

        /// <summary>
        /// The OData ETag for the item.  This should not be set by applications and should
        /// be empty during the first upload for an item.  It will subsequently be filled in
        /// after upload.
        /// </summary>
        public string ETag 
        {
            get
            {
                return _etag;
            }

            set
            {
                if (value != _etag)
                {
                    _etag = value;
                    RaisePropertyChanged("ETag");
                }
            }

        }

        /// <summary>
        /// The OData edit Uri for the item.  This should not be set by applications and should
        /// be empty during the first upload for an item.  It will subsequently be filled in
        /// after upload.
        /// </summary>
        public Uri EditUri 
        {
            get
            {
                return _editUri;
            }

            set
            {
                if (value != _editUri)
                {
                    _editUri = value;
                    RaisePropertyChanged("EditUri");
                }
            }
        }

        // INotifyPropertyChanged

        /// <summary>
        /// Property changed event for when any properties change (should only happen once).
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Used while creating Snapshot to do a depp copy of the original copy's metadata
        /// </summary>
        /// <returns></returns>
        internal OfflineEntityMetadata Clone()
        {
            OfflineEntityMetadata metaData = new OfflineEntityMetadata(
                _isTombstone,
                _id,
                _etag,
                _editUri);

            return metaData;
        }

        private bool _isTombstone;
        private string _id;
        private string _etag;
        private Uri _editUri;
    }
}

