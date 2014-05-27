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
    /// Validate that the number of elements in the collection are within the range.
    /// </summary>
    /// 
    [AttributeUsage(AttributeTargets.Property)]
    public class ConfigurationCollectionValidatorAttribute : ConfigurationValidatorAttribute
    {
        private uint minCount = 0;
        private uint maxCount = uint.MaxValue;

        /// <summary>
        /// Minimum occurrences that are allowed witin a collection.
        /// </summary>
        /// 
        public uint MinOccurences
        {
            get
            {
                return minCount;
            }
            set
            {
                if (value > maxCount)
                {
                    throw new ArgumentOutOfRangeException("value", "value cannot be greater than MaxOccurences");
                }
                this.minCount = value;
            }
        }

        /// <summary>
        /// Maximum occurrences that are allowed witin a collection.
        /// </summary>
        /// 
        public uint MaxOccurences
        {
            get
            {
                return maxCount;
            }
            set
            {
                if (value < minCount)
                {
                    throw new ArgumentOutOfRangeException("value", "value cannot be less than MinOccurences");
                }
                this.maxCount = value;
            }
        }

        /// <summary>
        /// Validation class to use.
        /// </summary>
        /// 
        public override ConfigurationValidatorBase ValidatorInstance
        {
            get
            {
                return new ConfigurationCollectionValidator(minCount, maxCount);
            }
        }
    }
}
