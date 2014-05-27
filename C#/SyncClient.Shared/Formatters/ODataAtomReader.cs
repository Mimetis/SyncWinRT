using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using Microsoft.Synchronization.ClientServices;
using Microsoft.Synchronization.ClientServices.Common;

namespace Microsoft.Synchronization.Services.Formatters
{
    /// <summary>
    /// SyncReader implementation for the OData Atompub format
    /// </summary>
    class ODataAtomReader : SyncReader
    {
        /// <summary>
        /// Constructor with no KnownTypes specified
        /// </summary>
        /// <param name="stream">Input reader stream</param>
        public ODataAtomReader(Stream stream)
            : this(stream, null)
        { }

        /// <summary> 
        /// Constructor with KnownTypes specified
        /// </summary>
        /// <param name="stream">Input reader stream</param>
        /// <param name="knownTypes">List of types to reflect from</param>
        public ODataAtomReader(Stream stream, Type[] knownTypes)
            : base(stream, knownTypes)
        {
            reader = XmlReader.Create(stream);
        }

        /// <summary>
        /// Validates that the stream contains a valid feed item.
        /// </summary>
        public override void Start()
        {
            reader.MoveToContent();

            if (!AtomHelper.IsAtomElement(reader, FormatterConstants.AtomPubFeedElementName))
            {
                throw new InvalidOperationException("Not a valid ATOM feed.");
            }
        }

        /// <summary>
        /// Returns the current Item type at which the reader is positioned.
        /// </summary>
        public override ReaderItemType ItemType
        {
            get
            {
                return currentType;
            }
        }

        /// <summary>
        /// Returns the current entry element casted as an IOfflineEntity element
        /// </summary>
        /// <returns>Typed entry element</returns>
        public override IOfflineEntity GetItem()
        {
            CheckItemType(ReaderItemType.Entry);

            // Get the type name and the list of properties.
            currentEntryWrapper = new AtomEntryInfoWrapper((XElement)XNode.ReadFrom(reader));

            liveEntity = ReflectionUtility.GetObjectForType(currentEntryWrapper, this.knownTypes);
            return liveEntity;
        }

        /// <summary>
        /// Returns the value of the sync:hasMoreChanges element
        /// </summary>
        /// <returns>bool</returns>
        public override bool GetHasMoreChangesValue()
        {
            CheckItemType(ReaderItemType.HasMoreChanges);
            return (bool)reader.ReadElementContentAs(FormatterConstants.BoolType, null);
        }

        /// <summary>
        /// Returns the sync:serverBlob element contents
        /// </summary>
        /// <returns>byte[]</returns>
        public override byte[] GetServerBlob()
        {
            CheckItemType(ReaderItemType.SyncBlob);
            string encodedBlob = (string)reader.ReadElementContentAs(FormatterConstants.StringType, null);
            return Convert.FromBase64String(encodedBlob);
        }

        /// <summary>
        /// Traverses through the feed and returns when it arrives at the necessary element.
        /// </summary>
        /// <returns>bool detecting whether or not there is more elements to be read.</returns>
        public override bool Next()
        {
            // User did not read the node.
            if (currentType != ReaderItemType.BOF && !currentNodeRead)
            {
                reader.Skip();
            }

            do
            {
                currentEntryWrapper = null;
                liveEntity = null;
                if (AtomHelper.IsAtomElement(reader, FormatterConstants.AtomPubEntryElementName) ||
                    AtomHelper.IsAtomTombstone(reader, FormatterConstants.AtomDeletedEntryElementName))
                {
                    currentType = ReaderItemType.Entry;
                    currentNodeRead = false;
                    return true;
                }
                
                if (AtomHelper.IsSyncElement(reader, FormatterConstants.ServerBlobText))
                {
                    currentType = ReaderItemType.SyncBlob;
                    currentNodeRead = false;
                    return true;
                }
                
                if (AtomHelper.IsSyncElement(reader, FormatterConstants.MoreChangesAvailableText))
                {
                    currentType = ReaderItemType.HasMoreChanges;
                    currentNodeRead = false;
                    return true;
                }
            } while (reader.Read());

            this.currentType = ReaderItemType.EOF;

            return false;
        }
    }
}
