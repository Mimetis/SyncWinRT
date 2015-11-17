using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncServiceLibTest
{
    [Microsoft.Synchronization.Services.SyncScope()]
    public class TestEntity
    {
        private ICollection<TestEntityTable> _TestEntityTables;
    }
}
