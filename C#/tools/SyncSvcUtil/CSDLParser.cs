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
using System.Net;
using System.Xml.Linq;
using Microsoft.Synchronization.Data;

namespace Microsoft.Synchronization.ClientServices
{
    /// <summary>
    /// Exception while parsing CSDL
    /// </summary>
    [Serializable]
    public class CsdlException : Exception 
    {
        /// <summary>
        /// Public constructor
        /// </summary>
        /// <param name="message">Exception message</param>
        public CsdlException(string message) : base(message) {}            
    }
    
    /// <summary>
    /// Class used to read and parse a CSDL and SyncScopes service document
    ///  
    /// This is the sample output from a $syncScopes document
    /// <service xml:base="http://services.odata.org/OData/OData.svc/" xmlns:atom="http://www.w3.org/2005/Atom" xmlns:app="http://www.w3.org/2007/app" xmlns="http://www.w3.org/2007/app">
    ///   <workspace>
    ///              <atom:title>Default</atom:title> 
    ///       <collection href="Products">
    ///              <atom:title>Products</atom:title> 
    ///          </collection>
    ///       <collection href="Categories">
    ///              <atom:title>Categories</atom:title> 
    ///          </collection>
    ///       <collection href="Suppliers">
    ///              <atom:title>Suppliers</atom:title> 
    ///          </collection>
    ///     </workspace>
    ///  </service>
    ///  
    /// 
    /// This is the sample output from a $metadata document
    ///    <edmx:Edmx Version="1.0" xmlns:edmx="http://schemas.microsoft.com/ado/2007/06/edmx">
    ///        <edmx:DataServices xmlns:m="http://schemas.microsoft.com/ado/2007/08/dataservices/metadata" m:DataServiceVersion="2.0">
    ///            <Schema Namespace="ODataDemo" xmlns:d="http://schemas.microsoft.com/ado/2007/08/dataservices" xmlns:m="http://schemas.microsoft.com/ado/2007/08/dataservices/metadata" xmlns="http://schemas.microsoft.com/ado/2007/05/edm">
    ///                <EntityType Name="Product">
    ///                    <Key>
    ///                        <PropertyRef Name="ID" /> 
    ///                    </Key>
    ///                    <Property Name="ID" Type="Edm.Int32" Nullable="false" /> 
    ///                    <Property Name="Name" Type="Edm.String" Nullable="true" m:FC_TargetPath="SyndicationTitle" m:FC_ContentKind="text" m:FC_KeepInContent="false" /> 
    ///                    <Property Name="Description" Type="Edm.String" Nullable="true" m:FC_TargetPath="SyndicationSummary" m:FC_ContentKind="text" m:FC_KeepInContent="false" /> 
    ///                    <Property Name="ReleaseDate" Type="Edm.DateTime" Nullable="false" /> 
    ///                    <Property Name="DiscontinuedDate" Type="Edm.DateTime" Nullable="true" /> 
    ///                    <Property Name="Rating" Type="Edm.Int32" Nullable="false" /> 
    ///                    <Property Name="Price" Type="Edm.Decimal" Nullable="false" /> 
    ///                </EntityType>
    ///             </Schema>
    ///         </edmx:DataServices>
    ///   </edmx:Edmx>
    /// </summary>
   
