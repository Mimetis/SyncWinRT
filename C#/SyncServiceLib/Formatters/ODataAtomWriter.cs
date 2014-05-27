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
using System.Xml;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
#if SERVER
using Microsoft.Synchronization.Services;
#elif CLIENT
using Microsoft.Synchronization.ClientServices;
#endif

namespace Microsoft.Synchronization.Services.Formatters
{
    /// <summary>
    /// SyncWriter implementation for the OData Atompub format
    /// </summary>
    class ODataAtomWriter : SyncWriter
    {
        XDocument _document;
        XElement _root;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serviceUri">Service Url to include as the base namespace</param>
        public ODataAtomWriter(Uri serviceUri)
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

            _document = new XDocument();

            // Add namespace prefixes
            XNamespace baseNs = this.BaseUri.ToString();
            XNamespace atom = FormatterConstants.AtomXmlNamespace;
            _root = new XElement(FormatterConstants.AtomXmlNamespace + FormatterConstants.AtomPubFeedElementName,
                new XAttribute(XNamespace.Xmlns + "base", baseNs),
                new XAttribute(FormatterConstants.AtomPubXmlNsPrefix, FormatterConstants.AtomXmlNamespace),
                new XAttribute(XNamespace.Xmlns + FormatterConstants.ODataDataNsPrefix, FormatterConstants.ODataDataNamespace),
                new XAttribute(XNamespace.Xmlns + FormatterConstants.ODataMetadataNsPrefix, FormatterConstants.ODataMetadataNamespace),
                new XAttribute(XNamespace.Xmlns + FormatterConstants.AtomDeletedEntryPrefix, FormatterConstants.AtomDeletedEntryNamespace),
                new XAttribute(XNamespace.Xmlns + FormatterConstants.SyncNsPrefix, FormatterConstants.SyncNamespace));


            // Add atom title element
            _root.Add(
                new XElement(atom + FormatterConstants.AtomPubTitleElementName, string.Empty));

            // Add atom id element
            _root.Add(new XElement(atom + FormatterConstants.AtomPubIdElementName, Guid.NewGuid().ToString("B")));

            // Add atom updated element
            _root.Add(new XElement(atom + FormatterConstants.AtomPubUpdatedElementName, XmlConvert.ToString(DateTime.Now, XmlDateTimeSerializationMode.Utc)));

            // add atom link element
            _root.Add(new XElement(atom + FormatterConstants.AtomPubLinkElementName,
                new XAttribute(FormatterConstants.AtomPubRelAttrName, "self"),
                new XAttribute(FormatterConstants.AtomPubHrefAttrName, string.Empty)));

            // Add the is last batch sync extension
            _root.Add(new XElement(FormatterConstants.SyncNamespace + FormatterConstants.MoreChangesAvailableText, !isLastBatch));

            // Add the serverBlob sync extension
            _root.Add(new XElement(FormatterConstants.SyncNamespace + FormatterConstants.ServerBlobText,
                (serverBlob != null) ? Convert.ToBase64String(serverBlob) : "null"));
        }

        /// <summary>
        /// Called by the runtime when all entities are written and contents needs to flushed to the underlying stream.
        /// </summary>
        /// <param name="writer">XmlWriter to which this feed will be serialized to</param>
        public override void WriteFeed(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            _document.Add(_root);

            // Write the </feed> end element.
            writer.WriteNode(_document.CreateReader(), true);
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
            XElement entryElement = WriteEntry(live, liveTempId, emitMetadataOnly);

            if (conflicting != null)
            {
                XElement conflictElement = new XElement(FormatterConstants.SyncNamespace + ((isConflict) ? FormatterConstants.SyncConlflictElementName : FormatterConstants.SyncErrorElementName));

                // Write the confliction resolution or errorElement.
                conflictElement.Add(new XElement(FormatterConstants.SyncNamespace + ((isConflict) ? FormatterConstants.ConflictResolutionElementName : FormatterConstants.ErrorDescriptionElementName), desc));

                // Write the confliction resolution or errorElement.

                XElement conflictingEntryElement = new XElement(FormatterConstants.SyncNamespace + ((isConflict) ? FormatterConstants.ConflictEntryElementName : FormatterConstants.ErrorEntryElementName));

                conflictingEntryElement.Add(WriteEntry(conflicting, conflictingTempId, false/*emitPartial*/));

                conflictElement.Add(conflictingEntryElement);

                entryElement.Add(conflictElement);
            }
            _root.Add(entryElement);
        }

