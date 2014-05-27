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
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.Synchronization.ClientServices.CodeDom;
using Microsoft.Synchronization.ClientServices.Configuration;
using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServer;

namespace Microsoft.Synchronization.ClientServices
{
    /// <summary>
    /// TODO: Need to do the following.
    /// 1. Camel case the internal variables.
    /// 2. Pascal case the public properties.
    /// </summary>
    public class SyncSvcUtil
    {
        private SyncSvcUtil() { }

        public static event EventHandler<StringEventArgs> LogOccured = null;

        /// <summary>
        /// Entry point
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            try
            {
                ArgsParser parser = null;

                try
                {
                    parser = ArgsParser.ParseArgs(args);
                }
                catch (Exception e)
                {
                    LogArgsError(e.Message);
                    return;
                }

                if (parser.HelpRequested)
                {
                    return;
                }
                if (!parser.ModeSpecified)
                {
                    LogArgsError("Required argument /mode is not specified.");
                    return;
                }

                if (parser.UseCSDLUrl)
                {
                    // This means user have specified a Config file. Read it and create the DbSyncDescription for it.
                    ProcessCSDLUri(parser);
                }
                else
                {
                    // This means user have specified a Config file. Read it and create the DbSyncDescription for it.
                    ProcessConfigFile(parser);
                }

                Log("{0} completed with no errors.", Constants.AssemblyName);
            }
            catch (Exception e)
            {
                Log(e.ToString());
                Log("{0} failed.", Constants.AssemblyName);
            }

            Console.ReadLine();
        }

        private static void ProcessCSDLUri(ArgsParser parser)
        {
            Dictionary<string, Dictionary<string, string>> tablesToColumnMappingsInfo = new Dictionary<string, Dictionary<string, string>>();

            ValidateCSDLMode(parser);
            string serviceUri = null;
            DbSyncScopeDescription scopeDescription = CSDLParser.GetDescriptionFromUri(parser, parser.CSDLUrl, out serviceUri);

            Log("Generating files...");
            EntityGenerator generator = EntityGeneratorFactory.Create(parser.CodeGenMode, null /* null syncSchema - not needed for client code generation */);
            generator.GenerateEntities(parser.GeneratedFilePrefix,
                string.IsNullOrEmpty(parser.Namespace)
                ? string.IsNullOrEmpty(parser.GeneratedFilePrefix) ? scopeDescription.ScopeName : parser.GeneratedFilePrefix
                : parser.Namespace,
                scopeDescription, tablesToColumnMappingsInfo, parser.WorkingDirectory, parser.Language, serviceUri);
        }