    internal static class CSDLParser
    {
        public static DbSyncScopeDescription GetDescriptionFromUri(ArgsParser parser, string uriString, out string serviceUri)
        {
            // Issue the request.
            WebClient request = new WebClient();
            SyncSvcUtil.Log("Trying to connect to Uri '{0}' to check for SyncScopeSchema document", uriString);

            string content = request.DownloadString(uriString);

            // Check to see if its a SyncScopes <services> document or an SyncScopeSchema <edmx> document.
            if (parser.UseVerbose)
            {
                SyncSvcUtil.Log("Download succeeded. Checking to see if its a SyncScope <Service> document or an SyncScopeSchema <edmx> document.");
                SyncSvcUtil.Log("Downloaded document content:\n {0}", content);
            }

            SyncSvcUtil.Log("Parsing downloaded document.", uriString);

            XDocument document = XDocument.Parse(content);
            if (document == null)
            {
                throw new CsdlException("Downloaded content is not a valid XML document.");
            }

            if (document.Root.Name.Equals(Constants.SyncScopeServicesElement))
            {
                if (parser.UseVerbose)
                {
                    SyncSvcUtil.Log("Found a <service> document. Checking for SyncScopes workspace.");
                }
                XElement workspaceElement = document.Root.Element(Constants.SyncScopeWorkspaceElement);
                if (workspaceElement == null)
                {
                    throw new CsdlException("Remote SyncScope services document did not contain a <workspace> element.");
                }

                // Look for <collection> element
                XElement[] collectionElements = workspaceElement.Elements(Constants.SyncScopeWorkspaceCollectionElement).ToArray();
                if (collectionElements.Length == 0)
                {
                    throw new CsdlException("Remote SyncScope services document did not contain a <collection> element.");
                }
                else if (collectionElements.Length > 1)
                {
                    SyncSvcUtil.Log("Multiple SyncScopes were found in the <service> document. Please specify the correct Url.");
                    foreach (XElement elem in collectionElements)
                    {
                        XAttribute hrefAttr = elem.Attribute(Constants.SyncScopeWorkspaceCollectionHrefAttribute);
                        SyncSvcUtil.Log("\t\t{0} - Uri: {1}{2}{0}/$metadata",
                        hrefAttr.Value,
                        parser.CSDLUrl,
                        parser.CSDLUrl.EndsWith("/") ? string.Empty : "/");
                    }
                    throw new CsdlException("Multiple SyncScopes found.");
                }
                else
                {
                    // We have exactly one SyncScope. Download the schema for that
                    XAttribute hrefAttr = collectionElements[0].Attribute(Constants.SyncScopeWorkspaceCollectionHrefAttribute);
                    if (hrefAttr == null)
                    {
                        throw new CsdlException("No Href attribute was found in the <collection> element.");
                    }

                    // Ensure the href param is not empty as this is the scopeName
                    if (string.IsNullOrEmpty(hrefAttr.Value))
                    {
                        throw new CsdlException(string.Format("Href attribute in <collection> must have a non empty string.\n Content: {0}", collectionElements[0].ToString()));
                    }

                    // Look for and remove $syncScopes
                    string origUrl = parser.CSDLUrl;
                    if (origUrl.EndsWith("$syncscopes", StringComparison.InvariantCultureIgnoreCase))
                    {
                        origUrl = origUrl.Substring(0, origUrl.LastIndexOf("/"));
                    }
                    
                    uriString = string.Format("{0}{1}{2}/$metadata",
                        origUrl,
                        origUrl.EndsWith("/") ? string.Empty : "/",
                        hrefAttr.Value);

                    return CSDLParser.GetDescriptionFromUri(parser, uriString, out serviceUri);
                }

            }
            else if (document.Root.Name.Equals(Constants.SyncScopeEdmxElement))
            {
                // Set the service URI and remove $metadata token from it.                
                //Remove the / at the end if present.
                serviceUri = (uriString.EndsWith("/")) ? uriString.Substring(0, uriString.Length - 1) : uriString;

                //The service will render the schema only if there is a $metadata at the end in the Uri.                
                serviceUri = serviceUri.Substring(0, serviceUri.Length - "/$metadata".Length);

                //Remove the scope name
                serviceUri = serviceUri.Substring(0, serviceUri.LastIndexOf("/") + 1);
               
                return ParseCSDLDocument(parser, uriString, document);
            }
            else
            {
                throw new CsdlException(string.Format("Downloaded XML content is not a valid <service> document. \nDocument Content: {0}", content));
            }
        }