        /// <summary>
        /// Writes the <entry/> tag and all its related elements.
        /// </summary>
        /// <param name="live">Actual entity whose value is to be emitted.</param>
        /// <param name="tempId">The temporary Id if any</param>
        /// <param name="emitPartial">Bool flag that denotes whether a partial metadata only entity is to be written</param>
        /// <returns>XElement representation of the entry element</returns>
        private XElement WriteEntry(IOfflineEntity live, string tempId, bool emitPartial)
        {
            string typeName = live.GetType().FullName;

            if (!live.ServiceMetadata.IsTombstone)
            {
                XElement entryElement = new XElement(FormatterConstants.AtomXmlNamespace + FormatterConstants.AtomPubEntryElementName);

                // Add Etag
                if (!string.IsNullOrEmpty(live.ServiceMetadata.ETag))
                {
                    entryElement.Add(new XAttribute(FormatterConstants.ODataMetadataNamespace + FormatterConstants.EtagElementName, live.ServiceMetadata.ETag));
                }

                // Add TempId element
                if (!string.IsNullOrEmpty(tempId))
                {
                    entryElement.Add(new XElement(FormatterConstants.SyncNamespace + FormatterConstants.TempIdElementName, tempId));
                }

                // Add Id element
                entryElement.Add(new XElement(FormatterConstants.AtomXmlNamespace + FormatterConstants.AtomPubIdElementName,
                    string.IsNullOrEmpty(live.ServiceMetadata.Id) ? string.Empty : live.ServiceMetadata.Id));

                // Add title element
                entryElement.Add(new XElement(FormatterConstants.AtomXmlNamespace + FormatterConstants.AtomPubTitleElementName,
                        new XAttribute(FormatterConstants.AtomPubTypeElementName, "text")));

                // Add updated element
                entryElement.Add(new XElement(FormatterConstants.AtomXmlNamespace + FormatterConstants.AtomPubUpdatedElementName,
                            XmlConvert.ToString(DateTime.Now, XmlDateTimeSerializationMode.Utc)));

                // Add author element
                entryElement.Add(new XElement(FormatterConstants.AtomXmlNamespace + FormatterConstants.AtomPubAuthorElementName,
                            new XElement(FormatterConstants.AtomXmlNamespace + FormatterConstants.AtomPubNameElementName)));

                // Write the <link> element
                entryElement.Add(
                    new XElement(FormatterConstants.AtomXmlNamespace + FormatterConstants.AtomPubLinkElementName,
                        new XAttribute(FormatterConstants.AtomPubRelAttrName, FormatterConstants.AtomPubEditLinkAttributeName),
                        new XAttribute(FormatterConstants.AtomPubTitleElementName, typeName),
                        new XAttribute(FormatterConstants.AtomPubHrefAttrName,
                            (live.ServiceMetadata.EditUri != null) ? live.ServiceMetadata.EditUri.ToString() : string.Empty)));

                // Write the <category> element
                entryElement.Add(
                    new XElement(FormatterConstants.AtomXmlNamespace + FormatterConstants.AtomPubCategoryElementName,
                        new XAttribute(FormatterConstants.AtomPubTermAttrName, live.GetType().FullName),
                        new XAttribute(FormatterConstants.AtomPubSchemaAttrName, FormatterConstants.ODataSchemaNamespace)));

                XElement contentElement = new XElement(FormatterConstants.AtomXmlNamespace + FormatterConstants.AtomPubContentElementName);

                if (!emitPartial)
                {
                    // Write the entity contents
                    contentElement.Add(WriteEntityContents(live));
                }

                // Add the contents entity to the outer entity.
                entryElement.Add(contentElement);

                return entryElement;
            }
            else
            {
                // Write the at:deleted-entry tombstone element
                XElement tombstoneElement = new XElement(FormatterConstants.AtomDeletedEntryNamespace + FormatterConstants.AtomDeletedEntryElementName);
                tombstoneElement.Add(new XElement(FormatterConstants.AtomNamespaceUri + FormatterConstants.AtomReferenceElementName, live.ServiceMetadata.Id));
                tombstoneElement.Add(new XElement(FormatterConstants.SyncNamespace + FormatterConstants.AtomPubCategoryElementName, typeName));
                return tombstoneElement;
            }
        }

        /// <summary>
        /// This writes the public contents of the Entity in the properties element.
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <returns>XElement representation of the properties element</returns>
        XElement WriteEntityContents(IOfflineEntity entity)
        {
            XElement contentElement = new XElement(FormatterConstants.ODataMetadataNamespace + FormatterConstants.PropertiesElementName);

            // Write only the primary keys if its an tombstone
            PropertyInfo[] properties = ReflectionUtility.GetPropertyInfoMapping(entity.GetType());

            // Write individual properties to the feed,
            foreach (PropertyInfo fi in properties)
            {
                string edmType = FormatterUtilities.GetEdmType(fi.PropertyType);
                object value = fi.GetValue(entity, null);
                Type propType = fi.PropertyType;
                if(fi.PropertyType.IsGenericType && fi.PropertyType.Name.Equals(FormatterConstants.NullableTypeName, StringComparison.InvariantCulture))
                {
                    // Its a Nullable<T> property
                    propType = fi.PropertyType.GetGenericArguments()[0];
                }

                if (value == null)
                {
                    contentElement.Add(
                        new XElement(FormatterConstants.ODataDataNamespace + fi.Name,
                            new XAttribute(FormatterConstants.ODataMetadataNamespace + FormatterConstants.AtomPubTypeElementName, edmType),
                            new XAttribute(FormatterConstants.ODataMetadataNamespace + FormatterConstants.AtomPubIsNullElementName, true)));
                }
                else if (propType == FormatterConstants.DateTimeType ||
                    propType == FormatterConstants.TimeSpanType ||
                    propType == FormatterConstants.DateTimeOffsetType)
                {
                    contentElement.Add(
                        new XElement(FormatterConstants.ODataDataNamespace + fi.Name,
                            new XAttribute(FormatterConstants.ODataMetadataNamespace + FormatterConstants.AtomPubTypeElementName, edmType),
                            FormatterUtilities.ConvertDateTimeForType_Atom(value, propType)));

                }
                else if (propType != FormatterConstants.ByteArrayType)
                {
                    contentElement.Add(
                        new XElement(FormatterConstants.ODataDataNamespace + fi.Name,
                            new XAttribute(FormatterConstants.ODataMetadataNamespace + FormatterConstants.AtomPubTypeElementName, edmType),
                            value));
                }
                else
                {
                    byte[] bytes = (byte[])value;
                    contentElement.Add(
                        new XElement(FormatterConstants.ODataDataNamespace + fi.Name,
                            new XAttribute(FormatterConstants.ODataMetadataNamespace + FormatterConstants.AtomPubTypeElementName, edmType),
                            Convert.ToBase64String(bytes)));
                }
            }

            return contentElement;
        }
    }
}
