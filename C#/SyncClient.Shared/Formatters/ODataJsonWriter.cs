using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Synchronization.ClientServices;
using Microsoft.Synchronization.ClientServices.Common;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Converters;

namespace Microsoft.Synchronization.Services.Formatters
{
    class ODataJsonWriter : SyncWriter
    {
        XDocument doc;
        XElement root;
        XElement results;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serviceUri">Service Url to include as the base namespace</param>
        public ODataJsonWriter(Uri serviceUri)
            : base(serviceUri)
        { }

        /// <summary>
        /// Should be called prior to any Items are added to the stream. This ensures that the stream is 
        /// set up with the right doc level feed parameters
        /// </summary>
        /// <param name="isLastBatch">Whether this feed will be the last batch or not.</param>
        /// <param name="serverBlob">Sync server blob.</param>
        public override void StartFeed(bool isLastBatch, byte[] serverBlob)
        {
            base.StartFeed(isLastBatch, serverBlob);
            doc = new XDocument();

            // Add the {d} root element
            XElement nRoot = new XElement(FormatterConstants.JsonDocumentElementName,
                new XAttribute(FormatterConstants.JsonTypeAttributeName, JsonElementTypes.Object));

            // Add the 'root' root element.
            doc.Add(nRoot);

            // Create an element for the base {d} object
            this.root = new XElement(FormatterConstants.JsonRootElementName,
                new XAttribute(FormatterConstants.JsonTypeAttributeName, JsonElementTypes.Object));

            // Add the Json root element to the Doc element root
            nRoot.Add(this.root);

            // Add the __Sync element;
            XElement syncElement = new XElement(FormatterConstants.JsonSyncMetadataElementName,
                new XAttribute(FormatterConstants.JsonTypeAttributeName, JsonElementTypes.Object));

            // Add the is last batch sync extension
            syncElement.Add(new XElement(FormatterConstants.MoreChangesAvailableText,
                new XAttribute(FormatterConstants.JsonTypeAttributeName, JsonElementTypes.Boolean),
                !isLastBatch));

            // Add the serverBlob sync extension
            syncElement.Add(new XElement(FormatterConstants.ServerBlobText,
                new XAttribute(FormatterConstants.JsonTypeAttributeName, (serverBlob != null) ? JsonElementTypes.String : JsonElementTypes.Object),
                (serverBlob != null) ? Convert.ToBase64String(serverBlob) : "null"));

            // Add the sync element to the root.
            this.root.Add(syncElement);

            // Create the results element
            results = new XElement(FormatterConstants.JsonSyncResultsElementName,
                new XAttribute(FormatterConstants.JsonTypeAttributeName, JsonElementTypes.Array));

            this.root.Add(results);
        }

        /// <summary>
        /// Called by the runtime when all entities are written and contents needs to flushed to the underlying stream.
        /// </summary>
        /// <param name="writer">XmlWriter to which this Json feed will be serialized to</param>
        public override void WriteFeed(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            //_doc.Add(_root);

            // Write the </feed> end element.
            writer.WriteNode(doc.CreateReader(), true);
            writer.Flush();
        }

        /// <summary>
        /// Adds an IOfflineEntity and its associated Conflicting/Error entity as an Atom entry element
        /// </summary>
        /// <param name="live">Live Entity</param>
        /// <param name="liveTempId">TempId for the live entity</param>
        /// <param name="conflicting">Conflicting entity that will be sent in synnConflict or syncError extension</param>
        /// <param name="conflictingTempId">TempId for the conflicting entity</param>
        /// <param name="desc">Error description or the conflict resolution</param>
        /// <param name="isConflict">Denotes if its an errorElement or conflict. Used only when <paramref name="desc"/> is not null</param>
        /// <param name="emitMetadataOnly">Bool flag that denotes whether a partial metadata only entity is to be written</param>
        public override void WriteItemInternal(IOfflineEntity live, string liveTempId, IOfflineEntity conflicting, string conflictingTempId, string desc, bool isConflict, bool emitMetadataOnly)
        {
            XElement entryElement = WriteEntry(live, null, liveTempId, emitMetadataOnly);

            if (conflicting != null)
            {
                XElement conflictElement = new XElement(((isConflict) ? FormatterConstants.JsonSyncConflictElementName : FormatterConstants.JsonSyncErrorElementName),
                    new XAttribute(FormatterConstants.JsonTypeAttributeName, JsonElementTypes.Object));

                // Write the confliction resolution or errorElement.
                conflictElement.Add(new XElement(((isConflict) ? FormatterConstants.ConflictResolutionElementName : FormatterConstants.ErrorDescriptionElementName),
                    new XAttribute(FormatterConstants.JsonTypeAttributeName, JsonElementTypes.String),
                    desc));

                // Write the confliction resolution or errorElement.
                XElement conflictingEntryElement = new XElement(((isConflict) ? FormatterConstants.ConflictEntryElementName : FormatterConstants.ErrorEntryElementName),
                                        new XAttribute(FormatterConstants.JsonTypeAttributeName, JsonElementTypes.Object));

                // Write the Conflicting entry in
                WriteEntry(conflicting, conflictingEntryElement, conflictingTempId, false/*emitPartial*/);

                // Add the conflicting entry data in to the conflict element
                conflictElement.Add(conflictingEntryElement);

                // Add the conflict element to the live entry
                entryElement.Add(conflictElement);
            }

            results.Add(entryElement);
        }

