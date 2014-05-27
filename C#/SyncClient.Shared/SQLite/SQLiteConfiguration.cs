using System;
using System.Collections.Generic;
using Microsoft;

namespace Microsoft.Synchronization.ClientServices.SQLite
{
    public class SQLiteConfiguration
    {
        /// <summary>
        /// Scope Name. May be unique in SQLite
        /// </summary>
        public String ScopeName { get; set; }

        /// <summary>
        /// Server Service URI
        /// </summary>
        public Uri ServiceUri { get; set; }
        
        /// <summary>
        /// All Types registered as Syncable
        /// </summary>
        public List<String> Types { get; set; }
        
        /// <summary>
        /// The last anchor blob received from the service.  Uploaded during every download and upload
        /// response.
        /// </summary>
        public Byte[] AnchorBlob { get; set; }
        
        /// <summary>
        /// Last Sync Date. 
        /// </summary>
        public DateTime LastSyncDate { get; set; }
    }

    internal class ScopeInfoTable
    {
        
        [PrimaryKey]
        public string ScopeName { get; set; }

        [MaxLength(250)]
        public String ServiceUri { get; set; }

        public DateTime LastSyncDate { get; set; }

        public Byte[] AnchorBlob { get; set; }

        [MaxLength(8000)]
        public String Configuration { get; set; }
    }
}