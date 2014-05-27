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
using Microsoft.Synchronization.ClientServices.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SyncSvcUtilUI
{
    internal class WizardHelper
    {
        static WizardHelper _instance = null;

        public static WizardHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new WizardHelper();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Property bag used by the config file wizard
        /// </summary>
        public Dictionary<string, string> ConfigFileWizardHelper = new Dictionary<string, string>();

        /// <summary>
        /// Property bag used by the provisioning wizard
        /// </summary>
        public Dictionary<string, string> ProvisioningWizardHelper = new Dictionary<string, string>();

        /// <summary>
        /// Property bag used by the code gen wizard
        /// </summary>
        public Dictionary<string, string> CodeGenWizardHelper = new Dictionary<string, string>();

        public static string SanitizeName(string name)
        {
            return name.Replace(' ', '_').Replace('.', '_');
        }

        public SyncConfigurationSection SyncConfigSection = null;

        public const string CONFIG_FILE_NAME = "Config File Name";

        public const string SELECTED_CONFIG_NAME = "Selected Config";

        public const string SELECTED_DB_NAME = "Selected Database";

        public const string SELECTED_PROV_MODE = "Selected Mode";

        public const string SELECTED_CODEGEN_SOURCE = "Codegen source";

        public const string CONFIG_FILE_CODEGEN_SOURCE = "ConfigFile";

        public const string CSDL_CODEGEN_SOURCE = "CSDL";

        public const string CSDL_CODEGEN_URL = "CSDL Uri";

        public const string CODEGEN_LANGUAGE = "Codegen Language";

        public const string CODEGEN_NAMESPACE= "Codegen Namespace";

        public const string CODEGEN_OUTPREFIX = "Codegen Outprefix";

        public const string CODEGEN_TARGET = "Codegen Target";

        public const string CODEGEN_OUTDIRECTORY = "Codegen Directory";

        public const string SELECT_TABLENAMES_QUERY = "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";

        public const string SELECT_ALL_ROWS_QUERY = "select * from {0}";

        public static string[] SYNC_TABLE_NAMES = new string[] { "schema_info", "scope_info", "scope_config", "scope_templates", "scope_parameters" };

        public const string PROV_PARAM_FORMAT = "/scopeconfig:{0} /mode:{1} /scopename:{2} /database:{3} ";

        public const string CONFIG_CODEGEN_PARAM_FORMAT = "/scopeconfig:{0} /scopename:{1} /database:{2} ";
        
        public const string CSDL_CODEGEN_PARAM_FORMAT = "/url:{0} /scopename:{1} ";

        public const string CODEGEN_COMMON_PARAMS_FORMAT = "/language:{0} /namespace:{1} /mode:codegen /target:{2} /directory:{3}";

        public const string CODEGEN_OUTPREFIX_PARAM_FORMAT = "{0} /outprefix:{1}";

        public const string CSDL_CODEGEN_URL_FORMAT = "{0}";

        public static string ExecuteProcessAndReturnLog(string inputParam)
        {
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo("SyncSvcUtil.exe", inputParam);
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            ManualResetEvent wait = new ManualResetEvent(false);
            process.StartInfo.UseShellExecute = false;
            process.Start();

            // Wait for process to complete
            process.WaitForExit();

            // Return the standard output
            return process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        }
    }
}
