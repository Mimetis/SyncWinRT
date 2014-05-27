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
using System.IO;
using System.Reflection;

namespace Microsoft.Synchronization.ClientServices
{
    /// <summary>
    /// Utility class to parse the input arguments
    /// </summary>
    class ArgsParser
    {
        static string _helpString;

        DirectoryInfo _workingDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
        OperationMode _operationMode;
        string _configFile;
        string _scopeName;
        string _targetDatabaseName;
        CodeLanguage _language;
        CodeGenTarget _codeGenMode;
        string _generateFilePrefix = string.Empty;
        string _namespace = string.Empty;
        bool _modeSpecified = false;
        bool _helpRequested = false;
        string _csdlUri = null;
        bool _verboseEnabled = false;

        internal ArgsParser() { }

        public bool HelpRequested
        {
            get { return _helpRequested; }
        }

        public string GeneratedFilePrefix
        {
            get { return _generateFilePrefix; }
        }

        public OperationMode OperationMode
        {
            get { return _operationMode; }
        }

        public string ConfigFile
        {
            get { return _configFile; }
        }

        public bool UseCSDLUrl
        {
            get { return this._csdlUri != null; }
        }

        public bool UseVerbose
        {
            get
            {
                return this._verboseEnabled;
            }
        }

        public string CSDLUrl
        {
            get
            {
                return this._csdlUri;
            }
        }

        public string ScopeName
        {
            get { return _scopeName; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException(value);
                }
                this._scopeName = value;
            }
        }

        public string TargetDatabaseName
        {
            get { return _targetDatabaseName; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException(value);
                }
                this._targetDatabaseName = value;
            }
        }

        public CodeLanguage Language
        {
            get { return _language; }
        }

        public CodeGenTarget CodeGenMode
        {
            get { return _codeGenMode; }
        }

        public DirectoryInfo WorkingDirectory
        {
            get { return _workingDirectory; }
        }

        public bool ModeSpecified
        {
            get { return _modeSpecified; }
        }

        public string Namespace
        {
            get { return _namespace; }
        }

        public static String GetHelpString()
        {
            if (String.IsNullOrEmpty(_helpString))
            {
                StringBuilder builder = new StringBuilder(Constants.AssemblyName);
                builder.AppendLine(" Params: [/?] [/directory:out-directory] /mode:operation-mode [/target:codegen-target] (/scopeconfig:configfile | /url:serviceCSDLurl) [/scopename:scope-name] [/database:target-database] [/language:codegen-language] [/outprefix:filenameprefix] [/verbose]");
                builder.AppendLine("Param Details:");
                builder.AppendLine("\t/?           : Print this help message");
                builder.AppendLine("\t/directory   : Working directory. Default is current directory");
                builder.AppendLine("\t/scopeconfig : XML file containing the SyncConfiguration entries.");
                builder.AppendLine("\t/url         : A URL for the Sync Scope Schema CSDL document.");
                builder.AppendLine("\t/scopename   : Name of <SyncScope> element. Required if more than one <SyncScope> entry is present.");
                builder.AppendLine("\t/database    : Name of <TargetDatabase> element. Required if more than one <TargetDatabase> entry is present.");
                builder.AppendLine("\t/language    : Code generation language. Options are CS and VB. Default is CS.");
                builder.AppendLine("\t/mode        : Operation mode of tool. Options are Provision, Deprovision, Deprovisionstore and CodeGen. Required parameter.");
                builder.AppendLine("\t/target      : Target for which code is being generated. Options are server, isclient and client.");
                builder.AppendLine("\t\tserver - This will generate the .SVC and an Entities.cs file that represents the Sync Service and the types objects for that service.");
                builder.AppendLine("\t\tisclient - This will generate an Silverlight client context, that uses the isolated storage as the storage layer, and its corresponding entities.");
                builder.AppendLine("\t\tSQLiteClient - This will generate an SQLite client context for Windows 8 application, that uses the SQLite Database as the storage layer, and its corresponding entities.");
                builder.AppendLine("\t\tclient - This will generate just the entities that can be used with any custom silverlight storage layer.");
                builder.AppendLine("\t/outprefix   : Prefix name for all generated files. Default is the value of /scopename entry.");
                builder.AppendLine("\t/namespace   : Namespace for generated types. Default is the value of /scopename entry.");
                builder.AppendLine("\t/verbose     : Emit verbose information.");

                _helpString = builder.ToString();
            }
            return _helpString;
        }

        public static ArgsParser ParseArgs(string[] args)
        {
            ArgsParser parser = new ArgsParser();

            foreach (string param in args)
            {
                string[] tokens = param.Split(new char[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);

                if (tokens.Count() != 2  && 
                    !tokens[0].Equals("/?", StringComparison.InvariantCultureIgnoreCase) && 
                    !tokens[0].Equals("/verbose", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new ArgumentException("Invalid parameter passed", param);
                }
                string token1 = tokens.Count() == 2 ? tokens[1] : string.Empty;

                switch (tokens[0].ToLowerInvariant())
                {
                    case "/mode":
                        parser._modeSpecified = true;
                        if (!EnumUtils.TryEnumParse<OperationMode>(token1, out parser._operationMode))
                        {
                            throw new ArgumentOutOfRangeException(param, string.Format("Invalid {0} option specified.", tokens[0]));
                        }
                        break;
                    case "/target":
                        if (!EnumUtils.TryEnumParse<CodeGenTarget>(token1, out parser._codeGenMode))
                        {
                            throw new ArgumentOutOfRangeException(param, string.Format("Invalid {0} option specified.", tokens[0]));
                        }
                        break;
                    case "/language":
                        if (!EnumUtils.TryEnumParse<CodeLanguage>(token1, out parser._language))
                        {
                            throw new ArgumentOutOfRangeException(param, string.Format("Invalid {0} option specified.", tokens[0]));
                        }
                        break;
                    case "/scopeconfig":
                        if (parser._csdlUri != null)
                        {
                            throw new InvalidOperationException("Cannot specify both /scopeconfig and /url option.");
                        }
                        parser._configFile = token1;
                        break;
                    case "/url":
                        if (parser._configFile != null)
                        {
                            throw new InvalidOperationException("Cannot specify both /scopeconfig and /url option.");
                        }
                        parser._csdlUri = token1;
                        if (!new Uri(parser._csdlUri).IsAbsoluteUri)
                        {
                            throw new ArgumentException(string.Format("Sync scope schema Uri cannot be relative."), param);
                        }
                        break;
                    case "/scopename":
                        parser._scopeName = token1;
                        break;
                    case "/database":
                        parser._targetDatabaseName = token1;
                        break;
                    case "/outprefix":
                        parser._generateFilePrefix = token1;
                        break;
                    case "/directory":
                        parser._workingDirectory = new DirectoryInfo(token1);
                        break;
                    case "/namespace":
                        parser._namespace = token1;
                        break;
                    case "/verbose":
                        parser._verboseEnabled = true;
                        break;
                    case "/?":
                        parser._helpRequested = true;
                        Console.WriteLine(GetHelpString());
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(param);
                }                
            }

            return parser;
        }
    }
}
