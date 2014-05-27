using System;
using System.ComponentModel;

namespace Microsoft.Synchronization.ClientServices.Common
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
        private Uri _editUri;
        private string _etag;
        private string _id;
        private bool _isTombstone;
    
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
            _isTombstone = isTombstone;
            _id = id;
            _etag = etag;
            _editUri = editUri;
        }

        /// <summary>
        /// Whether or not the entity to which this item is attached is a tombstone
        /// </summary>
        public bool IsTombstone
        {
            get { return _isTombstone; }

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
            get { return _id; }

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
            get { return _etag; }

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
            get { return _editUri; }

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

        #region INotifyPropertyChanged Members

        /// <summary>
        /// Property changed event for when any properties change (should only happen once).
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

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
            var metaData = new OfflineEntityMetadata(
                _isTombstone,
                _id,
                _etag,
                _editUri);

            return metaData;
        }
    }
}