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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceModel.Channels;
using System.Xml.Linq;
using Microsoft.Synchronization.Services.Formatters;

namespace Microsoft.Synchronization.Services
{
    /// <summary>
    /// Handler for the $metadata request command.
    /// </summary>
    internal class ScopeSchemaRequestProcessor : IRequestProcessor
    {
        #region Private Members

        private static readonly XNamespace _baseEdmNamespace = XNamespace.Get("http://schemas.microsoft.com/ado/2007/05/edm");
        private readonly SyncServiceConfiguration _configuration;

        #endregion

        #region Constructor

        public ScopeSchemaRequestProcessor(SyncServiceConfiguration configuration)
        {
            _configuration = configuration;
        }

        #endregion

        #region IRequestProcessor Implementation

        /// <summary>
        /// Process the $metadata request and return the xml description as per the sync protocol specification.
        /// </summary>
        /// <param name="incomingRequest">incoming request object.</param>
        /// <returns>WCF Message object that contains the output xml.</returns>
        public Message ProcessRequest(Request incomingRequest)
        {
            Debug.Assert(null != _configuration.ScopeNames);
            Debug.Assert(_configuration.ScopeNames.Count > 0);

            XDocument document = GetMetadataDocument();

            return WebUtil.CreateResponseMessage(document);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Get the metadata document as per sync specification for $metadata.
        /// </summary>
        /// <returns></returns>
        private XDocument GetMetadataDocument()
        {
            // We only have 1 scope for now.
            //Note: Currently this is read from the service configuration and * is not allowed anymore for the SetEnableScope method.
            string scopeName = _configuration.ScopeNames[0];

            // Get the type list from the TypeToTableGlobalNameMapping key list.
            List<Type> typeList = _configuration.TypeToTableGlobalNameMapping.Keys.ToList();

            var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));

            // Add Schema node
            // example: 
            //  <Edmx Namespace="http://schemas.microsoft.com/ado/2007/06/edmx">
            //     <DataServices Namespace="http://schemas.microsoft.com/ado/2007/08/dataservices/metadata">
            //        <Schema Namespace="SyncServiceLibUnitTest" 
            //         Alias="Self" 
            //         xmlns="http://schemas.microsoft.com/ado/2009/08/edm" 
            //         xmlns:sync="http://schemas.microsoft.com/sync/2010/03/syncprotocol">

            var edmxNode = new XElement(FormatterConstants.EdmxNamespace + "Edmx",
                                        new XAttribute("Version", "1.0"),
                                        new XAttribute(XNamespace.Xmlns + FormatterConstants.EdmxNsPrefix, FormatterConstants.EdmxNamespace));
            
            var dataservicesNode = new XElement(FormatterConstants.EdmxNamespace + "DataServices",
                                        new XAttribute(FormatterConstants.ODataMetadataNamespace + "DataServiceVersion", "2.0"),
                                        new XAttribute(XNamespace.Xmlns + FormatterConstants.ODataMetadataNsPrefix, FormatterConstants.ODataMetadataNamespace));

            var schemaNode = new XElement("Schema",
                                          new XAttribute(FormatterConstants.AtomPubXmlNsPrefix, _baseEdmNamespace),                          
                                          new XAttribute(XNamespace.Xmlns + FormatterConstants.SyncNsPrefix, FormatterConstants.SyncNamespace),
                                          new XAttribute("Namespace", _configuration.ServiceTypeNamespace),
                                          new XAttribute("Alias", "Self"));

            // Add entitycontainer node
            // example:
            // <EntityContainer Name="scope_name">
            var entityContainerNode = new XElement(_baseEdmNamespace + "EntityContainer",
                                                   new XAttribute("Name", scopeName));

            schemaNode.Add(entityContainerNode);

