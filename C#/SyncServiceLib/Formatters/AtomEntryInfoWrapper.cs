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
using System.Xml.Linq;
using System.Globalization;

namespace Microsoft.Synchronization.Services.Formatters
{
    class AtomEntryInfoWrapper : EntryInfoWrapper
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader"></param>
        public AtomEntryInfoWrapper(XElement reader) : base(reader)
        { }

        /// <summary>
        /// Looks for a sync:syncConflict or an sync:syncError element
        /// </summary>
        /// <param name="entry">entry element</param>
        protected override void LoadConflictEntry(XElement entry)
        {
            XElement conflictElement = entry.Element(FormatterConstants.SyncNamespace + FormatterConstants.SyncConlflictElementName);
            if (conflictElement != null)
            {
                // Its an conflict
                this.IsConflict = true;

                // Make sure it has an sync:conflictResolution element
                XElement resolutionType = conflictElement.Element(FormatterConstants.SyncNamespace + FormatterConstants.ConflictResolutionElementName);
                if (resolutionType == null)
                {
                    throw new InvalidOperationException("Conflict resolution not specified for entry element " + this.TypeName);
                }
                this.ConflictDesc = resolutionType.Value;

                XElement conflictingChangeElement = conflictElement.Element(FormatterConstants.SyncNamespace + FormatterConstants.ConflictEntryElementName);
                if (conflictingChangeElement == null)
                {
                    throw new InvalidOperationException("conflictingChange not specified for syncConflict element " + this.TypeName);
                }

                this.ConflictWrapper = new AtomEntryInfoWrapper(GetSubElement(conflictingChangeElement));
                return;
            }

            // Look for an errorElement element
            XElement errorElement = entry.Element(FormatterConstants.SyncNamespace + FormatterConstants.SyncErrorElementName);
            if (errorElement != null)
            {
                // Its not an conflict
                this.IsConflict = false;

                // Make sure it has an sync:errorDescription element
                XElement errorDesc = errorElement.Element(FormatterConstants.SyncNamespace + FormatterConstants.ErrorDescriptionElementName);
                if (errorDesc != null)
                {
                    this.ConflictDesc = errorDesc.Value;
                }

                XElement errorChangeElement = errorElement.Element(FormatterConstants.SyncNamespace + FormatterConstants.ErrorEntryElementName);
                if (errorChangeElement == null)
                {
                    throw new InvalidOperationException("errorInChange not specified for syncError element " + this.TypeName);
                }

                this.ConflictWrapper = new AtomEntryInfoWrapper(GetSubElement(errorChangeElement));
            }
        }

        /// <summary>
        /// Looks for either a &lt;entry/&gt; or a &lt;deleted-entry/&gt; subelement within the outer element.
        /// </summary>
        /// <param name="entryElement">The outer entry element</param>
        /// <returns>The inner entry or the deleted-entry subelement</returns>
        private XElement GetSubElement(XElement entryElement)
        {
            XElement element = entryElement.Element(FormatterConstants.AtomNamespaceUri + FormatterConstants.AtomPubEntryElementName);
            if (element == null)
            {
                element = entryElement.Element(FormatterConstants.AtomDeletedEntryNamespace + FormatterConstants.AtomDeletedEntryElementName);
            }
            return element;
        }

