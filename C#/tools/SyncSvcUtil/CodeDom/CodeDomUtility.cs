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
using System.Data;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;

namespace Microsoft.Synchronization.ClientServices.CodeDom
{
    class CodeDomUtility
    {
        public static CodeTypeDeclaration GetEntityForTableDescription(DbSyncTableDescription tableDesc, bool addKeyAttributes, Dictionary<string, string> colsMapping)
        {
            CodeTypeDeclaration entityDeclaration = new CodeTypeDeclaration(SanitizeName(tableDesc.UnquotedGlobalName));
            entityDeclaration.IsPartial = true;
            entityDeclaration.IsClass = true;

            foreach (DbSyncColumnDescription column in tableDesc.Columns)
            {
                string colName = column.UnquotedName;
                if (colsMapping != null)
                {
                    colsMapping.TryGetValue(column.UnquotedName.ToLowerInvariant(), out colName);
                    colName = colName ?? column.UnquotedName;
                }
                CodeTypeReference fieldTypeReference = GetTypeFromSqlType(tableDesc, column);
                CodeMemberField colField = new CodeMemberField(fieldTypeReference, "_" + SanitizeName(colName));
                colField.Attributes = MemberAttributes.Private;

                CodeMemberProperty propertyField = new CodeMemberProperty();
                propertyField.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                propertyField.Name = SanitizeName(colName);
                propertyField.Type = fieldTypeReference;
                propertyField.GetStatements.Add(new CodeMethodReturnStatement(new CodeArgumentReferenceExpression(colField.Name)));
                propertyField.SetStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(colField.Name), new CodeVariableReferenceExpression("value")));

                if (addKeyAttributes)
                {
                    if (column.IsPrimaryKey)
                    {
                        //Add the Key attribute
                        propertyField.CustomAttributes.Add(new CodeAttributeDeclaration(Constants.ClientKeyAtributeType));
                    }
                }
                else
                {
                    // This is service entity. Check to see if column mappings is present i.e colName is not the same as column.UnquotedName.
                    if (!colName.Equals(column.UnquotedName, StringComparison.Ordinal))
                    {
                        propertyField.CustomAttributes.Add(new CodeAttributeDeclaration(Constants.ServiceSyncColumnMappingAttribute,
                                    new CodeAttributeArgument("LocalName", new CodeSnippetExpression("\"" + column.UnquotedName + "\""))));
                    }

                    // For a nullable data type, we add the [SyncEntityPropertyNullable] attribute to the property that is code-generated.
                    // This is required because some data types such as string are nullable by default in .NET and so there is no good way to 
                    // later determine whether the type in the underlying data store is nullable or not.
                    if (column.IsNullable)
                    {
                        propertyField.CustomAttributes.Add(new CodeAttributeDeclaration(Constants.EntityPropertyNullableAttributeType));
                    }
                }

                entityDeclaration.Members.Add(colField);
                entityDeclaration.Members.Add(propertyField);
            }

