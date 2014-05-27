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
using System.Xml;

namespace Microsoft.Synchronization.Services.Formatters
{
    /// <summary>
    /// Internal helper class that reads and parses all relevant information about an Json object.
    /// </summary>
    internal class JsonEntryInfoWrapper : EntryInfoWrapper
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader"></param>
        public JsonEntryInfoWrapper(XElement reader) : base(reader)
        { }

        /// <summary>
        /// Looks for a syncConflict key or an syncError key inside an Json object
        /// </summary>
        /// <param name="entry">entry element</param>
        protected override void LoadConflictEntry(XElement entry)
        {
            XElement conflictElement = entry.Element(FormatterConstants.JsonSyncConflictElementName);
            if (conflictElement != null)
            {
                // Its an conflict
                this.IsConflict = true;

                // Make sure it has an conflictResolution object
                XElement resolutionType = conflictElement.Element(FormatterConstants.ConflictResolutionElementName);
                if (resolutionType == null)
                {
                    throw new InvalidOperationException("Conflict resolution not specified for Json object " + this.TypeName);
                }
                this.ConflictDesc = resolutionType.Value;

                XElement conflictingChangeElement = conflictElement.Element(FormatterConstants.ConflictEntryElementName);
                if (conflictingChangeElement == null)
                {
                    throw new InvalidOperationException("conflictingChange not specified for Json syncConflict object " + this.TypeName);
                }

                this.ConflictWrapper = new JsonEntryInfoWrapper(conflictingChangeElement);
                return;
            }

            // Look for an errorElement object
            XElement errorElement = entry.Element(FormatterConstants.JsonSyncErrorElementName);
            if (errorElement != null)
            {
                // Its not an conflict
                this.IsConflict = false;

                // Make sure it has an sync:errorDescription element
                XElement errorDesc = errorElement.Element(FormatterConstants.ErrorDescriptionElementName);
                if (errorDesc != null)
                {
                    this.ConflictDesc = errorDesc.Value;
                }

                XElement errorChangeElement = errorElement.Element(FormatterConstants.ErrorEntryElementName);
                if (errorChangeElement == null)
                {
                    throw new InvalidOperationException("errorInChange not specified for Json syncError object " + this.TypeName);
                }

                this.ConflictWrapper = new JsonEntryInfoWrapper(errorChangeElement);
            }
        }

        /// <summary>
        /// Inspects all Key/value pairs in the JSON element to load all properties.
        /// </summary>
        /// <param name="entry">Entry element</param>
        protected override void LoadEntryProperties(XElement entry)
        {
            foreach (XElement keyValuePair in entry.Elements())
            {
                if (!keyValuePair.Name.LocalName.Equals(FormatterConstants.JsonSyncEntryMetadataElementName, StringComparison.InvariantCulture) &&
                    !keyValuePair.Name.LocalName.Equals(FormatterConstants.IsDeletedElementName, StringComparison.InvariantCulture))
                {
                    // Its a property we care if its not a _metadata or the isDeleted sync flag

                    XAttribute nullableAttr = keyValuePair.Attribute(FormatterConstants.AtomPubTypeElementName);
                    // Check to see if its a null property
                    if (nullableAttr != null && nullableAttr.Value.Equals("null", StringComparison.OrdinalIgnoreCase))
                    {
                        this.PropertyBag[keyValuePair.Name.LocalName] = null;
                    }
                    else
                    {
                        this.PropertyBag[keyValuePair.Name.LocalName] = keyValuePair.Value;
                    }
                }
            }
        }

        /// <summary>
        /// Looks for the 'type' key inside of the __metadata : {}  Json object
        /// </summary>
        /// <param name="entry">Entry element</param>
        protected override void LoadTypeName(XElement entry)
        {
            // Look for the __metadata key
            var metadata = entry.Elements(FormatterConstants.JsonSyncEntryMetadataElementName);

            foreach (var c in metadata)
            {
                this.TypeName = c.Element(FormatterConstants.JsonSyncEntryTypeElementName).Value;

                // Look for Id element
                if (c.Element(FormatterConstants.JsonSyncEntryUriElementName) != null)
                {
                    this.Id = c.Element(FormatterConstants.JsonSyncEntryUriElementName).Value;
                    this.EditUri = new Uri(this.Id, UriKind.RelativeOrAbsolute);
                }

                // Look for TempId element
                if (c.Element(FormatterConstants.TempIdElementName) != null)
                {
                    this.TempId = c.Element(FormatterConstants.TempIdElementName).Value;
                }

                if (string.IsNullOrEmpty(this.Id) && this.TempId == null)
                {
                    // No id or tempid element was found. Throw.
                    throw new InvalidOperationException("A uri or a tempId key must be present in the __metadata object. Entity in error: " + entry.ToString(SaveOptions.None));
                }

                // Look for ETag element
                if (c.Element(FormatterConstants.EtagElementName) != null)
                {
                    this.ETag = c.Element(FormatterConstants.EtagElementName).Value;
                }

                // Look for EditUri element
                if (c.Element(FormatterConstants.EditUriElementName) != null)
                {
                    this.EditUri = new Uri(c.Element(FormatterConstants.EditUriElementName).Value, UriKind.RelativeOrAbsolute);
                }

                // Look for the IsDeleted entry
                if (c.Element(FormatterConstants.IsDeletedElementName) != null)
                {
                    this.IsTombstone = bool.Parse(c.Element(FormatterConstants.IsDeletedElementName).Value);
                }
            }

            if (string.IsNullOrEmpty(TypeName))
            {
                throw new InvalidOperationException("Json object does not have a _metadata tag containing the type information.");
            }
        }
    }
}
