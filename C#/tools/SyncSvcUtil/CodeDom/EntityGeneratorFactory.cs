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

namespace Microsoft.Synchronization.ClientServices.CodeDom
{
    class EntityGeneratorFactory
    {
        /// <summary>
        /// Create a new instance of EntityGenerator implementation.
        /// </summary>
        /// <param name="target">Target for which to generate code.</param>
        /// <param name="syncObjectSchema">Schema under which the sync related objects are created in the SQL database. 
        /// This can be null when generating non-server code.</param>
        /// <returns></returns>
        public static EntityGenerator Create(CodeGenTarget target, string syncObjectSchema)
        {
            switch (target)
            {
                case CodeGenTarget.Server:
                default:
                    return new ServiceEntityGenerator(syncObjectSchema);
                case CodeGenTarget.ISClient:
                    return new IsolatedStoreClientEntityGenerator();
                case CodeGenTarget.Client:
                    return new GenericClientEntityGenerator();
                case CodeGenTarget.SQLiteClient:
                    return new SQLiteClientEntityGenerator();

            }
        }
    }
}
