using Microsoft.Synchronization.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncServiceLibTest
{
    [SyncEntityType(TableGlobalName="TestEntityTables", TableLocalName="[TestEntityTables]", KeyFields="Id")]
    public class TestEntityTable
    {
        private int _Id;

        public int Id
        {
            get { return _Id; }
            set { _Id = value; }
        }
        
    }
}
