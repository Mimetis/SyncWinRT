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