        /// <summary>
        /// Inspects all m.properties element in the entry element to load all properties.
        /// </summary>
        /// <param name="entry">Entry element</param>
        protected override void LoadEntryProperties(XElement entry)
        {
            if (entry.Name.Namespace.Equals(FormatterConstants.AtomDeletedEntryNamespace))
            {
                // Read the tombstone
                this.IsTombstone = true;

                XElement id = entry.Element(FormatterConstants.AtomXmlNamespace + FormatterConstants.AtomReferenceElementName);
                if (id != null)
                {
                    this.Id = id.Value;
                }

                if (string.IsNullOrEmpty(this.Id))
                {
                    // No atom:id element was found in the tombstone. Throw.
                    throw new InvalidOperationException("A atom:ref element must be present for a tombstone entry. Entity in error: " + entry.ToString(SaveOptions.None));
                }
            }
            else
            {
                XElement properties = null;

                // Read ETag if present
                XAttribute etag = entry.Attribute(FormatterConstants.ODataMetadataNamespace + FormatterConstants.EtagElementName);
                if (etag != null)
                {
                    this.ETag = etag.Value;
                }

                // Read TempId if present
                XElement tempId = entry.Element(FormatterConstants.SyncNamespace + FormatterConstants.TempIdElementName);
                if (tempId != null)
                {
                    this.TempId = tempId.Value;
                }

                // Read Id if present
                XElement id = entry.Element(FormatterConstants.AtomXmlNamespace + FormatterConstants.AtomPubIdElementName);
                if (id != null)
                {
                    this.Id = id.Value;
                }

                if (string.IsNullOrEmpty(this.Id) && this.TempId == null)
                {
                    // No atom:id or sync:tempid element was found. Throw.
                    throw new InvalidOperationException("A atom:id or a sync:tempId element must be present. Entity in error: " + entry.ToString(SaveOptions.None));
                }

                // Read EditUri if present
                foreach (XElement linkElement in entry.Elements(FormatterConstants.AtomXmlNamespace + FormatterConstants.AtomPubLinkElementName))
                {
                    foreach (XAttribute attr in linkElement.Attributes(FormatterConstants.AtomPubRelAttrName))
                    {
                        if (attr.Value.Equals(FormatterConstants.AtomPubEditLinkAttributeName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (this.EditUri != null)
                            {
                                // Found duplicate edit urls in payload. Throw.
                                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Multiple Edit Url's found for atom with {0}: '{1}'", (this.Id == null) ? "TempId" : "Id", (this.Id == null) ? this.TempId : this.Id));
                            }
                            XAttribute hrefAttr = linkElement.Attribute(FormatterConstants.AtomPubHrefAttrName);
                            if (hrefAttr == null)
                            {
                                // No href="" attribute fouund in entry. Throw. 
                                throw new InvalidOperationException(
                                    string.Format("No href attribute found in the edit link for atom with  {0}: '{1}'",
                                        (this.Id == null) ? "TempId" : "Id", (this.Id == null) ? this.TempId : this.Id));
                            }
                            this.EditUri = new Uri(hrefAttr.Value, UriKind.RelativeOrAbsolute);
                        }
                    }
                }

                XElement content = entry.Element(FormatterConstants.AtomNamespaceUri + FormatterConstants.AtomPubContentElementName);
                if (content != null)
                {
                    properties = content.Element(FormatterConstants.ODataMetadataNamespace + FormatterConstants.PropertiesElementName);
                }

                // Load the properties as a property bag
                if (properties != null)
                {
                    foreach (XElement property in properties.Elements())
                    {
                        if (property.Name.Namespace == FormatterConstants.ODataDataNamespace)
                        {
                            XAttribute nullableAttr = property.Attribute(FormatterConstants.ODataMetadataNamespace + FormatterConstants.AtomPubIsNullElementName);
                            if (nullableAttr != null && nullableAttr.Value.Equals(Boolean.TrueString, StringComparison.OrdinalIgnoreCase))
                            {
                                this.PropertyBag[property.Name.LocalName] = null;
                            }
                            else
                            {
                                this.PropertyBag[property.Name.LocalName] = property.Value;
                            }

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Looks for the category element in the entry for the type name
        /// </summary>
        /// <param name="entry">Entry element</param>
        protected override void LoadTypeName(XElement entry)
        {
            bool isTombstone = (entry.Name.Namespace == FormatterConstants.AtomDeletedEntryNamespace);
            var categories = (isTombstone)
                ? entry.Elements(FormatterConstants.SyncNamespace + FormatterConstants.AtomPubCategoryElementName)
                : entry.Elements(FormatterConstants.AtomNamespaceUri + FormatterConstants.AtomPubCategoryElementName);
         
            foreach (var c in categories)
            {
                if (isTombstone)
                {
                    this.TypeName = c.Value;
                }
                else
                {
                    this.TypeName = c.Attribute(FormatterConstants.AtomPubTermAttrName).Value;
                }
            }

            if (string.IsNullOrEmpty(TypeName))
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, "Category element not found in {0} element.", (isTombstone)? FormatterConstants.AtomDeletedEntryElementName : FormatterConstants.AtomPubEntryElementName));
            }
        }
    }
}
