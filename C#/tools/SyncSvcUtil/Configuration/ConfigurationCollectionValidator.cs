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
    /// Helps validate a collection against a specified min and max count.
    /// </summary>
    public class ConfigurationCollectionValidator : ConfigurationValidatorBase
    {
        private uint minCount;
        private uint maxCount;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="minCount">Mininmum number of elements the collection can have.</param>
        /// <param name="maxCount">Maximum number of elements the collection can have.</param>
        /// 
        public ConfigurationCollectionValidator(uint minCount, uint maxCount)
        {
            this.minCount = minCount;
            this.maxCount = maxCount;
        }

        /// <summary>
        /// Validate that the number of elements in the collection are within the range.
        /// </summary>
        /// <param name="value">The configuration collection to validate.</param>
        /// 
        public override void Validate(object value)
        {
            ConfigurationElementCollection collection = value as ConfigurationElementCollection;
            if (collection != null)
            {
                if (collection.Count < minCount)
                {
                    throw new ArgumentException("The collection should contain atleast " + minCount + " entries.");
                }
                if (collection.Count > maxCount)
                {
                    throw new ArgumentException("The collection should not contain more than " + maxCount + " entries.");
                }
            }
        }

        /// <summary>
        /// Check if the passed in type can be validated.
        /// </summary>
        /// <param name="type">The type to check</param>
        /// 
        public override bool CanValidate(Type type)
        {
            return typeof(ConfigurationElementCollection).IsAssignableFrom(type);
        }
    }
}
