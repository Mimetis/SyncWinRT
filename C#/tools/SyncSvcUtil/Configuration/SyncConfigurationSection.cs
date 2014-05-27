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
    /// Represents the SyncConfiguration section
    /// </summary>
    public class SyncConfigurationSection : ConfigurationSection, ICloneable
    {
        /// <summary>
        /// Gets the list of SyncScope's configured
        /// </summary>
        [ConfigurationProperty("SyncScopes", IsDefaultCollection = true, IsRequired = true)]
        [ConfigurationCollectionValidator(MinOccurences = 1)]
        public SyncScopeCollection SyncScopes
        {
            get
            {
                SyncScopeCollection collection = (SyncScopeCollection)base["SyncScopes"];
                return collection;
            }
        }

        /// <summary>
        /// Gets the list of TargetDatabase sections
        /// </summary>
        [ConfigurationProperty("Databases", IsDefaultCollection = false, IsRequired = true)]
        [ConfigurationCollectionValidator(MinOccurences = 1)]
        public TargetDatabaseCollection Databases
        {
            get
            {
                TargetDatabaseCollection collection = (TargetDatabaseCollection)base["Databases"];
                return collection;
            }
        }

        /// <summary>
        /// Returns the section serialized a XML
        /// </summary>
        /// <returns>XML representation of the string</returns>
        public override string ToString()
        {
            return base.SerializeSection(this, "SyncConfiguration", ConfigurationSaveMode.Full);
        }

        #region ICloneable Members

        /// <summary>
        /// Returns a clone of the existing Config section
        /// </summary>
        /// <returns>Cloned object</returns>
        public object Clone()
        {
            SyncConfigurationSection section = new SyncConfigurationSection();

            // Clonet TargetDatabases
            foreach (TargetDatabaseConfigElement elem in Databases)
            {
                section.Databases.Add((TargetDatabaseConfigElement)elem.Clone());
            }

            // Clone SyncConfigs
            foreach (SyncScopeConfigElement elem in SyncScopes)
            {
                section.SyncScopes.Add((SyncScopeConfigElement)elem.Clone());
            }
            return section;
        }

        #endregion
    }
}
