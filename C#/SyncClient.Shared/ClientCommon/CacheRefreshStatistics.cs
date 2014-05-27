using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Synchronization.ClientServices
{
    /// <summary>
    /// Class that represents the stats for a sync session.
    /// </summary>
    public class CacheRefreshStatistics
    {
        /// <summary>
        /// Denotes any exception that happened during Refresh.
        /// </summary>
        public Exception Error { 
            get; 
            set; }

        /// <summary>
        /// Bool that indicates whether the Refresh operation was cancelled or not.
        /// </summary>
        public bool Cancelled { get; set; }


        /// <summary>
        /// Start Time of Sync Session
        /// </summary>
        public DateTime StartTime { get; internal set; }

        /// <summary>
        /// End Time of Sync Session
        /// </summary>
        public DateTime EndTime { get; internal set; }

        /// <summary>
        /// Total number of change sets downloaded
        /// </summary>
        public uint TotalChangeSetsDownloaded { get; internal set; }

        /// <summary>
        /// Total number of change sets uploaded
        /// </summary>
        public uint TotalChangeSetsUploaded { get; internal set; }

        /// <summary>
        /// Total number of Uploded Items
        /// </summary>
        public uint TotalUploads { get; internal set; }

        /// <summary>
        /// Total number of downloaded items
        /// </summary>
        public uint TotalDownloads { get; internal set; }

        /// <summary>
        /// Total number of Sync Conflicts
        /// </summary>
        public uint TotalSyncConflicts { get; internal set; }

        /// <summary>
        /// Total number of Sync Conflicts
        /// </summary>
        public uint TotalSyncErrors { get; internal set; }
    }
}
