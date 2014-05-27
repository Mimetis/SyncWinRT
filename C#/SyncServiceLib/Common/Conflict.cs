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

using System.Runtime.Serialization;
#if SERVER
namespace Microsoft.Synchronization.Services
#elif CLIENT
namespace Microsoft.Synchronization.ClientServices
#endif
{
    /// <summary>
    /// Represents a Conflict base type. 
    /// </summary>
    [DataContract()]
    public class Conflict
    {
        /// <summary>
        /// Represents the current live version that is stored on the server. The version when applied on
        /// the client will ensure data convergence between server and client for this particular
        /// entity.
        /// </summary>
        [DataMember]
        public IOfflineEntity LiveEntity { get; internal set; }
    }
}