            return entityDeclaration;
        }
        
        public static CodeTypeDeclaration GetSQLiteEntityForTableDescription(DbSyncTableDescription tableDesc, bool addKeyAttributes, Dictionary<string, string> colsMapping)
        {
            CodeTypeDeclaration entityDeclaration = new CodeTypeDeclaration(SanitizeName(tableDesc.UnquotedGlobalName));
            entityDeclaration.IsPartial = true;
            entityDeclaration.IsClass = true;

            foreach (DbSyncColumnDescription column in tableDesc.Columns)
            {
                string colName = column.UnquotedName;
                if (colsMapping != null)
                {
                    colsMapping.TryGetValue(column.UnquotedName.ToLowerInvariant(), out colName);
                    colName = colName ?? column.UnquotedName;
                }
                CodeTypeReference fieldTypeReference = GetTypeFromSqlType(tableDesc, column);
                CodeMemberField colField = new CodeMemberField(fieldTypeReference, "_" + SanitizeName(colName));
                colField.Attributes = MemberAttributes.Private;

                CodeMemberProperty propertyField = new CodeMemberProperty();
                propertyField.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                propertyField.Name = SanitizeName(colName);
                propertyField.Type = fieldTypeReference;
                propertyField.GetStatements.Add(new CodeMethodReturnStatement(new CodeArgumentReferenceExpression(colField.Name)));
                propertyField.SetStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(colField.Name), new CodeVariableReferenceExpression("value")));

                if (addKeyAttributes)
                {
                    if (column.IsPrimaryKey)
                    {
                        //Add the Key attribute
                        propertyField.CustomAttributes.Add(new CodeAttributeDeclaration(Constants.SQLitePrimaryKeyAtributeType));
                    }
                    if (column.SizeSpecified && column.Size != "0")
                    {
                        int maxSize;
                        var isNotMaxSize = int.TryParse(column.Size, out maxSize);
                        string maxLength = isNotMaxSize ? column.Size : "8000";
                        var cad = new CodeAttributeDeclaration(Constants.SQLiteMaxLengthAttribute);
                        cad.Arguments.Add(new CodeAttributeArgument(new CodeSnippetExpression(maxLength)));
                        //Add the MaxLength at CodeAttributeDeclaration(maxSizeAttributeString)tribute
                        propertyField.CustomAttributes.Add(cad);

                    }
                    if (column.AutoIncrementSeedSpecified)
                    {
                        propertyField.CustomAttributes.Add(new CodeAttributeDeclaration(Constants.SQLiteAutoIncrementAttribute));
                    }
                }
                else
                {
                    // This is service entity. Check to see if column mappings is present i.e colName is not the same as column.UnquotedName.
                    if (!colName.Equals(column.UnquotedName, StringComparison.Ordinal))
                    {
                        propertyField.CustomAttributes.Add(new CodeAttributeDeclaration(Constants.ServiceSyncColumnMappingAttribute,
                                    new CodeAttributeArgument("LocalName", new CodeSnippetExpression("\"" + column.UnquotedName + "\""))));
                    }

                    // For a nullable data type, we add the [SyncEntityPropertyNullable] attribute to the property that is code-generated.
                    // This is required because some data types such as string are nullable by default in .NET and so there is no good way to 
                    // later determine whether the type in the underlying data store is nullable or not.
                    if (column.IsNullable)
                    {
                        propertyField.CustomAttributes.Add(new CodeAttributeDeclaration(Constants.EntityPropertyNullableAttributeType));
                    }
                }

                entityDeclaration.Members.Add(colField);
                entityDeclaration.Members.Add(propertyField);
            }

            return entityDeclaration;
        }

        /// <summary>
        /// Takes the CodeCompileUnit and generates code for the specified language options
        /// and saves it to a file.
        /// </summary>
        /// <param name="cc">Actual CodeCompileUnit</param>
        /// <param name="option">Language Option</param>
        /// <param name="fileName">File name where the code will be saved</param>
        public static void SaveCompileUnitToFile(CodeCompileUnit cc, CodeLanguage option, string fileName)
        {
            CodeDomProvider csprovider = CodeDomProvider.CreateProvider(option.ToString());
            StringWriter builder = new StringWriter();
            csprovider.GenerateCodeFromCompileUnit(cc, builder, null);

            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(fs))
                {
                    writer.Write(builder.ToString());
                    writer.Flush();
                }
            }

        }

        public static void SaveSVCFile(string ns, string syncSvcTypeName, string codeBehindFileName, string fileName, CodeLanguage option)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat(Constants.ServiceSyncServicSVCFileContents, option.ToString().ToLowerInvariant(), ns, 
                syncSvcTypeName, new FileInfo(codeBehindFileName).Name, 
                AssemblyName.GetAssemblyName(Assembly.GetCallingAssembly().Location).Version.ToString());
            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(fs))
                {
                    writer.Write(builder.ToString());
                    writer.Flush();
                }
            }
        }


        public static string SanitizeName(string name)
        {
            return name.Replace(' ', '_').Replace('.', '_');
        }

        public static string GenerateFileName(string scopeName, DirectoryInfo dirInfo, string filePrefix, string fileSuffix, CodeLanguage option)
        {
            // Check to create the directory if it doesnt exist.
            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }
            return Path.Combine(
                dirInfo.FullName, 
                string.Format("{0}{1}.{2}", string.IsNullOrEmpty(filePrefix) ? scopeName : filePrefix, fileSuffix,  option.ToString().ToLowerInvariant())
                );
        }

        /// <summary>
        /// Generates a type with the following format.
        /// public abstract class [baseNamePrefix]OfflineEntityBase
        /// {
        ///     public Microsoft.Synchronization.[Client]Services.OfflineEntityMetata ServiceMetadata { get; set;}
        ///     
        ///     public [baseNamePrefix]OfflineEntityBase()
        ///     {
        ///         this.ServiceMetadata = new Microsoft.Synchronization.[Client]Services.OfflineEntityMetata();
        ///     }
        /// }
        /// </summary>
        /// <param name="baseNamePrefix">Prefix</param>
        /// <param name="isServer">Bool indication whether to use the OfflineEntityMetadata type from server namespace or not</param>
        /// <returns></returns>
        public static CodeTypeDeclaration CreateIOfflineEntityCustomBaseClass(string baseNamePrefix, bool isServer)
        {
            CodeTypeDeclaration baseDeclaration = new CodeTypeDeclaration(string.Format(Constants.OfflineEntityBaseTypeNameFormat, baseNamePrefix));
            baseDeclaration.Attributes = MemberAttributes.Abstract | MemberAttributes.Public;
            baseDeclaration.IsClass = true;

            // Add the default Ctor
            CodeConstructor ctor = new CodeConstructor();
            ctor.Attributes = MemberAttributes.Public;
            ctor.Statements.Add(
                new CodeAssignStatement(
                    new CodeVariableReferenceExpression(Constants.ServiceMetadataPropertyName),                    
                    new CodeObjectCreateExpression(isServer ? Constants.ServiceOfflineEntityMetadataTypeName : Constants.ClientOfflineEntityMetadataTypeName)));
            baseDeclaration.Members.Add(ctor);

            // Add the ServiceMetadata field            
            CodeMemberField svcMetadataField = new CodeMemberField();
            svcMetadataField.Attributes = MemberAttributes.Private;
            svcMetadataField.Name = Constants.ServiceMetadataFieldName;
            svcMetadataField.Type = new CodeTypeReference(isServer ? Constants.ServiceOfflineEntityMetadataTypeName : Constants.ClientOfflineEntityMetadataTypeName);
            baseDeclaration.Members.Add(svcMetadataField);

            // Add the ServiceMetadata Property            
            CodeMemberProperty svcMetadataProp = new CodeMemberProperty();
            svcMetadataProp.Attributes = MemberAttributes.Public;
            svcMetadataProp.Name = Constants.ServiceMetadataPropertyName;
            svcMetadataProp.Type = new CodeTypeReference(isServer ? Constants.ServiceOfflineEntityMetadataTypeName : Constants.ClientOfflineEntityMetadataTypeName);
            svcMetadataProp.ImplementationTypes.Add(isServer ? new CodeTypeReference(Constants.ServiceIOfflineEntity) : new CodeTypeReference(Constants.ClientIOfflineEntity));
            svcMetadataProp.GetStatements.Add(new CodeMethodReturnStatement(new CodeArgumentReferenceExpression(svcMetadataField.Name)));

            // This creates a check for following.
            // if(value == null) throw new ArgumentNullException("value")
            CodeConditionStatement checkForNullStmt = new CodeConditionStatement(
                new CodeBinaryOperatorExpression(
                    new CodeSnippetExpression("value"), CodeBinaryOperatorType.IdentityEquality, new CodePrimitiveExpression(null)
                    ),
                new CodeStatement[] 
                { 
                    new CodeThrowExceptionStatement(
                        new CodeObjectCreateExpression(new CodeTypeReference(typeof(ArgumentNullException)), new CodeSnippetExpression("\"value\""))
                        ) 
                });
            
            // Add the check for null stmt
            svcMetadataProp.SetStatements.Add(checkForNullStmt);
            svcMetadataProp.SetStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(svcMetadataField.Name), new CodeVariableReferenceExpression("value")));
            baseDeclaration.Members.Add(svcMetadataProp);

            return baseDeclaration;

        }

        /// <summary>
        /// Denotes the mapping between the SQLType and the actual .NET CLR type
        /// Uses the mapping defined in the following MSDN link http://msdn.microsoft.com/en-us/library/ms131092.aspx
        /// </summary>
        /// <param name="tableDesc">DbSyncTableDescription object</param>
        /// <param name="colDesc">DbSyncColumnDescription object</param>
        /// <returns> A .NET CLRT type name</returns>
        private static CodeTypeReference GetTypeFromSqlType(DbSyncTableDescription tableDesc, DbSyncColumnDescription colDesc)
        {
            string sqltype = colDesc.Type;
            bool isNullable = colDesc.IsNullable;

            if(sqltype.Equals("sql_variant", StringComparison.OrdinalIgnoreCase))
            {
                sqltype = "variant";
            }

            SqlDbType type = (SqlDbType)Enum.Parse(typeof(SqlDbType), sqltype, true);
            Type retType;
            switch (type)
            {
                case SqlDbType.Bit:
                    retType = typeof(bool);
                    break;
                case SqlDbType.BigInt:
                    retType = (typeof(Int64));
                    break;
                case SqlDbType.Binary:
                case SqlDbType.Image:
                case SqlDbType.VarBinary:
                case SqlDbType.Timestamp:
                    retType = (typeof(byte[]));
                    break;
                case SqlDbType.Char:
                    retType = (typeof(string));
                    break;
                case SqlDbType.Date:
                case SqlDbType.DateTime:
                case SqlDbType.DateTime2:
                case SqlDbType.SmallDateTime:
                    retType = (typeof(DateTime));
                    break;
                case SqlDbType.DateTimeOffset:
                    retType = (typeof(DateTimeOffset));
                    break;
                case SqlDbType.Decimal:
                case SqlDbType.Money:
                case SqlDbType.SmallMoney:
                    retType = (typeof(decimal));
                    break;
                case SqlDbType.Float:
                    retType = (typeof(double));
                    break;
                case SqlDbType.Int:
                    retType = (typeof(int));
                    break;
                case SqlDbType.NChar:
                case SqlDbType.Text:
                case SqlDbType.NText:
                case SqlDbType.NVarChar:
                case SqlDbType.VarChar:
                case SqlDbType.Xml:
                    retType = (typeof(string));
                    break;
                case SqlDbType.Real:
                    retType = (typeof(Single));
                    break;
                case SqlDbType.SmallInt:
                    retType = (typeof(Int16));
                    break;
                case SqlDbType.Time:
                    retType = (typeof(TimeSpan));
                    break;
                case SqlDbType.TinyInt:
                    retType = (typeof(byte));
                    break;
                case SqlDbType.UniqueIdentifier:
                    retType = (typeof(Guid));
                    break;
                case SqlDbType.Variant:
                default:
                    throw new NotSupportedException(string.Format("Column '{0}' in Table '{1}' has an unsupported SqlType - '{2}'",
                        colDesc.UnquotedName, tableDesc.UnquotedGlobalName, sqltype));
            }

            if (isNullable && retType.IsValueType)
            {
                CodeTypeReference ctr = new CodeTypeReference(typeof(Nullable<>));
                ctr.TypeArguments.Add(retType);
                return ctr;
            }
            else
            {
                return new CodeTypeReference(retType);
            }
        }
    }
}
