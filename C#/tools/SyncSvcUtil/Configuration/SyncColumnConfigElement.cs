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
    /// ConfigElement for below mentioned config entry
    ///  <add Name="[ColName]" GlobalName="[GlobalName]" IsPrimaryKey="true|false" IsNullable="true|false" SqlType="[SqlType]"/>
    /// </summary>
    public class SyncColumnConfigElement : ConfigurationElement, ICloneable
    {
        /// <summary>
        /// Gets or sets the name of the Sync column
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
        /// Gets or sets the Global name to use if different from the backend name
        /// </summary>
        [ConfigurationProperty("GlobalName", IsRequired = false, DefaultValue = null)]
        [StringValidator(InvalidCharacters = "~!@#$%^&*()[]{}/;'\"|\\")]
        public string GlobalName
        {
            get
            {
                return (string)this["GlobalName"];
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
        /// Gets or sets the SQL Type of the column
        /// </summary>
        [ConfigurationProperty("SqlType", IsRequired = true)]
        public string SqlType
        {
            get
            {
                return (string)this["SqlType"];
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this["SqlType"] = value;
            }
        }

        /// <summary>
        /// Bool denoting whether or not this column is a primary key
        /// </summary>
        [ConfigurationProperty("IsPrimaryKey", IsRequired = false, DefaultValue = false)]
        public bool IsPrimaryKey
        {
            get
            {
                return (bool)this["IsPrimaryKey"];
            }
            set
            {
                this["IsPrimaryKey"] = value;
            }
        }

        /// <summary>
        /// Bool denoting whether this column can hold null values in the backened database
        /// </summary>
        [ConfigurationProperty("IsNullable", IsRequired = false, DefaultValue = true)]
        public bool IsNullable
        {
            get
            {
                return (bool)this["IsNullable"];
            }
            set
            {
                this["IsNullable"] = value;
            }
        }

        #region ICloneable Members

        /// <summary>
        /// Returns a clone of the existing Config section
        /// </summary>
        /// <returns>Cloned object</returns>
        public object Clone()
        {
            SyncColumnConfigElement syncCol = new SyncColumnConfigElement()
            {
                Name = this.Name,
                IsNullable = this.IsNullable,
                IsPrimaryKey = this.IsPrimaryKey,
                SqlType = this.SqlType
            };

            if(!string.IsNullOrEmpty(this.GlobalName))
            {
                syncCol.GlobalName = this.GlobalName;
            }
            return syncCol;
        }

        #endregion
    }
}
