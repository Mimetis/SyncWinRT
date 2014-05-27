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
#if CLIENT
using Microsoft.Synchronization.ClientServices;
#endif
namespace Microsoft.Synchronization.Services.Formatters
{
    static class FormatterConstants
    {
        public const string NullableTypeName = "Nullable`1";
        public const string MoreChangesAvailableText = "moreChangesAvailable";
        public const string ServerBlobText = "serverBlob";
        public const string PluralizeEntityNameFormat = "{0}s";
        public const string SyncConlflictElementName = "syncConflict";
        public const string SyncErrorElementName = "syncError";
        public const string ConflictEntryElementName = "conflictingChange";
        public const string ErrorEntryElementName = "changeInError";
        public const string ErrorDescriptionElementName = "errorDescription";
        public const string ConflictResolutionElementName = "conflictResolution";
        public const string IsDeletedElementName = "isDeleted";
        public const string IsConflictResolvedElementName = "isResolved";
        public const string TempIdElementName = "tempId";
        public const string EtagElementName = "etag";
        public const string EditUriElementName = "edituri";
        public const string SingleQuoteString = "'";
        public const string LeftBracketString = "(";
        public const string RightBracketString = ")";

        #region ATOM constants
        public const string ApplicationXmlContentType = "application/xml";
        public const string PropertiesElementName = "properties";
        public static XNamespace AtomNamespaceUri = XNamespace.Get("http://www.w3.org/2005/Atom");

        public static XNamespace XmlNamespace = XNamespace.Get("http://www.w3.org/2000/xmlns/");
        public const string SyncNsPrefix = "sync";
        public static XNamespace SyncNamespace = XNamespace.Get("http://odata.org/sync/v1");

        public const string EdmxNsPrefix = "edmx";
        public static XNamespace EdmxNamespace = XNamespace.Get("http://schemas.microsoft.com/ado/2007/06/edmx");

        public const string ODataMetadataNsPrefix = "m";
        public static XNamespace ODataMetadataNamespace = XNamespace.Get("http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");

        public const string ODataDataNsPrefix = "d";
        public static XNamespace ODataDataNamespace = XNamespace.Get("http://schemas.microsoft.com/ado/2007/08/dataservices");
        public static XNamespace ODataSchemaNamespace = XNamespace.Get("http://schemas.microsoft.com/ado/2007/08/dataservices/schema");

        public const string AtomDeletedEntryPrefix = "at";
        public static XNamespace AtomDeletedEntryNamespace = XNamespace.Get("http://purl.org/atompub/tombstones/1.0");

        public const string AtomPubFeedElementName = "feed";
        public const string AtomPubEntryElementName = "entry";
        public const string AtomPubTitleElementName = "title";
        public const string AtomPubIdElementName = "id";
        public const string AtomPubContentElementName = "content";
        public const string AtomPubCategoryElementName = "category";
        public const string AtomPubUpdatedElementName = "updated";
        public const string AtomPubLinkElementName = "link";
        public const string AtomPubTermAttrName = "term";
        public const string AtomPubSchemaAttrName = "schema";
        public const string AtomPubRelAttrName = "rel";
        public const string AtomPubHrefAttrName = "href";
        public const string AtomPubXmlNsPrefix = "xmlns";
        public const string AtomPubTypeElementName = "type";
        public const string AtomPubIsNullElementName = "null";
        public const string AtomPubAuthorElementName = "author";
        public const string AtomPubNameElementName = "name";
        public const string AtomPubEditLinkAttributeName = "edit";
        public const string AtomDeletedEntryElementName = "deleted-entry";
        public const string AtomReferenceElementName = "ref";
        public static XNamespace AtomXmlNamespace = XNamespace.Get("http://www.w3.org/2005/Atom");

        public const string AtomDateTimeOffsetLexicalRepresentation = "yyyy-MM-ddTHH:mm:ss.fffffffzzz";
        public const string AtomDateTimeLexicalRepresentation = "yyyy-MM-ddTHH:mm:ss.fffffff";
        #endregion

        #region JSON Constants
        public const string JsonDocumentElementName = "root";
        public const string JsonRootElementName = "d";
        public const string JsonTypeAttributeName = "type";
        public const string JsonSyncMetadataElementName = "__sync";
        public const string JsonSyncConflictElementName = "__syncConflict";
        public const string JsonSyncErrorElementName = "__syncError";
        public const string JsonSyncEntryMetadataElementName = "__metadata";
        public const string JsonSyncResultsElementName = "results";
        public const string JsonSyncEntryTypeElementName = "type";
        public const string JsonSyncEntryUriElementName = "uri";
        public static XNamespace JsonNamespace = XNamespace.Get("http://tempuri.org");

        public const string JsonDateTimeFormat = @"/Date({0})/";
        public const string JsonTimeFormat = @"time'{0}'";
        public const string JsonDateTimeOffsetFormat = @"datetimeoffset'{0}'";
        public const string JsonDateTimeOffsetLexicalRepresentation = "yyyy-MM-ddTHH:mm:ss.fffffffzzz";
        public const string JsonDateTimeLexicalRepresentation = "yyyy-MM-ddTHH:mm:ss.fffffff";
        public static DateTime JsonDateTimeStartTime = new DateTime(1970, 1, 1); // Midnight of Jan 1 , 1970 as per OData Json standard
        public static long JsonNanoToMilliSecondsFactor = 10000;

        #endregion

        #region Types
        public static readonly Type DateTimeType = typeof(DateTime);
        public static readonly Type DateTimeOffsetType = typeof(DateTimeOffset);
        public static readonly Type TimeSpanType = typeof(TimeSpan);
        public static readonly Type ByteArrayType = typeof(byte[]);
        public static readonly Type BoolType = typeof(bool);
        public static readonly Type FloatType = typeof(float);
        public static readonly Type DecimalType = typeof(decimal);
        public static readonly Type GuidType = typeof(Guid);
        public static readonly Type StringType = typeof(string);
        public static readonly Type CharType = typeof(char);
        public static readonly Type NullableType = typeof(Nullable<>);
        public static readonly Type SyncConflictResolutionType = typeof(SyncConflictResolution);
        #endregion
    }
}
