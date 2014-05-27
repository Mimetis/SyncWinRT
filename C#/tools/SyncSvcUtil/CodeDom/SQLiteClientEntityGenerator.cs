using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Synchronization.Data;

namespace Microsoft.Synchronization.ClientServices.CodeDom
{
    class SQLiteClientEntityGenerator : EntityGenerator
    {
        public override void GenerateEntities(string filePrefix, string nameSpace, Data.DbSyncScopeDescription desc, Dictionary<string, Dictionary<string, string>> colsMappingInfo, System.IO.DirectoryInfo dirInfo, CodeLanguage option, string serviceUri)
        {
            // First generate the custom Context file
            CodeCompileUnit compileUnit = GenerateContextFile(filePrefix, nameSpace, desc, serviceUri);
            CodeDomUtility.SaveCompileUnitToFile(compileUnit, option, CodeDomUtility.GenerateFileName(desc.ScopeName, dirInfo, filePrefix, "OfflineContext", option));

            // Then generate the file containing the actual entities
            compileUnit = GenerateEntitiesFile(nameSpace, desc, colsMappingInfo);
            CodeDomUtility.SaveCompileUnitToFile(compileUnit, option, CodeDomUtility.GenerateFileName(desc.ScopeName, dirInfo, filePrefix, "Entities", option));

        }

        private CodeCompileUnit GenerateEntitiesFile(string nameSpace, Data.DbSyncScopeDescription desc, Dictionary<string, Dictionary<string, string>> colsMappingInfo)
        {
            CodeCompileUnit entitiesCC = new CodeCompileUnit();

            CodeNamespace entityScopeNs = new CodeNamespace(nameSpace);

            // Generate the entities
            foreach (DbSyncTableDescription table in desc.Tables)
            {
                Dictionary<string, string> curTableMapping = null;
                colsMappingInfo.TryGetValue(table.UnquotedGlobalName, out curTableMapping);
                
                // Generate the actual entity
                CodeTypeDeclaration entityDecl = CodeDomUtility.GetSQLiteEntityForTableDescription(table, true, curTableMapping);

                // Add the base type for the entity
                entityDecl.BaseTypes.Add(new CodeTypeReference(Constants.SQLiteOfflineEntity));
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

            // Add SQLiteContext base type
            wrapperEntity.BaseTypes.Add(Constants.SQLiteContextBaseType);

            #region Generate the GetSchema method
            CodeMemberMethod getSchemaMethod = new CodeMemberMethod();
            getSchemaMethod.Name = Constants.ClientIsolatedStoreGetSchemaMethodName;
            getSchemaMethod.Attributes = MemberAttributes.Private | MemberAttributes.Final | MemberAttributes.Static;
            getSchemaMethod.ReturnType = new CodeTypeReference(Constants.SQLiteSchemaBaseType);

            // Add the line 'OfflineSchema schema = new OfflineSchema ()'
            CodeVariableDeclarationStatement initSchemaStmt = new CodeVariableDeclarationStatement(Constants.SQLiteSchemaBaseType, "schema");
            initSchemaStmt.InitExpression = new CodeObjectCreateExpression(Constants.SQLiteSchemaBaseType);
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

                #region Add this entity to the GetSchema method
                CodeMethodInvokeExpression expr = new CodeMethodInvokeExpression();
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
                        new CodeParameterDeclarationExpression[] { }));
            ctor.BaseConstructorArgs.Add(new CodeSnippetExpression(Constants.ClientIsolatedStoreBaseCtor));
            wrapperEntity.Members.Add(ctor);

            #endregion


            getSchemaMethod.Statements.Add(new CodeMethodReturnStatement(new CodeSnippetExpression("schema")));
            wrapperEntity.Members.Add(getSchemaMethod);

            ctxScopeNs.Types.Add(wrapperEntity);
            contextCC.Namespaces.Add(ctxScopeNs);

            return contextCC;
        }
    }
}
