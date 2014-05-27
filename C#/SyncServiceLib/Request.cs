// Copyright © Microsoft Corporation. All rights reserved.

// Microsoft Limited Permissive License (Ms-LPL)

// This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.

// 1. Definitions
// The terms “reproduce,” “reproduction,” “derivative works,” and “distribution” have the same meaning here as under U.S. copyright law.
// A “contribution” is the original software, or any additions or changes to the software.
// A “contributor” is any person that distributes its contribution under this license.
// “Licensed patents” are a contributor’s patent claims that read directly on its contribution.

// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any contributors’ name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
// (E) The software is licensed “as-is.” You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.
// (F) Platform Limitation- The licenses granted in sections 2(A) & 2(B) extend only to the software or derivative works that you create that run on a Microsoft Windows operating system product.

using System.Collections.Generic;
using Microsoft.Synchronization.Services.SqlProvider;

namespace Microsoft.Synchronization.Services
{
    /// <summary>
    /// Desribes an incoming request and contains all necessary data needed to process the request.
    /// </summary>
    internal sealed class Request
    {
        #region Properties

        /// <summary>
        /// Indicates the type of request.
        /// </summary>
        internal RequestCommand RequestCommand { get; private set; }

        /// <summary>
        /// This is a dictionary of parameters that will be required to process a request. 
        /// The keys in the dictionary are of type CommandParamType
        /// </summary>
        internal Dictionary<CommandParamType, object> CommandParams { get; private set; }

        /// <summary>
        /// Blob that is passed to the client for every upload and download request.
        /// </summary>
        internal byte[] SyncBlob { get; set; }
        
        internal HttpContextServiceHost ServiceHost { get; private set; }

        /// <summary>
        /// List of entities parsed from the incoming request payload.
        /// </summary>
        internal List<IOfflineEntity> EntityList { get; private set; }

        /// <summary>
        /// Contains the filter parameters used by the provider.
        /// </summary>
        internal List<SqlSyncProviderFilterParameterInfo> ProviderParams { get; set; }

        /// <summary>
        /// Gets/sets the serialization format for the outgoing response.
        /// </summary>
        internal SyncSerializationFormat ResponseSerializationFormat { get; set; }

        /// <summary>
        /// Contains the mapping between the entity id and the tempId. This is used
        /// when writing upload responses for inserts, conflicts and errors.
        /// </summary>
        internal Dictionary<string, string> IdToTempIdMapping { get; set; }

        #endregion

        internal Request(RequestCommand requestCommand, 
                        HttpContextServiceHost serviceHost, 
                        Dictionary<CommandParamType, object> commandParams, 
                        byte[] blob,
                        List<IOfflineEntity> entities, 
                        SyncSerializationFormat responseSerializationFormat)
        {
            IdToTempIdMapping = new Dictionary<string, string>();

            RequestCommand = requestCommand;
            ServiceHost = serviceHost;
            CommandParams = commandParams;
            SyncBlob = blob;
            ResponseSerializationFormat = responseSerializationFormat;

            if (null != entities && requestCommand != RequestCommand.UploadChanges)
            {
                throw SyncServiceException.CreateBadRequestError(Strings.EntitiesOnlyAllowedForUploadChangesRequest);
            }

            EntityList = entities;
        }
    }
}
