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
using Microsoft.Synchronization.Data;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;

namespace Microsoft.Synchronization.ClientServices.CodeDom
{
    class ServiceEntityGenerator : EntityGenerator
    {
        private string _syncObjectSchema;

        /// <summary>
        /// Create a new instance of the ServiceEntityGenerator class.
        /// </summary>
        /// <param name="syncObjectSchema">Schema under which the sync related objects are created in the SQL database.</param>
        public ServiceEntityGenerator(string syncObjectSchema)
        {
            _syncObjectSchema = syncObjectSchema;
        }

        /// <summary>
        /// Generates thes following entities for the service.
        /// 1. An XXXEntities.cs file that contains the SyncScope attributed collection name and all the individual Entities that make that scope.
        /// 2. An .SVC file for the sync service.
        /// 3. The .svc.[Language] code behind file for the above .svc file generated.
        /// </summary>
        /// <param name="filePrefix"></param>
        /// <param name="nameSpace"></param>
        /// <param name="desc"></param>
        /// <param name="dirInfo"></param>
        /// <param name="option"></param>
        /// <param name="colsMappingInfo"></param>
        /// <param name="serviceUri"></param>
        public override void GenerateEntities(string filePrefix, string nameSpace, DbSyncScopeDescription desc, Dictionary<string, Dictionary<string, string>> colsMappingInfo,
            System.IO.DirectoryInfo dirInfo, CodeLanguage option, string serviceUri)
        {
            CodeCompileUnit cc = GenerateEntitiesCompileUnit(filePrefix, nameSpace, desc, colsMappingInfo);

            CodeDomUtility.SaveCompileUnitToFile(cc, option, CodeDomUtility.GenerateFileName(desc.ScopeName, dirInfo, filePrefix, "Entities", option));

            cc = GenerateSyncServiceCompileUnit(filePrefix, nameSpace, desc);

            // Generate the codebehing file for .svc file
            string codeBehindFilename = CodeDomUtility.GenerateFileName(desc.ScopeName, dirInfo, filePrefix, "SyncService.svc", option);
            CodeDomUtility.SaveCompileUnitToFile(cc, option, codeBehindFilename);

            // Generate the actual .SVC file.
            CodeDomUtility.SaveSVCFile(nameSpace, cc.Namespaces[0].Types[0].Name, codeBehindFilename, CodeDomUtility.GenerateFileName(desc.ScopeName, dirInfo, filePrefix, "SyncService", CodeLanguage.SVC), option);
        }

        private CodeCompileUnit GenerateSyncServiceCompileUnit(string prefix, string nameSpace, DbSyncScopeDescription desc)
        {
            CodeCompileUnit cc = new CodeCompileUnit();

            CodeNamespace scopeNs = new CodeNamespace(nameSpace);

            // Generate the SyncService<T> definition type.
            CodeTypeDeclaration wrapperEntity = new CodeTypeDeclaration(
                string.Format(Constants.ServiceCustomSyncServiceTypeFormat, string.IsNullOrEmpty(prefix) ? desc.ScopeName : prefix)
                );

            CodeTypeReference baseSyncType = new CodeTypeReference(Constants.ServiceSyncServiceBaseTypeFormat);
            baseSyncType.TypeArguments.Add(string.Format(Constants.ServiceOuterEntityNameFormat, string.IsNullOrEmpty(prefix) ? desc.ScopeName : prefix));
            wrapperEntity.BaseTypes.Add(baseSyncType);

            wrapperEntity.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "SyncService: Configuration and setup"));

