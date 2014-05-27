
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Synchronization.ClientServices.Common;

namespace Microsoft.Synchronization.ClientServices
{
    /// <summary>
    /// Event args for the CacheRequestHandler.ProcessCacheRequestAsync call.
    /// </summary>
    public class CacheRequestResult
    {
        public Guid Id;
        public ChangeSet ChangeSet;
        public ChangeSetResponse ChangeSetResponse;
        public Exception Error;
        public Object State { get; set; }
        public uint BatchUploadCount;

        public CacheRequestResult(Guid id, ChangeSetResponse response, int uploadCount, Exception error, object state)
        {
            this.ChangeSetResponse = response;
            this.Error = error;
            this.State = state;
            this.Id = id;
            this.BatchUploadCount = (uint)uploadCount;

            // Check that error is carried over to the response
            if (this.Error == null) return;
            
            if (this.ChangeSetResponse == null)
                this.ChangeSetResponse = new ChangeSetResponse();
            
            this.ChangeSetResponse.Error = this.Error;
        }

        public CacheRequestResult(Guid id, ChangeSet changeSet, Exception error, object state)
        {
            this.ChangeSet = changeSet;
            this.Error = error;
            this.State = state;
            this.Id = id;
        }
    }
}