        /// <summary>
        /// Writes the Json object tag and all its related elements.
        /// </summary>
        /// <param name="live">Actual entity whose value is to be emitted.</param>
        /// <param name="entryElement">This is the parent entry element that is needs to go in to. Will be null for regular items and non null for 
        /// conflict/error items only</param>
        /// <param name="tempId">The tempId for the element if passed in by the client.</param>
        /// <param name="emitPartial">Bool flag that denotes whether a partial metadata only entity is to be written</param>
        /// <returns>XElement representation of the entry element</returns>
        private XElement WriteEntry(IOfflineEntity live, XElement entryElement, string tempId, bool emitPartial)
        {
            string typeName = live.GetType().FullName;

            if (entryElement == null)
            {
                entryElement = new XElement("item",
                   new XAttribute(FormatterConstants.JsonTypeAttributeName, JsonElementTypes.Object));
            }

            // Write the _metadata object for this entry
            XElement entryMetadata = new XElement(FormatterConstants.JsonSyncEntryMetadataElementName,
                new XAttribute(FormatterConstants.JsonTypeAttributeName, JsonElementTypes.Object));

            // Add the tempId to metadata
            if (!string.IsNullOrEmpty(tempId))
            {
                entryMetadata.Add(new XElement(FormatterConstants.TempIdElementName,
                    new XAttribute(FormatterConstants.JsonTypeAttributeName, JsonElementTypes.String), tempId));
            }

            // Add the uri to metadata
            entryMetadata.Add(new XElement(FormatterConstants.JsonSyncEntryUriElementName,
                new XAttribute(FormatterConstants.JsonTypeAttributeName, JsonElementTypes.String),
                string.IsNullOrEmpty(live.GetServiceMetadata().Id) ? string.Empty : live.GetServiceMetadata().Id));

            // Add the etag to metadata
            if (!string.IsNullOrEmpty(live.GetServiceMetadata().ETag))
            {
                entryMetadata.Add(new XElement(FormatterConstants.EtagElementName,
                    new XAttribute(FormatterConstants.JsonTypeAttributeName, JsonElementTypes.String), live.GetServiceMetadata().ETag));
            }

            // Add the edituri to metadata
            if (live.GetServiceMetadata().EditUri != null)
            {
                entryMetadata.Add(new XElement(FormatterConstants.EditUriElementName,
                    new XAttribute(FormatterConstants.JsonTypeAttributeName, JsonElementTypes.String), live.GetServiceMetadata().EditUri));
            }

            // Add the type to metadata
            entryMetadata.Add(new XElement(FormatterConstants.JsonSyncEntryTypeElementName,
                new XAttribute(FormatterConstants.JsonTypeAttributeName, JsonElementTypes.String),
                typeName));

            // Write the tombstone element
            if (live.GetServiceMetadata().IsTombstone)
            {
                entryMetadata.Add(new XElement(FormatterConstants.IsDeletedElementName,
                    new XAttribute(FormatterConstants.JsonTypeAttributeName, JsonElementTypes.Boolean),
                    true));
            }
            else if (!emitPartial)
            {
                // Write the entity contents only when its not a tombstone
                WriteEntityContentsToElement(entryElement, live);
            }

            // Add the metadata to the entry element
            entryElement.Add(entryMetadata);

            return entryElement;
        }

