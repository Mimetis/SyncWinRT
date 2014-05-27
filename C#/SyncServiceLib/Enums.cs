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

using System;
namespace Microsoft.Synchronization.Services
{
    /// <summary>
    /// Represents the options for the conflict resolution policy to use for synchronization.
    /// </summary>
    public enum ConflictResolutionPolicy
    {
        /// <summary>
        /// Indicates that the change on the server wins in case of a conflict.
        /// </summary>
        ServerWins,

        /// <summary>
        /// Indicates that the change sent by the client wins in case of a conflict.
        /// </summary>
        ClientWins
    }

    /// <summary>
    /// Represents the serialization format to be used for the response.
    /// </summary>
    public enum SyncSerializationFormat
    {
        /// <summary>
        /// Indicates that OData AtomPub is the serialization format.
        /// </summary>
        ODataAtom,

        /// <summary>
        /// Indicates that OData JSON is the serialization format.
        /// </summary>
        ODataJson
    }

    /// <summary>
    /// Represents the type of request as obtained from the incoming request details.
    /// </summary>
    internal enum RequestCommand
    {
        /// <summary>
        /// Indicates an upload changes request type.
        /// </summary>
        UploadChanges,

        /// <summary>
        /// Indicates a download changes request type.
        /// </summary>
        DownloadChanges,

        /// <summary>
        /// Indicates a request type to enumerate scopes.
        /// </summary>
        SyncScopes,

        /// <summary>
        /// Indicates a request type to get scope metadata.
        /// </summary>
        ScopeMetadata
    }
    
    /// <summary>
    /// Represents the SyncOperations for which a SyncInterceptorAttribute applies to.
    /// </summary>
    [Flags]
    public enum SyncOperations : int
    {
        /// <summary>
        /// Represents a Sync DownloadChanges request/response operation.
        /// </summary>
        Download = 1,

        /// <summary>
        /// Represents a Sync UploadChanges request/response operation.
        /// </summary>
        Upload = 2,

        /// <summary>
        /// Represents a Sync DownloadChanges/UploadChanges request/response operation.
        /// </summary>
        All = Download | Upload
    }
    
    /// <summary>
    /// Represents the valid values for the command parameters that are sent as a part of the request.
    /// </summary>
    internal enum CommandParamType
    {
        /// <summary>
        /// ScopeName that is sent as a part of the request.
        /// </summary>
        ScopeName,

        /// <summary>
        /// Filter parameters sent as a part of the request. This is used for provisioning a new scope.
        /// </summary>
        FilterParameters
    }
}