            // Add the method InitializeService
            CodeMemberMethod initSvcMethod = new CodeMemberMethod();
            initSvcMethod.Name = "InitializeService";
            initSvcMethod.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            initSvcMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(Constants.ServiceISyncSvcConfigurationType), "config"));

            // Add comments for the following lines.
            foreach (string s in Constants.ServiceSyncSvcCommentLines)
            {
                initSvcMethod.Statements.Add(new CodeCommentStatement(s));
            }

            // Set the sync object schema. This is used when creating new scopes.
            // For SQL Azure, tables have to be explicitly referenced with the schema name.
            if (!String.IsNullOrEmpty(_syncObjectSchema))
            {
                foreach (string s in Constants.ServiceSyncSvcPreSchemaCommentLines)
                {
                    initSvcMethod.Statements.Add(new CodeCommentStatement(s));
                }

                CodeMethodInvokeExpression expr =
                    new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("config"), 
                                                   Constants.ServiceSyncSvcInitializeService_SetSchemaMethodFormat, 
                                                   new CodeExpression[] { new CodePrimitiveExpression(_syncObjectSchema) });

                initSvcMethod.Statements.Add(expr);
            }

            wrapperEntity.Members.Add(initSvcMethod);
            wrapperEntity.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, string.Empty));

            scopeNs.Types.Add(wrapperEntity);
            cc.Namespaces.Add(scopeNs);

            return cc;
        }

        private static CodeCompileUnit GenerateEntitiesCompileUnit(string prefix, string nameSpace, Data.DbSyncScopeDescription desc, Dictionary<string, Dictionary<string, string>> colsMappingInfo)
        {
            CodeCompileUnit cc = new CodeCompileUnit();

            CodeNamespace scopeNs = new CodeNamespace(nameSpace);

            // Generate the outer most entity
            CodeTypeDeclaration wrapperEntity = new CodeTypeDeclaration(
                string.Format(Constants.ServiceOuterEntityNameFormat, string.IsNullOrEmpty(prefix) ? desc.ScopeName : prefix)
                );
            wrapperEntity.CustomAttributes.Add(new CodeAttributeDeclaration(Constants.ServiceSyncScopeAttributeDefinition));

            // Generate the base class for all the entities which will implement IOfflineEntity
            CodeTypeDeclaration baseEntity = CodeDomUtility.CreateIOfflineEntityCustomBaseClass(
                string.IsNullOrEmpty(prefix) ? desc.ScopeName : prefix,
                true /*isServer*/);

            // Set the base type
            // VB uses different keywords for class and interface inheritence. For it to emit the
            // right keyword it must inherit from object first before the actual interface.
            baseEntity.BaseTypes.Add(new CodeTypeReference(typeof(object)));
            baseEntity.BaseTypes.Add(new CodeTypeReference(Constants.ServiceIOfflineEntity));

            scopeNs.Types.Add(baseEntity);

            // Generate the entities
            foreach (DbSyncTableDescription table in desc.Tables)
            {
                Dictionary<string, string> curTableMapping = null;
                colsMappingInfo.TryGetValue(table.UnquotedGlobalName, out curTableMapping);

                string tableName = CodeDomUtility.SanitizeName(table.UnquotedGlobalName);
                CodeTypeReference icollReference = new CodeTypeReference(typeof(ICollection<>));

                // Generate the private field
                CodeTypeReference entityReference = new CodeTypeReference(tableName);
                icollReference.TypeArguments.Clear();
                icollReference.TypeArguments.Add(entityReference);

                CodeMemberField itemCollectionField = new CodeMemberField(icollReference, string.Format(Constants.EntityFieldNameFormat, tableName));
                itemCollectionField.Attributes = MemberAttributes.Private;
                wrapperEntity.Members.Add(itemCollectionField);

                // Generate the actual entity
                CodeTypeDeclaration entityDecl = CodeDomUtility.GetEntityForTableDescription(table, false /*addKeyAttributes*/, curTableMapping);
                entityDecl.BaseTypes.Add(baseEntity.Name);

                var syncEntityAttributeDeclaration = new CodeAttributeDeclaration(Constants.ServiceSyncEntityAttribute,
                    new CodeAttributeArgument("TableGlobalName", new CodeSnippetExpression("\"" + CodeDomUtility.SanitizeName(table.UnquotedGlobalName) + "\"")),
                    // table.LocalName is quoted and contains the schema name.
                    new CodeAttributeArgument("TableLocalName", new CodeSnippetExpression("\"" + table.LocalName + "\"")),
                    new CodeAttributeArgument("KeyFields",
                        new CodeSnippetExpression("\"" +
                            String.Join(",", table.PkColumns.
                            Select(e => CodeDomUtility.SanitizeName(GetKeyColumnName(e.UnquotedName, curTableMapping))).ToArray()) +
                            "\"")));

                // Use the PkColumns property to generate the column list for the KeyFields property. 
                // This is important as it is used later to generate SyncId's for rows and ordering is important.
                entityDecl.CustomAttributes.Add(syncEntityAttributeDeclaration);

                scopeNs.Types.Add(entityDecl);
            }

            scopeNs.Types.Add(wrapperEntity);
            cc.Namespaces.Add(scopeNs);
            return cc;
        }

        private static string GetKeyColumnName(string columnName, Dictionary<string, string> colsMapping)
        {
            return (colsMapping == null || !colsMapping.ContainsKey(columnName.ToLowerInvariant())) ? columnName : colsMapping[columnName.ToLowerInvariant()];
        }
    }
}
