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
using System.Configuration;

namespace Microsoft.Synchronization.ClientServices.Configuration
{
    /// <summary>
    /// ConfigElement for the below mentioned config defintion.
    /// <SyncScope Name ="ScopeName">
    ///   <SyncTables>
    ///     <clear/>
    ///     <add/>
    ///   </SyncTables>
    /// </SyncScope>
    /// </summary>
    public class SyncScopeConfigElement : ConfigurationElement, ICloneable
    {
        /// <summary>
        /// Represents the name of the SyncScope
        /// </summary>
        [ConfigurationProperty("Name", IsRequired = true, IsKey = true)]
        [StringValidator(InvalidCharacters = "~!@#$%^&*()[]{}/;'\"|\\")]
        public string Name
        {
            get
            {
                return (string)this["Name"];
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this["Name"] = value;
            }
        }

        /// <summary>
        /// Represents the sql Schema name in which all sync related sidetables and procedures will be created.
        /// Schema must pre-exist on the target database.
        /// </summary>
        [ConfigurationProperty("SchemaName", IsKey = false)]
        [StringValidator(InvalidCharacters = "~!@#$%^&*()[]{}/;'\"|\\")]
        public string SchemaName
        {
            get
            {
                return (string)this["SchemaName"];
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this["SchemaName"] = value;
            }
        }

        /// <summary>
        /// Represents whether this scope is a template scope or a static scope definition.
        /// </summary>
        [ConfigurationProperty("IsTemplateScope")]
        public bool IsTemplateScope
        {
            get
            {
                return (bool)this["IsTemplateScope"];
            }
            set
            {
                this["IsTemplateScope"] = value;
            }
        }

        /// <summary>
        /// Represents where the service will use TVP's to perform bulk application for applying Sync changes or not. 
        /// Supported only on Sql Server 2008 and higher SKU'. Default is true
        /// </summary>
        [ConfigurationProperty("EnableBulkApplyProcedures", IsRequired = false, DefaultValue = true)]
        public bool EnableBulkApplyProcedures
        {
            get
            {
                return (bool)this["EnableBulkApplyProcedures"];
            }
            set
            {
                this["EnableBulkApplyProcedures"] = value;
            }
        }

        /// <summary>
        /// Collection of the tables that belong to this scope.
        /// </summary>
        [ConfigurationProperty("SyncTables", IsDefaultCollection = false)]
        public SyncTableCollection SyncTables
        {
            get
            {
                SyncTableCollection collection = (SyncTableCollection)base["SyncTables"];
                return collection;
            }
        }

        #region ICloneable Members

        /// <summary>
        /// Returns a clone of the existing Config section
        /// </summary>
        /// <returns>Cloned object</returns>
        public object Clone()
        {
            SyncScopeConfigElement config = new SyncScopeConfigElement()
            {
                Name = this.Name,
                IsTemplateScope = this.IsTemplateScope,
                EnableBulkApplyProcedures = this.EnableBulkApplyProcedures
            };

            if (!string.IsNullOrEmpty(this.SchemaName))
            {
                config.SchemaName = this.SchemaName;
            }

            foreach (SyncTableConfigElement table in SyncTables)
            {
                config.SyncTables.Add((SyncTableConfigElement)table.Clone());
            }
            return config;
        }

        #endregion
    }
}
