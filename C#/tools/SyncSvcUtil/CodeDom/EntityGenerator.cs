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
using System.CodeDom;
using Microsoft.Synchronization.Data;
using System.IO;

namespace Microsoft.Synchronization.ClientServices.CodeDom
{
    abstract class EntityGenerator
    {
        public abstract void GenerateEntities(string filePrefix, string nameSpace, DbSyncScopeDescription desc, Dictionary<string, Dictionary<string, string>> colsMappingInfo,
            DirectoryInfo dirInfo, CodeLanguage option, string serviceUri);
    }
}
