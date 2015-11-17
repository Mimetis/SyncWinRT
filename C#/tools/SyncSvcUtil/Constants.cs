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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.Synchronization.ClientServices
{
    /// <summary>
    /// Constant string definitions
    /// </summary>
    internal class Constants
    {
        public const string SyncConfigurationSectionName = "SyncConfiguration";
        public static string AssemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

        // Code generation constants

        public const string EntityGetCollectionFormat = "{0}Collection";
        public const string EntityFieldNameFormat = "_{0}s";

        #region service coddgen constants

        public const string ServiceOuterEntityNameFormat = "{0}OfflineEntities";
        public const string ServiceCustomSyncServiceTypeFormat = "{0}SyncService";
        public const string ServiceSyncServiceBaseTypeFormat = "Microsoft.Synchronization.Services.SyncService";
        public const string ServiceSyncScopeAttributeDefinition = "Microsoft.Synchronization.Services.SyncScopeAttribute";
        public const string ServiceIOfflineEntity = "Microsoft.Synchronization.Services.IOfflineEntity";
        public const string ServiceSyncEntityAttribute = "Microsoft.Synchronization.Services.SyncEntityTypeAttribute";
        public const string ServiceSyncColumnMappingAttribute = "Microsoft.Synchronization.Services.SyncEntityPropertyMappingAttribute";
        public const string ServiceISyncSvcConfigurationType = "Microsoft.Synchronization.Services.ISyncServiceConfiguration";
        public static string[] ServiceSyncSvcCommentLines = new string[] {
            "TODO: MUST set these values",
            "config.ServerConnectionString = \"connection string here\";",
            "config.SetEnableScope(\"scope name goes here\");", 
            string.Empty,
            string.Empty,
            "TODO: Optional.", 
            "config.SetDefaultSyncSerializationFormat(Microsoft.Synchronization.Services.SyncSerializationFormat.ODataJson);" ,
            "config.SetConflictResolutionPolicy(Microsoft.Synchronization.Services.ConflictResolutionPolicy.ServerWins);"
        };
        public static string[] ServiceSyncSvcPreSchemaCommentLines = new string[] {
            string.Empty,
            string.Empty,
            "Note: The scope has specified a non dbo schema for creating sync tables. Removing this line will cause requests from new clients to fail."
        };
        public static string ServiceSyncSvcInitializeService_SetSchemaMethodFormat = "SetSyncObjectSchema";
        public const string ServiceSyncServicSVCFileContents = "<%@ ServiceHost Language=\"{0}\" Service=\"{1}.{2}\" CodeBehind=\"{3}\" Factory=\"Microsoft.Synchronization.Services.SyncServiceHostFactory, Microsoft.Synchronization.Services, Version={4}, Culture=neutral\" %>";
        public const string ServiceOfflineEntityMetadataTypeName = "Microsoft.Synchronization.Services.OfflineEntityMetadata";
        public const string EntityPropertyNullableAttributeType = "Microsoft.Synchronization.Services.SyncEntityPropertyIsNullableAttribute";

        #endregion

        #region ClientSpecific constants
        
        public const string ClientContextAddMethodFormat = "Add{0}";
        public const string ClientContextDeleteMethodFormat = "Delete{0}";
        public const string ClientContextClassNameFormat = "{0}OfflineContext";
        public const string ClientAddMethodBodyFormat = "AddItem";
        public const string ClientDeleteMethodBodyFormat = "DeleteItem";
        public const string ClientGetCollectionMethodBodyFormat = "GetCollection";
        public const string ClientContextBaseType = "Microsoft.Synchronization.ClientServices.IsolatedStorage.IsolatedStorageOfflineContext";
        public const string SQLiteContextBaseType = "Microsoft.Synchronization.ClientServices.SQLite.SQLiteContext";
        public const string ClientSchemaBaseType = "Microsoft.Synchronization.ClientServices.IsolatedStorage.IsolatedStorageSchema";
        public const string SQLiteSchemaBaseType = "Microsoft.Synchronization.ClientServices.Common.OfflineSchema";
        public const string ClientIsolatedStoreBaseCtor = "SyncScopeName, cachePath, serviceUri, cookieContainer";
        public const string ClientIsolatedStoreCallCtorWithUri = "SyncScopeName, SyncScopeUri";
        public const string ClientIsolatedStoreEncryptedBaseCtor = "SyncScopeName, cachePath, serviceUri, symmAlgorithm";
        public const string ClientIsolatedStoreCallEncryptedCtorWithUri = "SyncScopeName, SyncScopeUri, symmAlgorithm";
        public const string ClientIsolatedStoreOfflineEntity = "Microsoft.Synchronization.ClientServices.IsolatedStorage.IsolatedStorageOfflineEntity";
        public const string SQLiteOfflineEntity = "Microsoft.Synchronization.ClientServices.SQLite.SQLiteOfflineEntity";
        public const string ClientIOfflineEntity = "Microsoft.Synchronization.ClientServices.IOfflineEntity";
        public const string ClientCallOnPropertyChanging = "OnPropertyChanging";
        public const string ClientCallOnPropertyChanged = "OnPropertyChanged";
        public const string ClientKeyAtributeType = "System.ComponentModel.DataAnnotations.KeyAttribute";
        public const string SQLitePrimaryKeyAtributeType = "Microsoft.Synchronization.ClientServices.SQLite.PrimaryKey";
        public const string SQLiteMaxLengthAttribute = "Microsoft.Synchronization.ClientServices.SQLite.MaxLength";
        public const string SQLiteAutoIncrementAttribute = "Microsoft.Synchronization.ClientServices.SQLite.AutoIncrement";
        public const string ClientIsolatedStoreGetSchemaMethodName = "GetSchema";
        public const string ClientOfflineEntityMetadataTypeName = "Microsoft.Synchronization.ClientServices.OfflineEntityMetadata";
        public const string ClientCachePathArgName = "cachePath";
        public const string ClientServiceUriArgName = "serviceUri";
        public const string ClientServiceCookieContainerArgName = "cookieContainer";
        public const string ClientServiceCookieContainerArgAttrOptional = "Optional";
        public const string ClientServiceCookieContainerArgAttrDefaultParam = "DefaultParameterValue";
        public const string ClientSymmetricAlgorithmArgName = "symmAlgorithm";
        public const string SymmetricAlgorithmTypeName = "System.Security.Cryptography.SymmetricAlgorithm";
        public const string ClientEntityVariableName = "entity";
        public const string StringifyProperty = "\"{0}\"";
        #endregion

        public const string ServiceMetadataFieldName = "_serviceMetadata";
        public const string ServiceMetadataPropertyName = "ServiceMetadata";
        public const string OfflineEntityBaseTypeNameFormat = "{0}OfflineEntityBase";

        #region OData CSDL service metadata document constants
        public static XNamespace EdmxNamespace = XNamespace.Get("http://schemas.microsoft.com/ado/2007/06/edmx");
        public static XNamespace EdmNamespace = XNamespace.Get("http://schemas.microsoft.com/ado/2007/05/edm");
        public static XNamespace AppNamespace = XNamespace.Get("http://www.w3.org/2007/app");

        public static XName SyncScopeServicesElement = AppNamespace + "service";
        public static XName SyncScopeWorkspaceElement = AppNamespace + "workspace";
        public static XName SyncScopeWorkspaceCollectionElement = AppNamespace + "collection";
        public static XName SyncScopeAtomTitleElement = AppNamespace + "title";
        public static XName SyncScopeWorkspaceCollectionHrefAttribute = XName.Get("href");
        
        public static XName SyncScopeEdmxElement = EdmxNamespace + "Edmx";
        public static XName SyncScopeDataServicesElement = EdmxNamespace + "DataServices";
        public static XName SyncScopeSchemaElement = EdmNamespace + "Schema";
        public static XName SyncScopeNamespaceAttribute = EdmNamespace + "Namespace";
        public static XName SyncScopeEntityTypeElement = EdmNamespace + "EntityType";
        public static XName SyncScopeEntityTypeKeyElement = EdmNamespace + "Key";
        public static XName SyncScopeEntityTypeKeyRefElement = EdmNamespace + "PropertyRef"; 
        public static XName SyncScopeEntityTypeNameAttribute = XName.Get("Name");
        public static XName SyncScopeEntityTypePropertyElement = EdmNamespace + "Property"; 
        public static XName SyncScopeEntityTypeTypeAttribute = XName.Get("Type");
        public static XName SyncScopeEntityTypeNullableAttribute = XName.Get("Nullable");

        #endregion
    }
}
