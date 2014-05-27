using System;
using System.Net;
using System.Collections.Generic;
using Microsoft.Synchronization.ClientServices.Common;

namespace Microsoft.Synchronization.ClientServices
{
    /// <summary>
    /// Wrapper Class representing all the related information about an Sync request
    /// </summary>
    class CacheRequest
    {
        public Guid RequestId;
        public ICollection<IOfflineEntity> Changes;
        public CacheRequestType RequestType;
        public SerializationFormat Format;
        public byte[] KnowledgeBlob;
        public bool IsLastBatch;
    }
}
