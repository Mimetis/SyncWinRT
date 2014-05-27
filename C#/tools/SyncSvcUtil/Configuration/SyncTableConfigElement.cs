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
using Microsoft.Synchronization.ClientServices.CodeDom;

namespace Microsoft.Synchronization.ClientServices.Configuration
{
    /// <summary>
    /// ConfigElement for the below mentioned config defintion.
    /// <SyncTable Name="[DatabaseName]" GlobalName="[Global Name]" SchemaName="[Schema name]" IncludeAllColumns="true|false" FilterClause="[Filter clause]"/>
    /// </summary>
    public class SyncTableConfigElement : ConfigurationElement, ICloneable
    {
        /// <summary>
        /// Gets or set the name of the Sync table
        /// </summary>
        [ConfigurationProperty("Name", IsRequired = true, IsKey = true)]
        [StringValidator(InvalidCharacters = "~!@#$%^&*(){}/;'\"|\\")]
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
        /// Gets or sets the GlobalName for the table if different than the backend database name
        /// </summary>
        [ConfigurationProperty("GlobalName", IsRequired = false, DefaultValue = null)]
        [StringValidator(InvalidCharacters = "~!@#$%^&*()[]{}/;'\"|\\")]
        public string GlobalName
        {
            get
            {
                string gName = (string)this["GlobalName"];
                if (string.IsNullOrEmpty(gName))
                {
                    return CodeDomUtility.SanitizeName(this.Name);
                }
                return CodeDomUtility.SanitizeName(gName);
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this["GlobalName"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the Schema name where the Sync side table is to be created. This schema has to exist on the database.
        /// </summary>
        [ConfigurationProperty("SchemaName", IsRequired = false, DefaultValue = null)]
        [StringValidator(InvalidCharacters = "~!@#$%^&*()[]{}/;'\"|\\.")]
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
        /// Gets or sets whether to include all columns of the table or not
        /// </summary>
        [ConfigurationProperty("IncludeAllColumns", IsRequired = false, DefaultValue = true)]
        public bool IncludeAllColumns
        {
            get
            {
                return (bool)this["IncludeAllColumns"];
            }
            set
            {
                this["IncludeAllColumns"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the filter clause to use for the table
        /// </summary>
        [ConfigurationProperty("FilterClause", IsRequired = false, DefaultValue = "")]
        public string FilterClause
        {
            get
            {
                return (string)this["FilterClause"];
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this["FilterClause"] = value;
            }
        }

        /// <summary>
        /// Gets the List of columns that are sync enabled (if IncludeAllColumns is false).
        /// </summary>
        [ConfigurationProperty("SyncColumns", IsDefaultCollection = false)]
        public SyncColumnCollection SyncColumns
        {
            get
            {
                SyncColumnCollection collection = (SyncColumnCollection)base["SyncColumns"];
                return collection;
            }
        }

        /// <summary>
        /// Gets the list of Coloumns if the table is using parameterized filters.
        /// </summary>
        [ConfigurationProperty("FilterColumns", IsDefaultCollection = false)]
        public FilterColumnCollection FilterColumns
        {
            get
            {
                FilterColumnCollection collection = (FilterColumnCollection)base["FilterColumns"];
                return collection;
            }
        }

        /// <summary>
        /// Gets the list of the actual Parameters to use
        /// </summary>
        [ConfigurationProperty("FilterParameters", IsDefaultCollection = false)]
        public FilterParameterCollection FilterParameters
        {
            get
            {
                FilterParameterCollection collection = (FilterParameterCollection)base["FilterParameters"];
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
            SyncTableConfigElement table = new SyncTableConfigElement()
            {
                Name = this.Name,
                FilterClause = this.FilterClause,
                IncludeAllColumns = this.IncludeAllColumns,
            };

            if(!string.IsNullOrEmpty((string)this["GlobalName"]))
            {
                table.GlobalName = (string)this["GlobalName"];
            }
            if (!string.IsNullOrEmpty(this.SchemaName))
            {
                table.GlobalName = this.SchemaName;
            }

            //Clone columns
            foreach (SyncColumnConfigElement column in SyncColumns)
            {
                table.SyncColumns.Add((SyncColumnConfigElement)column.Clone());
            }

            // Clone filter columns
            foreach (FilterColumnConfigElement column in FilterColumns)
            {
                table.FilterColumns.Add((FilterColumnConfigElement)column.Clone());
            }

            // Clone filter parameters
            foreach (FilterParameterConfigElement column in FilterParameters)
            {
                table.FilterParameters.Add((FilterParameterConfigElement)column.Clone());
            }

            return table;
        }

        #endregion
    }
}
