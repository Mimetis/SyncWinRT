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

using System.Collections.Generic;
using System.Xml.Linq;
using System;

namespace Microsoft.Synchronization.Services.Formatters
{
    /// <summary>
    /// Internal helper class that reads and parses all relevant information about an entry element.
    /// </summary>
    internal abstract class EntryInfoWrapper
    {
        public string TypeName;
        public Dictionary<string, string> PropertyBag = new Dictionary<string, string>();
        public bool IsTombstone;
        public string ConflictDesc;
        public EntryInfoWrapper ConflictWrapper;
        public bool IsConflict;
        public string ETag;
        public string TempId;
        public Uri EditUri;
        public string Id;

        protected abstract void LoadConflictEntry(XElement entry);
        protected abstract void LoadEntryProperties(XElement entry);
        protected abstract void LoadTypeName(XElement entry);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader"></param>
        protected EntryInfoWrapper(XElement reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            PropertyBag = new Dictionary<string, string>();

            LoadTypeName(reader);
            LoadEntryProperties(reader);
            LoadConflictEntry(reader);
        }

    }
}
