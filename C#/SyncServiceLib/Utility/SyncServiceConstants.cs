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
    ///<summary>
    /// This class contains constants used by the service code.
    ///</summary>
    internal static class SyncServiceConstants
    {
        internal const string SYNC_SCOPES_URL_SEGMENT = "$syncscopes";
        internal const string SYNC_SCOPE_METADATA_URL_SEGMENT = "$metadata";
        internal const string SYNC_OPERATION_UPLOAD_CHANGES = "uploadchanges";
        internal const string SYNC_OPERATION_DOWNLOAD_CHANGES = "downloadchanges";
        internal const string SYNC_OPERATION_SCOPE_METADATA = "$metadata";
        internal const string SYNC_SERVERBLOB_QUERYKEY = "$syncServerBlob";
        internal const string SYNC_SERVERBLOB_HEADERKEY = "SyncServerBlob";
        internal const string HTTP_VERB_GET = "GET";
        internal const string HTTP_VERB_POST = "POST";

        internal const string SYNC_SERVICE_VERSION_KEY = "SyncServiceVersion";
        internal const string SYNC_SERVICE_VERSION_VALUE = "1.0";

        internal const string CONTENT_TYPE_HTML = "text/html; charset=UTF-8";

        internal const string SYNC_ENTITY_TYPE_TABLE_GLOBAL_NAME = "TableGlobalName";
        internal const string SYNC_ENTITY_TYPE_TABLE_LOCAL_NAME = "TableLocalName";

        internal static readonly Type SYNC_REQUEST_INTERCEPTOR_TYPE = typeof(SyncRequestInterceptorAttribute);
        internal static readonly Type SYNC_RESPONSE_INTERCEPTOR_TYPE = typeof(SyncResponseInterceptorAttribute);
        internal static readonly Type SYNC_CONFLICT_INTERCEPTOR_TYPE = typeof(SyncConflictInterceptorAttribute);
        internal static readonly Type SYNC_OPERATIONCONTEXT_TYPE = typeof(SyncOperationContext);
        internal static readonly Type SYNC_CONFLICT_RESOLUTION_TYPE = typeof(SyncConflictResolution);
        internal static readonly Type SYNC_CONFLICT_CONTEXT_TYPE = typeof(SyncConflictContext);
        internal static readonly Type VOID_TYPE = typeof(void);
        internal static readonly Type IOFFLINEENTITY_BYREFTYPE = typeof(IOfflineEntity).MakeByRefType();
        internal static readonly Type IOFFLINEENTITY_TYPE = typeof(IOfflineEntity);
        internal static readonly Type SYNC_ENTITY_PROPERTY_NULLABLE_ATTRIBUTE_TYPE = typeof(SyncEntityPropertyIsNullableAttribute);

        internal const string SYNC_INCORRECT_INTERCEPTOR_SIGNATURE = "Method '{0}' on type '{1}' is marked as '{2}' but is not of valid format. " + 
            "Valid format is '{3}'";
        internal const string SYNC_REQUEST_INTERCEPTOR_FORMAT = "public void MethodName(SyncOperationContext context)";
        internal const string SYNC_CONFLICT_INTERCEPTOR_FORMAT = "public SyncConflictResolution MethodName(SyncConflictContext context, out IOfflineEntity mergedEntity)";
    }
}
