using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.Synchronization.ClientServices.Common;

namespace Microsoft.Synchronization.ClientServices.IsolatedStorage
{
    [DataContract]
    internal class ResponseData
    {
        [DataMember]
        public byte[] Anchor
        {
            get;
            set;
        }

        [DataMember]
        public IEnumerable<IOfflineEntity> Entities
        {
            get;
            set;
        }
    }
}
