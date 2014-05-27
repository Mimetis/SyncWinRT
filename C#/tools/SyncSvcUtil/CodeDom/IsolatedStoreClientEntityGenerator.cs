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
using System.CodeDom.Compiler;
using System.CodeDom;
using Microsoft.Synchronization.Data;

namespace Microsoft.Synchronization.ClientServices.CodeDom
{
    /// <summary>
    /// Generates two files per SyncScope.
    /// 1. A customized OfflineContext file that derives from IsolatedStorageOfflineContext
    /// 2. An entities file that emits enitites that derive from IsolatedStorageOfflineEntity
    /// </summary>
    class IsolatedStoreClientEntityGenerator : EntityGenerator
    {
        public override void GenerateEntities(string filePrefix, string nameSpace, Data.DbSyncScopeDescription desc, Dictionary<string, Dictionary<string, string>> colsMappingInfo,
            System.IO.DirectoryInfo dirInfo, CodeLanguage option, string serviceUri)
        {
            // First generate the custom Context file
            CodeCompileUnit compileUnit = GenerateContextFile(filePrefix, nameSpace, desc, serviceUri);
            CodeDomUtility.SaveCompileUnitToFile(compileUnit, option, CodeDomUtility.GenerateFileName(desc.ScopeName, dirInfo, filePrefix, "OfflineContext", option));
         
            // Then generate the file containing the actual entities
            compileUnit = GenerateEntitiesFile(nameSpace, desc, colsMappingInfo);
            CodeDomUtility.SaveCompileUnitToFile(compileUnit, option, CodeDomUtility.GenerateFileName(desc.ScopeName, dirInfo, filePrefix, "Entities", option));
        }

        private static CodeCompileUnit GenerateEntitiesFile(string nameSpace, DbSyncScopeDescription desc, Dictionary<string, Dictionary<string, string>> colsMappingInfo)
        {
            CodeCompileUnit entitiesCC = new CodeCompileUnit();

            CodeNamespace entityScopeNs = new CodeNamespace(nameSpace);

            // Generate the entities
            foreach (DbSyncTableDescription table in desc.Tables)
            {
                Dictionary<string, string> curTableMapping = null;
                colsMappingInfo.TryGetValue(table.UnquotedGlobalName, out curTableMapping);
                // Generate the actual entity
                CodeTypeDeclaration entityDecl = CodeDomUtility.GetEntityForTableDescription(table, true, curTableMapping);

                // Add the base type for the entity
                entityDecl.BaseTypes.Add(new CodeTypeReference(Constants.ClientIsolatedStoreOfflineEntity));
                entityScopeNs.Types.Add(entityDecl);

                foreach (CodeTypeMember member in entityDecl.Members)
                {
                    CodeMemberProperty prop = member as CodeMemberProperty;
                    if (prop != null)
                    {
                        // For each setter add the OnPropertyChanging and OnPropertyChanged lines
                        CodeStatement stmt = prop.SetStatements[0];
                        prop.SetStatements.Clear();

                        prop.SetStatements.Add(new CodeMethodInvokeExpression(
                            new CodeBaseReferenceExpression(), 
                            Constants.ClientCallOnPropertyChanging, 
                            new CodeSnippetExpression(string.Format(Constants.StringifyProperty, prop.Name)))
                            );
                        prop.SetStatements.Add(stmt);
                        prop.SetStatements.Add(new CodeMethodInvokeExpression(
                            new CodeBaseReferenceExpression(),
                            Constants.ClientCallOnPropertyChanged,
                            new CodeSnippetExpression(string.Format(Constants.StringifyProperty, prop.Name)))
                            );
                    }
                }
            }

            entitiesCC.Namespaces.Add(entityScopeNs);
            return entitiesCC;
        }

