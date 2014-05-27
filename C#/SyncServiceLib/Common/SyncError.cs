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

#if SERVER
namespace Microsoft.Synchronization.Services
#elif CLIENT
namespace Microsoft.Synchronization.ClientServices
#endif
{
    /// <summary>
    /// Represents a Synchronization related backend store error that was raised and handled on the server.
    /// </summary>
    public class SyncError : Conflict
    {
        /// <summary>
        /// Represents a copy of the Client Entity that raised the error on the server.
        /// </summary>
        public IOfflineEntity ErrorEntity { get; set; }

        /// <summary>
        /// The description as sent by the sync service explaining the reason for the error.
        /// </summary>
        public string Description { get; internal set; }
    }
}
