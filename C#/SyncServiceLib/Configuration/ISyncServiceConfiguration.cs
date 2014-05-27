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
    /// Interface that is used to configure service wide policies.
    /// </summary>
    public interface ISyncServiceConfiguration
    {
        /// <summary>
        /// Change the conflict resolution policy. The default value is ClientWins.
        /// </summary>
        /// <param name="policy">The new conflict resolution policy</param>
        void SetConflictResolutionPolicy(ConflictResolutionPolicy policy);

        /// <summary>
        /// Change the serialization format. The default value is ODataAtom.
        /// </summary>
        /// <param name="serializationFormat">serialization format</param>
        void SetDefaultSyncSerializationFormat(SyncSerializationFormat serializationFormat);

        /// <summary>
        /// Enable scopes. No scopes are enabled by default.
        /// </summary>
        /// <param name="scopeName">Scope name to enable.</param>
        void SetEnableScope(string scopeName);

        /// <summary>
        /// Add a new filter parameter configuration. This method can be called during service initialization
        /// and SqlSyncProvider filter parameter definitions can be added to service configuration.
        /// </summary>
        /// <param name="queryStringParam">Name of the querystring parameter. This is parsed when changes are requested.</param>
        /// <param name="tableName">SQL table name</param>
        /// <param name="sqlParameterName">SQL parameter name (has to be exact since its used in query formation)</param>
        /// <param name="typeOfParam">
        /// Indicates the Type of the parameter. This is used to change the value of the query string parameter to the 
        /// type requested by the SqlParameter type.
        /// </param>
        void AddFilterParameterConfiguration(string queryStringParam, string tableName, string sqlParameterName, Type typeOfParam);

        /// <summary>
        /// Set the path where batches will be spooled. The directory must already exist. Default directory is %TEMP%.
        /// </summary>
        /// <param name="directoryPath">Path to the batch spooling directory.</param>
        void SetBatchSpoolDirectory(string directoryPath);

        /// <summary>
        /// Set a download batch size. Batching is disabled by default.
        /// This indicates the size with which data is batched. The size of the actual response will be greater due to the 
        /// OData elements that take extra space.
        /// </summary>
        /// <param name="batchSizeInKB">Download batch size in KB</param>
        void SetDownloadBatchSize(uint batchSizeInKB);

        /// <summary>
        /// Set the schema name under which sync related objects were generated in the SQL database when the database was provisioned.
        /// Note: This setting applies to only objects that were created by the sync framework. The schema name of the individual tables
        /// is included in the TableName property of the SyncEntityTypeAttribute class.
        /// </summary>
        /// <param name="schemaName">Name of the schema under which sync related objects are created.</param>
        void SetSyncObjectSchema(string schemaName);

        /// <summary>
        /// Gets/Sets the server connection string.
        /// </summary>
        string ServerConnectionString { get; set; }

        /// <summary>
        /// Gets/Sets the error level for sync operations. Default value is false.
        /// If true - detailed error information is returned in the response. 
        /// This can be used for debugging.
        /// </summary>
        /// <remarks>For production deployments set this to false.</remarks>
        bool UseVerboseErrors { get; set; }

        /// <summary>
        /// Indicates if batching is enabled on the provider service.
        /// </summary>
        bool IsBatchingEnabled { get;  }

        /// <summary>Enable or disable the diagnostic page served by the $diag URL.</summary>
        bool EnableDiagnosticPage { get; set; }
    }
}
