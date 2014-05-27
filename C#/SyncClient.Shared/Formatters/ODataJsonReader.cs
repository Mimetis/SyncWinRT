using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Synchronization.ClientServices;
using Microsoft.Synchronization.ClientServices.Common;

namespace Microsoft.Synchronization.Services.Formatters
{
    internal class ODataJsonReader : SyncReader
    {
        private bool traversingResultsNode;

        /// <summary>
        /// Constructor with no KnownTypes specified
        /// </summary>
        /// <param name="stream">Input reader stream</param>
        public ODataJsonReader(Stream stream)
            : this(stream, null)
        {
        }

        /// <summary>
        /// Constructor with KnownTypes specified
        /// </summary>
        /// <param name="stream">Input reader stream</param>
        /// <param name="knownTypes">List of types to reflect from</param>
        public ODataJsonReader(Stream stream, Type[] knownTypes)
            : base(stream, knownTypes)
        {
            
            //    reader = JsonReaderWriterFactory.CreateJsonReader(stream, XmlDictionaryReaderQuotas.Max);
            reader = new XmlJsonReader(stream, XmlDictionaryReaderQuotas.Max);
        }


        /// <summary>
        /// Returns the current Item type at which the reader is positioned.
        /// </summary>
        public override ReaderItemType ItemType
        {
            get { return currentType; }
        }

        /// <summary>
        /// Validates that the stream contains a valid Json feed.
        /// </summary>
        public override void Start()
        {
            reader.MoveToContent();

            if (reader.Name != FormatterConstants.JsonDocumentElementName)
                throw new InvalidOperationException("Not a valid Json Feed.");
        }

        /// <summary>
        /// Returns the current Json object casted as an IOfflineEntity element
        /// </summary>
        /// <returns>Typed entry element</returns>
        public override IOfflineEntity GetItem()
        {
            CheckItemType(ReaderItemType.Entry);

            // Get the type name and the list of properties.
            XElement elem = (XElement) XNode.ReadFrom(reader);

            currentEntryWrapper = new JsonEntryInfoWrapper(elem);

            liveEntity = ReflectionUtility.GetObjectForType(currentEntryWrapper, this.knownTypes);
            return liveEntity;
        }

        /// <summary>
        /// Returns the value of the hasMoreChanges key in __sync:{} Json object
        /// </summary>
        /// <returns>bool</returns>
        public override bool GetHasMoreChangesValue()
        {
            CheckItemType(ReaderItemType.HasMoreChanges);
            //var result = reader.ReadContentAsBoolean();
            var result = reader.ReadElementContentAs(FormatterConstants.BoolType, null);
            return (bool) result;
        }

        /// <summary>
        /// Returns the serverBlob key contents from the __sync:{} Json object
        /// </summary>
        /// <returns>byte[]</returns>
        public override byte[] GetServerBlob()
        {
            CheckItemType(ReaderItemType.SyncBlob);
            var encodedBlob = (string) reader.ReadElementContentAs(FormatterConstants.StringType, null);
            return Convert.FromBase64String(encodedBlob);
        }

        /// <summary>
        /// Traverses through the feed and returns when it arrives at the necessary object.
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
                if (JsonHelper.IsElement(reader, FormatterConstants.JsonSyncResultsElementName))
                {
                    if (traversingResultsNode && reader.IsStartElement())
                        throw new InvalidOperationException("Json feed has more than one results entry. Invalid stream.");
                    
                    traversingResultsNode = reader.IsStartElement();
                }
                else if (JsonHelper.IsElement(reader, "item") && traversingResultsNode)
                {
                    currentType = ReaderItemType.Entry;
                    currentNodeRead = false;
                    return true;
                }
                else if (JsonHelper.IsElement(reader, FormatterConstants.ServerBlobText))
                {
                    currentType = ReaderItemType.SyncBlob;
                    currentNodeRead = false;
                    return true;
                }
                else if (JsonHelper.IsElement(reader, FormatterConstants.MoreChangesAvailableText))
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