        private static DbSyncScopeDescription ParseCSDLDocument(ArgsParser parser, string uriString, XDocument document)
        {
            DbSyncScopeDescription scopeDescription = null;
            Uri uri = new Uri(uriString);
            // Assumption is that for OData Sync metadata document, the URI is of format http://foo/snc.svc/scopename/$metadata.
            // In this case we are looking for the last but one segment.
            string scopeName = uri.Segments[uri.Segments.Length - 2];
            if (scopeName.EndsWith("/"))
            {
                scopeName = scopeName.Substring(0, scopeName.Length - 1);
            }

            if (parser.UseVerbose)
            {
                SyncSvcUtil.Log("Parsed ScopeName as {0}", scopeName);
            }

            // Its an CSDL document
            XElement dataServicesElem = document.Root.Element(Constants.SyncScopeDataServicesElement);
            if (dataServicesElem == null)
            {
                throw new CsdlException("No <DataServices> element found in the <edmx> document.");
            }
            XElement schemaElement = dataServicesElem.Element(Constants.SyncScopeSchemaElement);
            if (schemaElement == null)
            {
                throw new CsdlException("No <Schema> element found in the <DataServices> document.");
            }

            scopeDescription = new DbSyncScopeDescription(scopeName);
            // Loop over each <EntityType> element and add it as a DbSyncTableDescription
            foreach (XElement entity in schemaElement.Elements(Constants.SyncScopeEntityTypeElement))
            {
                XAttribute nameAttr = entity.Attribute(Constants.SyncScopeEntityTypeNameAttribute);
                if (nameAttr == null)
                {
                    throw new CsdlException("<EntityType> has no Name attribute. \n" + entity.ToString());
                }
                // Parse each entity and create a DbSyncTableDescription
                DbSyncTableDescription table = new DbSyncTableDescription(nameAttr.Value);

                // Look for <Key> element
                XElement keyElem = entity.Element(Constants.SyncScopeEntityTypeKeyElement);
                if (keyElem == null)
                {
                    throw new CsdlException("<EntityType> has no <Key> elements defined. \n" + entity.ToString());
                }

                List<string> keyNames = new List<string>();
                // Loop over each <PropertyRef> element and add it to the list for lookup
                foreach (XElement prop in keyElem.Elements(Constants.SyncScopeEntityTypeKeyRefElement))
                {
                    XAttribute keyName = prop.Attribute(Constants.SyncScopeEntityTypeNameAttribute);
                    if (keyName != null)
                    {
                        keyNames.Add(keyName.Value);
                    }
                }

                // Loop over each <Property> element and add it as a DbSyncColumnDescription
                foreach (XElement field in entity.Elements(Constants.SyncScopeEntityTypePropertyElement))
                {
                    // Read Property name
                    XAttribute fieldName = field.Attribute(Constants.SyncScopeEntityTypeNameAttribute);
                    if (fieldName == null)
                    {
                        throw new CsdlException("<Property> has no Name attribute. \n" + field.ToString());
                    }

                    // Read Property Edm type
                    XAttribute fieldType = field.Attribute(Constants.SyncScopeEntityTypeTypeAttribute);
                    if (fieldType == null)
                    {
                        throw new CsdlException("<Property> has no Type attribute. \n" + field.ToString());
                    }

                    // Read Property Nullable attribute
                    XAttribute fieldNullable = field.Attribute(Constants.SyncScopeEntityTypeNullableAttribute);

                    DbSyncColumnDescription column = new DbSyncColumnDescription(fieldName.Value, GetSqlTypeForEdm(fieldType.Value));
                    if (fieldNullable != null && bool.Parse(fieldNullable.Value))
                    {
                        column.IsNullable = true;
                    }
                    column.IsPrimaryKey = keyNames.Contains(fieldName.Value);
                    table.Columns.Add(column);
                }

                scopeDescription.Tables.Add(table);
            }
            return scopeDescription;
        }

        /// <summary>
        /// This mapping between EDM type and .NET type and its corresponding SQLDbType comes from the following link
        /// http://www.odata.org/developers/protocols/overview#AbstractTypeSystem
        /// </summary>
        /// <param name="edmType">Input EDM type</param>
        /// <returns>SqlDbType</returns>
        private static string GetSqlTypeForEdm(string edmType)
        {
            switch (edmType.ToLowerInvariant())
            {
                case "edm.binary":
                    return "varbinary";
                case "edm.boolean":
                    return "bit";
                case "edm.byte":
                    return "tinyint";
                case "edm.datetime":
                    return "datetime";
                case "edm.decimal":
                    return "decimal";
                case "edm.double":
                case "edm.single":
                    return "float";
                case "edm.guid":
                    return "uniqueidentifier";
                case "edm.int16":
                    return "smallint";
                case "edm.int32":
                    return "int";
                case "edm.int64":
                    return "bigint";
                case "edm.sbyte":
                    return "tinyint";
                case "edm.string":
                    return "nvarchar";
                case "edm.time":
                    return "time";
                case "edm.datetimeoffset":
                    return "datetimeoffset";
                default:
                    throw new CsdlException("Unsupported Edm type " + edmType);
            }
        }
    }
}