        private static void ProcessConfigFile(ArgsParser parser)
        {
            DbSyncScopeDescription scopeDescription;
            Dictionary<string, Dictionary<string, string>> tablesToColumnMappingsInfo = new Dictionary<string, Dictionary<string, string>>();

            if (string.IsNullOrEmpty(parser.ConfigFile))
            {
                LogArgsError("Required argument /scopeconfig is not specified.");
                return;
            }
            if (!System.IO.File.Exists(parser.ConfigFile))
            {
                LogArgsError("Unable to find scopeconfig file '" + parser.ConfigFile + "'");
                return;
            }

            System.Configuration.Configuration config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = parser.ConfigFile }, ConfigurationUserLevel.None);

            Log("Reading specified config file...");
            SyncConfigurationSection syncConfig = config.GetSection(Constants.SyncConfigurationSectionName) as SyncConfigurationSection;

            // ValidateConfigFile the config and the passed in input parameters
            ValidateConfigFile(parser, syncConfig);

            // Fill in the defaults value for the parser.
            SelectedConfigSections selectedConfig = FillDefaults(parser, syncConfig);


            Log("Generating DbSyncScopeDescription for scope {0}...", selectedConfig.SelectedSyncScope.Name);
            scopeDescription = GetDbSyncScopeDescription(selectedConfig);
            tablesToColumnMappingsInfo = BuildColumnMappingInfo(selectedConfig);
            switch (parser.OperationMode)
            {

                case OperationMode.Provision:
                    try
                    {


                        SqlSyncScopeProvisioning prov = new SqlSyncScopeProvisioning(new SqlConnection(selectedConfig.SelectedTargetDatabase.GetConnectionString()),
                                            scopeDescription, selectedConfig.SelectedSyncScope.IsTemplateScope ? SqlSyncScopeProvisioningType.Template : SqlSyncScopeProvisioningType.Scope);

                        // Note: Deprovisioning does not work because of a bug in the provider when you set the ObjectSchema property to “dbo”. 
                        // The workaround is to not set the property (it internally assumes dbo in this case) so that things work on deprovisioning.
                        if (!String.IsNullOrEmpty(selectedConfig.SelectedSyncScope.SchemaName))
                        {
                            prov.ObjectSchema = selectedConfig.SelectedSyncScope.SchemaName;
                        }

                        foreach (SyncTableConfigElement tableElement in selectedConfig.SelectedSyncScope.SyncTables)
                        {
                            // Check and set the SchemaName for individual table if specified
                            if (!string.IsNullOrEmpty(tableElement.SchemaName))
                            {
                                prov.Tables[tableElement.GlobalName].ObjectSchema = tableElement.SchemaName;
                            }

                            prov.Tables[tableElement.GlobalName].FilterClause = tableElement.FilterClause;
                            foreach (FilterColumnConfigElement filterCol in tableElement.FilterColumns)
                            {
                                prov.Tables[tableElement.GlobalName].FilterColumns.Add(scopeDescription.Tables[tableElement.GlobalName].Columns[filterCol.Name]);
                            }
                            foreach (FilterParameterConfigElement filterParam in tableElement.FilterParameters)
                            {
                                CheckFilterParamTypeAndSize(filterParam);
                                prov.Tables[tableElement.GlobalName].FilterParameters.Add(new SqlParameter(filterParam.Name, (SqlDbType)Enum.Parse(typeof(SqlDbType), filterParam.SqlType, true)));
                                prov.Tables[tableElement.GlobalName].FilterParameters[filterParam.Name].Size = filterParam.DataSize;
                            }
                        }

                        // enable bulk procedures.
                        prov.SetUseBulkProceduresDefault(selectedConfig.SelectedSyncScope.EnableBulkApplyProcedures);

                        // Create a new set of enumeration stored procs per scope. 
                        // Without this multiple scopes share the same stored procedure which is not desirable.
                        prov.SetCreateProceduresForAdditionalScopeDefault(DbSyncCreationOption.Create);

                        if (selectedConfig.SelectedSyncScope.IsTemplateScope)
                        {
                            if (!prov.TemplateExists(selectedConfig.SelectedSyncScope.Name))
                            {
                                Log("Provisioning Database {0} for template scope {1}...", selectedConfig.SelectedTargetDatabase.Name, selectedConfig.SelectedSyncScope.Name);
                                prov.Apply();
                            }
                            else
                            {
                                throw new InvalidOperationException(string.Format("Database {0} already contains a template scope {1}. Please deprovision the scope and retry.", selectedConfig.SelectedTargetDatabase.Name,
                                    selectedConfig.SelectedSyncScope.Name));
                            }
                        }
                        else
                        {
                            if (!prov.ScopeExists(selectedConfig.SelectedSyncScope.Name))
                            {
                                Log("Provisioning Database {0} for scope {1}...", selectedConfig.SelectedTargetDatabase.Name, selectedConfig.SelectedSyncScope.Name);
                                prov.Apply();
                            }
                            else
                            {
                                throw new InvalidOperationException(string.Format("Database {0} already contains a scope {1}. Please deprovision the scope and retry.", selectedConfig.SelectedTargetDatabase.Name,
                                    selectedConfig.SelectedSyncScope.Name));
                            }
                        }
                    }
                    catch (ConfigurationErrorsException)
                    {
                        throw;
                    }
                    catch (InvalidOperationException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException("Unexpected error when executing the Provisioning command. See inner exception for details.", e);
                    }
                    break;
                case OperationMode.Deprovision:
                    try
                    {
                        SqlSyncScopeDeprovisioning deprov = new SqlSyncScopeDeprovisioning(new SqlConnection(selectedConfig.SelectedTargetDatabase.GetConnectionString()));

                        // Set the ObjectSchema property.
                        if (!String.IsNullOrEmpty(selectedConfig.SelectedSyncScope.SchemaName))
                        {
                            deprov.ObjectSchema = selectedConfig.SelectedSyncScope.SchemaName;
                        }

                        Log("Deprovisioning Database {0} for scope {1}...", selectedConfig.SelectedTargetDatabase.Name, selectedConfig.SelectedSyncScope.Name);

                        if (selectedConfig.SelectedSyncScope.IsTemplateScope)
                        {
                            deprov.DeprovisionTemplate(selectedConfig.SelectedSyncScope.Name);
                        }
                        else
                        {
                            deprov.DeprovisionScope(selectedConfig.SelectedSyncScope.Name);

                        }
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException("Unexpected error when executing the Deprovisioning command. See inner exception for details.", e);
                    }

                    break;
                case OperationMode.Deprovisionstore:
                    try
                    {
                        SqlSyncScopeDeprovisioning deprov = new SqlSyncScopeDeprovisioning(new SqlConnection(selectedConfig.SelectedTargetDatabase.GetConnectionString()));

                        Log("Deprovisioning Store Database {0} ...", selectedConfig.SelectedTargetDatabase.Name);

                        deprov.DeprovisionStore();
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException("Unexpected error when executing the Deprovisioning command. See inner exception for details.", e);
                    }

                    break;
                case OperationMode.Codegen:
                    Log("Generating files...");
                    EntityGenerator generator = EntityGeneratorFactory.Create(parser.CodeGenMode, selectedConfig.SelectedSyncScope.SchemaName);
                    generator.GenerateEntities(parser.GeneratedFilePrefix,
                        string.IsNullOrEmpty(parser.Namespace)
                        ? string.IsNullOrEmpty(parser.GeneratedFilePrefix) ? scopeDescription.ScopeName : parser.GeneratedFilePrefix
                        : parser.Namespace,
                        scopeDescription, tablesToColumnMappingsInfo, parser.WorkingDirectory, parser.Language, null/*serviceUri*/);
                    break;
                default:
                    break;
            }
        }

        private static Dictionary<string, Dictionary<string, string>> BuildColumnMappingInfo(SelectedConfigSections selectedConfig)
        {
            Dictionary<string, Dictionary<string, string>> mappingInfo = new Dictionary<string, Dictionary<string, string>>();
            foreach (SyncTableConfigElement table in selectedConfig.SelectedSyncScope.SyncTables)
            {
                foreach (SyncColumnConfigElement column in table.SyncColumns)
                {
                    // If globalname doesnt match local name then add to mapping
                    if (!column.GlobalName.Equals(column.Name, StringComparison.CurrentCulture))
                    {
                        if (!mappingInfo.ContainsKey(table.GlobalName))
                        {
                            mappingInfo.Add(table.GlobalName, new Dictionary<string, string>());
                        }
                        // Add the mapping info
                        mappingInfo[table.GlobalName].Add(column.Name.ToLowerInvariant(), column.GlobalName);
                    }
                }
            }
            return mappingInfo;
        }

        private static void CheckFilterParamTypeAndSize(FilterParameterConfigElement filterParam)
        {
            // Check that all relavant properties for a SqlParameter are present.

            // Check that name starts with a @
            if (!filterParam.Name.Trim().StartsWith("@", StringComparison.Ordinal))
            {
                throw new ConfigurationErrorsException(string.Format("FilterParameter '{0}' Name property does not start with '@'.", filterParam.Name));
            }

            // First check that SqlType is a valid SqlDbType enum
            if (Enum.GetNames(typeof(SqlDbType)).Where((e) => e.Equals(filterParam.SqlType, StringComparison.OrdinalIgnoreCase)).Count() == 0)
            {
                throw new ConfigurationErrorsException(string.Format("SqlType '{0}' for filter parameter {1} is not a valid SqlDbType enum.", filterParam.SqlType, filterParam.Name));
            }

            // Ensure that the DataSize attribute has a value set for the required types
            SqlDbType type = (SqlDbType)Enum.Parse(typeof(SqlDbType), filterParam.SqlType, true);
            switch (type)
            {
                case SqlDbType.Binary:
                case SqlDbType.Char:
                case SqlDbType.Image:
                case SqlDbType.NChar:
                case SqlDbType.NText:
                case SqlDbType.NVarChar:
                case SqlDbType.Text:
                case SqlDbType.VarBinary:
                case SqlDbType.VarChar:
                    if (filterParam.DataSize <= 0)
                    {
                        throw new ConfigurationErrorsException(string.Format("Filter parameter '{0}' must specify a non zero number for the DataSize attribute.", filterParam.Name));
                    }
                    break;
            }
        }

        private static void LogArgsError(string errorMessage)
        {
            Log(errorMessage);
            Log(ArgsParser.GetHelpString());
        }

        internal static void Log(string message, params object[] args)
        {
            string logMessage;
            if (args.Length > 0)
            {
                logMessage = string.Format(message, args);
            }
            else
            {
                logMessage = message;
            }

            if (LogOccured != null)
                LogOccured(null, new StringEventArgs(logMessage));

            Console.WriteLine(logMessage);
        }

        /// <summary>
        /// Based on the passed in SyncScope element and the TargetDatabase element the system generates a DbSyncScopeDescription element.
        /// </summary>
        /// <param name="selectedConfig">SelectedConfigSection wrapper</param>
        /// <returns>A DbSyncScopeDesceription object</returns>
        private static DbSyncScopeDescription GetDbSyncScopeDescription(SelectedConfigSections selectedConfig)
        {
            DbSyncScopeDescription desc = new DbSyncScopeDescription(selectedConfig.SelectedSyncScope.Name);

            using (SqlConnection conn = new SqlConnection(selectedConfig.SelectedTargetDatabase.GetConnectionString()))
            {
                conn.Open();
                foreach (SyncTableConfigElement table in selectedConfig.SelectedSyncScope.SyncTables)
                {
                    DbSyncTableDescription tableDesc = SqlSyncDescriptionBuilder.GetDescriptionForTable(table.Name, conn);

                    // Ensure all specified columns do belong to the table on the server.
                    foreach (SyncColumnConfigElement colElem in table.SyncColumns)
                    {
                        if (tableDesc.Columns.Where((e) => e.UnquotedName.Equals(colElem.Name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault() == null)
                        {
                            throw new InvalidOperationException(string.Format("Table '{0}' does not have a column '{1}' defined in the target database. Please check your SyncColumn definitions",
                                table.Name, colElem.Name));
                        }
                    }

                    List<DbSyncColumnDescription> columnsToRemove = new List<DbSyncColumnDescription>();

                    // Mark timestamp columns for removal
                    columnsToRemove.AddRange(tableDesc.Columns.Where(
                        e => (e.Type.ToLower() == "timestamp")));

                    if (!table.IncludeAllColumns || table.SyncColumns.Count > 0)
                    {
                        //Users wants a subset of columns. Remove the ones they are not interested in
                        foreach (DbSyncColumnDescription columnDesc in tableDesc.Columns)
                        {
                            SyncColumnConfigElement configElement = table.SyncColumns.Cast<SyncColumnConfigElement>().FirstOrDefault((e) => e.Name.Equals(columnDesc.UnquotedName, StringComparison.InvariantCultureIgnoreCase));
                            if (configElement == null)
                            {
                                // Found a column that was not specified by the user. Remove it
                                columnsToRemove.Add(columnDesc);
                            }
                            else
                            {
                                columnDesc.IsNullable = configElement.IsNullable;
                                columnDesc.IsPrimaryKey = configElement.IsPrimaryKey;
                            }
                        }
                    }

                    // Remove columns marked for removal
                    columnsToRemove.ForEach(e => tableDesc.Columns.Remove(e));

                    // Check to see that columns count is greater than 0
                    if (tableDesc.Columns.Count == 0)
                    {
                        throw new InvalidOperationException(
                            string.Format("SyncTable '{0}' has zero SyncColumns configured for sync. Either set IncludeAllColumns to true or specify atleast one SyncColumn.", table.Name));
                    }

                    // Fill in global name
                    if (!string.IsNullOrEmpty(table.GlobalName))
                    {
                        tableDesc.GlobalName = table.GlobalName;
                    }

                    desc.Tables.Add(tableDesc);
                }
            }

            return desc;
        }

        private static SelectedConfigSections FillDefaults(ArgsParser parser, SyncConfigurationSection syncConfig)
        {
            SelectedConfigSections sections = new SelectedConfigSections();

            if (string.IsNullOrEmpty(parser.ScopeName) && syncConfig.SyncScopes.Count == 1)
            {
                sections.SelectedSyncScope = syncConfig.SyncScopes.Cast<SyncScopeConfigElement>().First();
                parser.ScopeName = sections.SelectedSyncScope.Name;
            }
            else
            {
                sections.SelectedSyncScope = syncConfig.SyncScopes.Cast<SyncScopeConfigElement>().Single((e) => e.Name.Equals(parser.ScopeName, StringComparison.InvariantCultureIgnoreCase));
            }

            if (string.IsNullOrEmpty(parser.TargetDatabaseName) && syncConfig.Databases.Count == 1)
            {
                sections.SelectedTargetDatabase = syncConfig.Databases.Cast<TargetDatabaseConfigElement>().First();
                parser.TargetDatabaseName = sections.SelectedTargetDatabase.Name;
            }
            else
            {
                sections.SelectedTargetDatabase = syncConfig.Databases.Cast<TargetDatabaseConfigElement>().Single((e) => e.Name.Equals(parser.TargetDatabaseName, StringComparison.InvariantCultureIgnoreCase));
            }

            return sections;
        }

        private static void ValidateConfigFile(ArgsParser parser, SyncConfigurationSection syncConfig)
        {
            if (syncConfig == null)
            {
                throw new InvalidOperationException("Unable to parse config file.");
            }

            if (syncConfig.SyncScopes.Count == 0)
            {
                throw new InvalidOperationException("Config file should contain atleast one <SyncScope> definition.");
            }

            if (syncConfig.Databases.Count == 0)
            {
                throw new InvalidOperationException("Config file should contain atleast one <TargetDatabase> definition.");
            }

            if (syncConfig.SyncScopes.Count > 1 && string.IsNullOrEmpty(parser.ScopeName))
            {
                throw new InvalidOperationException("More than one <SyncScope> definitions found. Specify /scopename parameter.");
            }

            if (syncConfig.Databases.Count > 1 && string.IsNullOrEmpty(parser.TargetDatabaseName))
            {
                throw new InvalidOperationException("More than one <TargetDatabase> definitions found. Specify /database parameter.");
            }

            if (!string.IsNullOrEmpty(parser.ScopeName) &&
                syncConfig.SyncScopes.Cast<SyncScopeConfigElement>().FirstOrDefault((e) => e.Name.Equals(parser.ScopeName, StringComparison.InvariantCultureIgnoreCase)) == null)
            {
                throw new ArgumentException("Scopename not found in SyncConfiguration/SyncScope definition.", parser.ScopeName);
            }

            if (!string.IsNullOrEmpty(parser.TargetDatabaseName) &&
                syncConfig.Databases.Cast<TargetDatabaseConfigElement>().FirstOrDefault((e) => e.Name.Equals(parser.TargetDatabaseName, StringComparison.InvariantCultureIgnoreCase)) == null)
            {
                throw new ArgumentException("TargetDatabase not found in SyncConfiguration/Databases definition.", parser.ScopeName);
            }
        }


        private static void ValidateCSDLMode(ArgsParser parser)
        {
            if (parser.OperationMode != OperationMode.Codegen)
            {
                throw new InvalidOperationException("Only /mode:codegen allowed when /url flag is used.");
            }

            if (parser.CodeGenMode == CodeGenTarget.Server)
            {
                throw new InvalidOperationException("/target:server is not a valid target when /url flag is used. Only isclient and client targets allowed.");
            }
        }
    }


    public class StringEventArgs : EventArgs
    {
        public String LogMessage { get; set; }
        public StringEventArgs(string logMessage)
        {
            this.LogMessage = logMessage;
        }
    }
    class SelectedConfigSections
    {
        public SyncScopeConfigElement SelectedSyncScope;
        public TargetDatabaseConfigElement SelectedTargetDatabase;
    }
}
