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

using System.Xml;
using System.Xml.Linq;

namespace Microsoft.Synchronization.Services.Formatters
{
    static class AtomHelper
    {
        /// <summary>
        /// Check whether the XmlReader is currently at the start of an element with 
        /// the given name in the Atom namespace
        /// </summary>
        /// <param name="reader">XmlReader to check on</param>
        /// <param name="name">Element name</param>
        /// <returns>True if the reader if at the indicated Atom element</returns>
        internal static bool IsAtomElement(XmlReader reader, string name)
        {
            return reader.NodeType == XmlNodeType.Element &&
                   reader.LocalName == name &&
                   reader.NamespaceURI == FormatterConstants.AtomNamespaceUri;
        }

        /// <summary>
        /// Check whether the XmlReader is currently at the start of an tombstone element with 
        /// the given name in the Tombstone namespace
        /// </summary>
        /// <param name="reader">XmlReader to check on</param>
        /// <param name="name">Element name</param>
        /// <returns>True if the reader if at the indicated Atom element</returns>
        internal static bool IsAtomTombstone(XmlReader reader, string name)
        {
            return reader.NodeType == XmlNodeType.Element &&
                   reader.LocalName == name &&
                   reader.NamespaceURI == FormatterConstants.AtomDeletedEntryNamespace;
        }

        /// <summary>
        /// Check whether the XmlReader is currently at the start of an element 
        /// in the Odata namespace
        /// </summary>
        /// <param name="reader">XmlReader to check on</param>
        /// <param name="ns">Element Namespace name</param>
        /// <returns>True if the reader if at the indicated namespace</returns>
        internal static bool IsODataNamespace(XmlReader reader, XNamespace ns)
        {
            return reader.NodeType == XmlNodeType.Element &&
                   reader.NamespaceURI == ns.NamespaceName;
        }

        /// <summary>
        /// Check whether the XmlReader is currently at the start of an element with 
        /// the given name in the sync namespace
        /// </summary>
        /// <param name="reader">XmlReader to check on</param>
        /// <param name="name">Element name</param>
        /// <returns>True if the reader if at the indicated anchor element</returns>
        internal static bool IsSyncElement(XmlReader reader, string name)
        {
            return reader.NodeType == XmlNodeType.Element &&
                   reader.LocalName == name &&
                   reader.NamespaceURI == FormatterConstants.SyncNamespace.NamespaceName;
        }
    }
}