        private static CodeCompileUnit GenerateContextFile(string prefix, string nameSpace, DbSyncScopeDescription desc, string serviceUri)
        {
            CodeCompileUnit contextCC = new CodeCompileUnit();

            CodeNamespace ctxScopeNs = new CodeNamespace(nameSpace);

            // Generate the outer most entity
            CodeTypeDeclaration wrapperEntity = new CodeTypeDeclaration(
                string.Format(Constants.ClientContextClassNameFormat, string.IsNullOrEmpty(prefix) ? desc.ScopeName : prefix)
                );
            wrapperEntity.BaseTypes.Add(Constants.ClientContextBaseType);

            #region Generate the GetSchema method
            CodeMemberMethod getSchemaMethod = new CodeMemberMethod();
            getSchemaMethod.Name = Constants.ClientIsolatedStoreGetSchemaMethodName;
            getSchemaMethod.Attributes = MemberAttributes.Private | MemberAttributes.Final | MemberAttributes.Static;
            getSchemaMethod.ReturnType = new CodeTypeReference(Constants.ClientSchemaBaseType);

            // Add the line 'IsolatedStoreSchema schema = new IsolatedStoreSchema()'
            CodeVariableDeclarationStatement initSchemaStmt = new CodeVariableDeclarationStatement(Constants.ClientSchemaBaseType, "schema");
            initSchemaStmt.InitExpression = new CodeObjectCreateExpression(Constants.ClientSchemaBaseType);
            getSchemaMethod.Statements.Add(initSchemaStmt);

            #endregion

            // Generate the entities
            foreach (DbSyncTableDescription table in desc.Tables)
            {
                string tableName = CodeDomUtility.SanitizeName(table.UnquotedGlobalName);
                CodeTypeReference icollReference = new CodeTypeReference(typeof(IEnumerable<>));

                // Generate the private field
                CodeTypeReference entityReference = new CodeTypeReference(tableName);
                icollReference.TypeArguments.Clear();
                icollReference.TypeArguments.Add(entityReference);

                #region Generate the AddXXX method.

                // Define the method signature as 'public void Add[Entity]([Entity] entity)'
                CodeMemberMethod addMethod = new CodeMemberMethod();
                addMethod.Name = string.Format(Constants.ClientContextAddMethodFormat, tableName);
                addMethod.Parameters.Add(new CodeParameterDeclarationExpression(tableName, "entity"));
                addMethod.ReturnType = new CodeTypeReference(typeof(void));
                addMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final;

                //Generate the base.Add method
                CodeMethodInvokeExpression baseAddExpr = new CodeMethodInvokeExpression();
                baseAddExpr.Method = new CodeMethodReferenceExpression();
                baseAddExpr.Method.TargetObject = new CodeBaseReferenceExpression();
                baseAddExpr.Method.MethodName = Constants.ClientAddMethodBodyFormat;
                baseAddExpr.Method.TypeArguments.Add(tableName);
                baseAddExpr.Parameters.Add(new CodeSnippetExpression(Constants.ClientEntityVariableName));
                addMethod.Statements.Add(baseAddExpr);

                #endregion
                
                wrapperEntity.Members.Add(addMethod);

                #region Generate the DeleteXXX method.

                // Define the method signature as 'public void Add[Entity]([Entity] entity)'
                CodeMemberMethod delMethod = new CodeMemberMethod();
                delMethod.Name = string.Format(Constants.ClientContextDeleteMethodFormat, tableName);
                delMethod.Parameters.Add(new CodeParameterDeclarationExpression(tableName, Constants.ClientEntityVariableName));
                delMethod.ReturnType = new CodeTypeReference(typeof(void));
                delMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final;

                //Generate the base.Add method
                CodeMethodInvokeExpression baseDelExpr = new CodeMethodInvokeExpression();
                baseDelExpr.Method = new CodeMethodReferenceExpression();
                baseDelExpr.Method.TargetObject = new CodeBaseReferenceExpression();
                baseDelExpr.Method.MethodName = Constants.ClientDeleteMethodBodyFormat;
                baseDelExpr.Method.TypeArguments.Add(tableName);
                baseDelExpr.Parameters.Add(new CodeSnippetExpression(Constants.ClientEntityVariableName));
                delMethod.Statements.Add(baseDelExpr);
                #endregion

                wrapperEntity.Members.Add(delMethod);

                #region Generate the [Entities] property

                CodeMethodInvokeExpression getCollectionExpr = new CodeMethodInvokeExpression();
                getCollectionExpr.Method = new CodeMethodReferenceExpression();
                getCollectionExpr.Method.TargetObject = new CodeBaseReferenceExpression();
                getCollectionExpr.Method.MethodName = Constants.ClientGetCollectionMethodBodyFormat;
                getCollectionExpr.Method.TypeArguments.Add(tableName);

                CodeMemberProperty getProperty = new CodeMemberProperty();
                getProperty.Name = string.Format(Constants.EntityGetCollectionFormat, tableName);
                getProperty.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                getProperty.Type = icollReference;
                getProperty.GetStatements.Add(new CodeMethodReturnStatement(getCollectionExpr));
                #endregion

                wrapperEntity.Members.Add(getProperty);


                #region Add this entity to the GetSchema method
                CodeMethodInvokeExpression expr =new CodeMethodInvokeExpression();
                expr.Method = new CodeMethodReferenceExpression();
                expr.Method.TargetObject = new CodeVariableReferenceExpression("schema");
                expr.Method.MethodName = "AddCollection";
                expr.Method.TypeArguments.Add(tableName);
                getSchemaMethod.Statements.Add(expr);
                #endregion

            }

            #region Add a const for the scopeName and default URL
            CodeMemberField scopeField = new CodeMemberField(typeof(string), "SyncScopeName");
            scopeField.Attributes = MemberAttributes.Const | MemberAttributes.Private;
            scopeField.InitExpression = new CodePrimitiveExpression(desc.ScopeName);
            wrapperEntity.Members.Add(scopeField);

            if (serviceUri != null)
            {
                CodeMemberField urlField = new CodeMemberField(typeof(Uri), "SyncScopeUri");
                urlField.Attributes = MemberAttributes.Static | MemberAttributes.Private;
                urlField.InitExpression = new CodeObjectCreateExpression(typeof(Uri), new CodePrimitiveExpression(serviceUri));
                wrapperEntity.Members.Add(urlField);
            }

            #endregion

            #region Add Constructor

            if (serviceUri != null)
            {
                // If serviceUri is present then add constructors with just the cachePath and 
                // cachePath with encryption algorithm specified

                // Add the constructor with just the cachepath
                CodeConstructor ctor1 = new CodeConstructor();
                ctor1.Attributes = MemberAttributes.Public;
                ctor1.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), Constants.ClientCachePathArgName));
                ctor1.ChainedConstructorArgs.Add(new CodeSnippetExpression(Constants.ClientIsolatedStoreCallCtorWithUri));
                wrapperEntity.Members.Add(ctor1);


