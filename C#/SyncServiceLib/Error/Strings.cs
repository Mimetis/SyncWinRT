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

namespace Microsoft.Synchronization.Services
{
    internal static class Strings
    {
        internal const string TableGlobalNameCannotByEmpty = "The TableGlobalName property cannot be null or empty in SyncEntityType attribute.";
        internal const string TableLocalNameCannotByEmpty = "The TableLocalName property cannot be null or empty in SyncEntityType attribute.";
        internal const string DuplicateGlobalTableName = "The global table name '{0}' already exists in the dictionary. Two tables cannot have the same global name.";
        internal const string RowExceedsConfiguredBatchSize = "The configured batch size {0} KB is too low for a row in table {1} which is of size {2} bytes. Please increase the value passed to SetDownloadBatchSize in the InitializeService method.";
        internal const string UnableToParseKeyValueForProperty = "Error in parsing the value of the key {0} in URI {1}";
        internal const string BadRequestKeyNotFoundInResource = "The key '{0}' specified in the URI '{1}' does not belong to the entity type '{2}'";
        internal const string BadRequestKeyCountMismatch = "The number of keys specified in the URI '{0}' does not match the number of key properties for the entity type '{1}'";
        internal const string TombstoneEntityHasNoId = "A tombstone entity sent in the request by the client does not have the Id property set.";
        internal const string EntityIdFormatIsIncorrect = "The Id property of the entity is not in the correct format. EntityId: '{0}'";
        internal const string MultipleEntriesWithSamePrimaryKeyInIncomingRequest = "Error occurred writing output response. Multiple entries have the same primary key.";
        internal const string ConflictEntityMissingInIncomingRequest = "Error occurred writing output response. Upload operation resulted in a conflict entity that is not present in the request payload.";
        internal const string ErrorEntityMissingInIncomingRequest = "Error occurred writing output response. Upload operation resulted in an error entity that is not present in the request payload.";
        internal const string BothIdAndTempIdAreMissing = "Entity must have a value for either Id or tempId.";
        internal const string InvalidUrlFormat = "URL format is invalid. Cannot have more than 2 relative segments after the service (.svc) file name.";
        internal const string InvalidHttpMethodForSyncScopesRequest = "Unsupported HTTP method. Please use GET for GetScopes request.";

        internal const string InvalidHttpMethodForScopeMetadataRequest =
            "Unsupported HTTP method. Please use GET for scope metadata request.";

        internal const string UnsupportedHttpMethod = "Unsupported HTTP method. Only GET and POST are supported.";
        internal const string InvalidHttpMethodForUploadChangesRequest =
            "Unsupported HTTP method. Please use POST for UploadChanges request.";

        internal const string DuplicateFilterParameter = "Duplicate filter parameter";
        internal const string BadRequestPayload = "Invalid request payload";
        internal const string UnsupportedContentType = "Unsupported content type";
        internal const string NoContentTypeInHeader = "No content type specified";
        internal const string UnsupportedAcceptHeaderValue = "Unsupported Accept header";
        internal const string UnsupportedConflictResolutionPolicy = "Unsupported conflict resolution policy";
        internal const string InvalidBatchSpoolDirectory = "Batch spool directory does not exist.";
        internal const string SyncScopeNotSupported = "SyncScope not supported.";
        internal const string EntitiesOnlyAllowedForUploadChangesRequest = "Entities can only be used for UploadChange command";
        internal const string TemplateClassNotMarkedWithSyncScopeAttribute = "Template class must be marked with [SyncScope] attribute";
        internal const string ConnectionStringNotSet = "ServerConnectionString not set.";
        internal const string NoScopesVisible = "No scopes are visible";
        internal const string ErrorInServiceInitializeUserCodeMethod = "Invalid initialize client method";
        internal const string NullWebOperationContext = "WebOperationContext.Current is null for incoming request";
        internal const string DuplicateParametersInRequestUri = "Syntax error in Request URI (duplicate parameters)";
        internal const string MissingScopeNameInRequest = "ScopeName missing";
        internal const string MissingOperationNameinRequest = "Missing Operation";
        internal const string InvalidOperationName = "Invalid Operation";
        internal const string NoValidTypeFoundForSync = "No valid types found for sync.";

        internal const string InitializeServiceMethodNotImplemented =
            "Service initialization method 'InitializeService' is not defined by the user.";

        internal const string NoScopeOrTemplateFound = "No scope or template found on server.";

        internal const string InternalServerError = "Internal Server Error";
    }
}