        /// <summary>
        /// This writes the public contents of the Entity to the passed in XElement.
        /// </summary>
        /// <param name="contentElement">The XElement to which the type properties is added to</param>
        /// <param name="entity">Entity</param>
        /// <returns>XElement representation of the properties element</returns>
        void WriteEntityContentsToElement(XElement contentElement, IOfflineEntity entity)
        {
            IEnumerable<PropertyInfo> properties = ReflectionUtility.GetPropertyInfoMapping(entity.GetType());

            // Write individual properties to the feed,
            foreach (PropertyInfo fi in properties)
            {
                object objValue = fi.GetValue(entity, null);
                if (objValue == null)
                {
                    contentElement.Add(
                        new XElement(fi.Name,
                            new XAttribute(FormatterConstants.JsonTypeAttributeName, JsonElementTypes.Null), null));
                }
                else if (fi.PropertyType == FormatterConstants.CharType ||
                    fi.PropertyType == FormatterConstants.StringType ||
                    fi.PropertyType == FormatterConstants.GuidType)
                {
                     contentElement.Add(
                        new XElement(fi.Name,
                            new XAttribute(FormatterConstants.JsonTypeAttributeName, JsonElementTypes.String),
                             objValue));
                }
                else if (fi.PropertyType == FormatterConstants.DateTimeType ||
                    fi.PropertyType == FormatterConstants.TimeSpanType ||
                    fi.PropertyType == FormatterConstants.DateTimeOffsetType)
                {
                    contentElement.Add(
                        new XElement(fi.Name,
                            new XAttribute(FormatterConstants.JsonTypeAttributeName, JsonElementTypes.String),
                            FormatterUtilities.ConvertDateTimeForType_Json(objValue, fi.PropertyType)));
                }
                else if (fi.PropertyType == FormatterConstants.BoolType)
                {
                    contentElement.Add(
                        new XElement(fi.Name,
                            new XAttribute(FormatterConstants.JsonTypeAttributeName, JsonElementTypes.Boolean),
                            objValue));
                }
                else if (fi.PropertyType == FormatterConstants.ByteArrayType)
                {
                    byte[] bytes = (byte[])objValue;
                    contentElement.Add(
                        new XElement(fi.Name,
                            new XAttribute(FormatterConstants.JsonTypeAttributeName, JsonElementTypes.String),
                            Convert.ToBase64String(bytes)));
                }
                else if (fi.PropertyType.IsGenericType() && fi.PropertyType.Name.Equals(FormatterConstants.NullableTypeName, StringComparison.CurrentCultureIgnoreCase))
                {
                    // Its a Nullable<T> property
                    // OLD Type genericParamType = fi.PropertyType.GetGenericArguments()[0];
                    Type genericParamType = fi.PropertyType.GenericTypeArguments[0];

                    string elementType = JsonElementTypes.Number;
                    if (genericParamType == FormatterConstants.BoolType)
                    {
                        elementType = JsonElementTypes.Boolean;
                    }
                    else if (genericParamType == FormatterConstants.DateTimeType ||
                        genericParamType == FormatterConstants.TimeSpanType ||
                        genericParamType == FormatterConstants.DateTimeOffsetType)
                    {
                        contentElement.Add(
                            new XElement(fi.Name,
                                new XAttribute(FormatterConstants.JsonTypeAttributeName, JsonElementTypes.String),
                                FormatterUtilities.ConvertDateTimeForType_Json(objValue, genericParamType)));
                        continue;
                    }
                    else if (genericParamType == FormatterConstants.CharType ||
                        genericParamType == FormatterConstants.GuidType)
                    {
                        elementType = JsonElementTypes.String;
                    }

                    contentElement.Add(
                        new XElement(fi.Name,
                            new XAttribute(FormatterConstants.JsonTypeAttributeName, elementType),
                            objValue));

                }
                else // Its a number
                {
                    contentElement.Add(
                        new XElement(fi.Name,
                            new XAttribute(FormatterConstants.JsonTypeAttributeName, JsonElementTypes.Number),
                            objValue));
                }
            }
        }


    }
}
