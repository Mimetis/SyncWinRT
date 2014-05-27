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
using System.Data.SqlClient;

namespace Microsoft.Synchronization.ClientServices.Configuration
{
    /// <summary>
    /// ConfigElement representing the following config section.
    /// <TargetDatabase Name="MixProductionDb" DbServer="202.13.13.45" DbName="Mix" UserName="sync" Password="syncpwd"/>
    /// </summary>
    public class TargetDatabaseConfigElement : ConfigurationElement, ICloneable
    {
        /// <summary>
        /// Get or Set the name of the Database
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
        /// Get or set the name of the backend SQL server
        /// </summary>
        [ConfigurationProperty("DbServer", IsRequired = true, IsKey = true)]
        [StringValidator(InvalidCharacters = "~!#$%^&*{}/;'\"|")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Db", Justification = "Not Required")]
        public string DbServer
        {
            get
            {
                return (string)this["DbServer"];
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this["DbServer"] = value;
            }
        }

        /// <summary>
        /// Get or set the Database name in the backend Sql server
        /// </summary>
        [ConfigurationProperty("DbName", IsRequired = true, IsKey = true)]
        [StringValidator(InvalidCharacters = "~!@#$%^&*(){}/;'\"|\\")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Db", Justification = "Not required")]
        public string DbName
        {
            get
            {
                return (string)this["DbName"];
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this["DbName"] = value;
            }
        }

        /// <summary>
        /// Get or set the Username to use to connect to the backend database
        /// </summary>
        [ConfigurationProperty("UserName", IsRequired = false, IsKey = true)]
        [StringValidator(InvalidCharacters = "~!#$%^&*(){}/;'\"|\\")]
        public string UserName
        {
            get
            {
                return (string)this["UserName"];
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this["UserName"] = value;
            }
        }

        /// <summary>
        /// Get or set the password to use to connect to the backend database
        /// </summary>
        [ConfigurationProperty("Password", IsRequired = false, IsKey = true)]
        public string Password
        {
            get
            {
                return (string)this["Password"];
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this["Password"] = value;
            }
        }

        /// <summary>
        /// Get or set the bool flag that denotes whether to use windows auth to connect to the backend database
        /// </summary>
        [ConfigurationProperty("UseIntegratedAuth", IsRequired = false, DefaultValue = false)]
        public bool UseIntegratedAuth
        {
            get
            {
                return (bool)this["UseIntegratedAuth"];
            }
            set
            {
                this["UseIntegratedAuth"] = value;
            }
        }

        /// <summary>
        /// Returns a SqlConnection string based on passed inputs
        /// </summary>
        /// <returns>Connection string</returns>
        public string GetConnectionString()
        {
            SqlConnectionStringBuilder connBuilder = new SqlConnectionStringBuilder();
            connBuilder.DataSource = this.DbServer;
            connBuilder.InitialCatalog = this.DbName;
            connBuilder.UserID = this.UserName;
            connBuilder.Password = this.Password;
            if(this.UseIntegratedAuth)
            {
                connBuilder.IntegratedSecurity = true;
            }
            return connBuilder.ConnectionString;
        }

        #region ICloneable Members

        /// <summary>
        /// Returns a clone of the config element
        /// </summary>
        /// <returns>Clone element</returns>
        public object Clone()
        {
            return new TargetDatabaseConfigElement()
            {
                Name = this.Name,
                Password = this.Password,
                UserName = this.UserName,
                UseIntegratedAuth = this.UseIntegratedAuth,
                DbName = this.DbName,
                DbServer = this.DbServer
            };
        }

        #endregion
    }
}
