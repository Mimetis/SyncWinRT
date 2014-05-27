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
    /// Represents a FilterColumn config element
    /// </summary>
    public class FilterColumnConfigElement : ConfigurationElement, ICloneable
    {
        /// <summary>
        /// Gets or sets the Name of the filter column
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

        #region ICloneable Members

        /// <summary>
        /// Returns a clone of the existing Config section
        /// </summary>
        /// <returns>Cloned object</returns>
        public object Clone()
        {
            return new FilterColumnConfigElement()
            {
                Name = this.Name,
            };
        }

        #endregion
    }
}