            // Add each entry as an EntityType node.
            foreach (var type in typeList)
            {
                // Add the type to the entityContainer Node as an EntitySet node
                // example:
                // <EntitySet Name="ScheduleItem" EntityType="SyncServiceLibUnitTest.ScheduleItem" /> 
                var entitySetNode = new XElement(_baseEdmNamespace + "EntitySet",
                                                 new XAttribute("Name", type.Name),
                                                 new XAttribute("EntityType", type.FullName));

                entityContainerNode.Add(entitySetNode);

                // Create entity type node
                // example: <EntityType Name="ScheduleItem">
                var entityType = new XElement(_baseEdmNamespace + "EntityType", new XAttribute("Name", type.Name));

                schemaNode.Add(entityType);

                // Get the keys from the Key attribute applied on the entity classes.
                PropertyInfo[] keyList = ReflectionUtility.GetPrimaryKeysPropertyInfoMapping(type);

                if (keyList.Length > 0)
                {
                    // create <Key> node
                    var keyNode = new XElement(_baseEdmNamespace + "Key");
                    foreach (var key in keyList)
                    {
                        // add PropertyRef nodes
                        // example: <PropertyRef Name="ScheduleItemID" /> 
                        var propertyRefNode = new XElement(_baseEdmNamespace + "PropertyRef", new XAttribute("Name", key.Name));

                        keyNode.Add(propertyRefNode);
                    }

                    entityType.Add(keyNode);
                }

                PropertyInfo[] entityProperties = ReflectionUtility.GetPropertyInfoMapping(type);

                foreach (var property in entityProperties)
                {
                    // Check if the property has a SyncEntityPropertyNullable attribute.
                    // Presence of this attribute indicates that the property is nullable/non-nullable in the underlying data store.
                    // Some data types such as string are nullable in .NET but may or may not be nullable in the data store (such as a 
                    // SQL Server table in a database.)
                    bool isNullable = (0 != property.GetCustomAttributes(
                                                SyncServiceConstants.SYNC_ENTITY_PROPERTY_NULLABLE_ATTRIBUTE_TYPE, false).ToList().Count);

                    // create Property node
                    // example:
                    // <Property Name="ScheduleItemID" Type="Edm.Guid" Nullable="false" /> 
                    var propertyNode = new XElement(_baseEdmNamespace + "Property",
                                                    new XAttribute("Name", property.Name),
                                                    new XAttribute("Type", FormatterUtilities.GetEdmType(property.PropertyType)),
                                                    new XAttribute("Nullable", isNullable));

                    entityType.Add(propertyNode);
                }
            }

            // Add filter parameter information to the document.
            if (_configuration.FilterParameters.Count > 0)
            {
                // Write the scynscopeparameters node.
                // example: <sync:SyncScopeParameters>
                var syncScopeParamsNode = new XElement(FormatterConstants.SyncNamespace + "SyncScopeParameters");

                // We only want to add distinct filter parameters. A single parameter may be applied to many tables internally.
                var distinctFilterParams = new List<string>();
                foreach (var filterParameter in _configuration.FilterParameters)
                {
                    // Continue with the next filter parameter if we already processed the current one.
                    if (distinctFilterParams.Contains(filterParameter.QueryStringKey))
                    {
                        continue;
                    }

                    distinctFilterParams.Add(filterParameter.QueryStringKey);

                    string edmType = FormatterUtilities.GetEdmType(filterParameter.ValueType);

                    // The 'Name' of the parameter is the query string key configured in the InitializeService method using the 
                    // AddFilterParameterConfiguration method.
                    // example: <sync:ScopeParameter Name="userid" Type="Edm.Guid" /> 
                    syncScopeParamsNode.Add(
                        new XElement(FormatterConstants.SyncNamespace + "ScopeParameter",
                                     new XAttribute("Name", filterParameter.QueryStringKey),
                                     new XAttribute("Type", edmType)));
                }

                schemaNode.Add(syncScopeParamsNode);
            }
            dataservicesNode.Add(schemaNode);
            edmxNode.Add(dataservicesNode);
            document.Add(edmxNode);
            return document;
        }   

        #endregion
    }
}
