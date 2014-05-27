using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Synchronization.Services.Formatters
{
    enum ReaderItemType
    {
        BOF,
        Entry,
        SyncBlob,
        HasMoreChanges,
        EOF
    }
}
