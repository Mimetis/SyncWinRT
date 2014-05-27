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
    /// Represents the FilterParameter element in the config file
    /// </summary>
    public class FilterParameterConfigElement : ConfigurationElement, ICloneable
    {
        /// <summary>
        /// Gets or sets the Name of the filter parameter. Name must be prefixed with the the @ character
        /// </summary>
        [ConfigurationProperty("Name", IsRequired = true, IsKey = true)]
        [StringValidator(InvalidCharacters = "~!#$%^&*()[]{}/;'\"|\\")]
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
        /// Gets or sets the SqlType of the parameter
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
        /// Gets or sets the size of variable sized type
        /// </summary>
        [ConfigurationProperty("DataSize", IsRequired = false)]
        public int DataSize
        {
            get
            {
                return (int)this["DataSize"];
            }
            set
            {
                this["DataSize"] = value;
            }
        }

        #region ICloneable Members

        /// <summary>
        /// Returns a clone of the existing Config section
        /// </summary>
        /// <returns>Cloned object</returns>
        public object Clone()
        {
            return new FilterParameterConfigElement()
            {
                Name = this.Name,
                DataSize = this.DataSize,
                SqlType = this.SqlType
            };
        }

        #endregion
    }
}