                // Add the constructor with just cache path and encryption overload
                CodeConstructor eCtor1 = new CodeConstructor();
                eCtor1.Attributes = MemberAttributes.Public;
                eCtor1.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), Constants.ClientCachePathArgName));
                eCtor1.Parameters.Add(new CodeParameterDeclarationExpression(Type.GetType(Constants.SymmetricAlgorithmTypeName),
                    Constants.ClientSymmetricAlgorithmArgName));
                eCtor1.ChainedConstructorArgs.Add(new CodeSnippetExpression(Constants.ClientIsolatedStoreCallEncryptedCtorWithUri));
                wrapperEntity.Members.Add(eCtor1);
            }

            // Add the constructor with no encryption
            CodeConstructor ctor = new CodeConstructor();
            ctor.Attributes = MemberAttributes.Public;
            ctor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), Constants.ClientCachePathArgName));
            ctor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(Uri), Constants.ClientServiceUriArgName));
            ctor.BaseConstructorArgs.Add(
                new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(
                        new CodeSnippetExpression(wrapperEntity.Name), Constants.ClientIsolatedStoreGetSchemaMethodName),
                        new CodeParameterDeclarationExpression[] {}));
            ctor.BaseConstructorArgs.Add(new CodeSnippetExpression(Constants.ClientIsolatedStoreBaseCtor));
            wrapperEntity.Members.Add(ctor);

            // Add the constructor with encryption overload
            CodeConstructor eCtor = new CodeConstructor();
            eCtor.Attributes = MemberAttributes.Public;
            eCtor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), Constants.ClientCachePathArgName));
            eCtor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(Uri), Constants.ClientServiceUriArgName));
            eCtor.Parameters.Add(new CodeParameterDeclarationExpression(Type.GetType(Constants.SymmetricAlgorithmTypeName),
                Constants.ClientSymmetricAlgorithmArgName));
            eCtor.BaseConstructorArgs.Add(
                new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(
                        new CodeSnippetExpression(wrapperEntity.Name), Constants.ClientIsolatedStoreGetSchemaMethodName),
                        new CodeParameterDeclarationExpression[] { }));
            eCtor.BaseConstructorArgs.Add(new CodeSnippetExpression(Constants.ClientIsolatedStoreEncryptedBaseCtor));
            wrapperEntity.Members.Add(eCtor);

            #endregion


            getSchemaMethod.Statements.Add(new CodeMethodReturnStatement(new CodeSnippetExpression("schema")));
            wrapperEntity.Members.Add(getSchemaMethod);

            ctxScopeNs.Types.Add(wrapperEntity);
            contextCC.Namespaces.Add(ctxScopeNs);

            return contextCC;
        }
    }
}
