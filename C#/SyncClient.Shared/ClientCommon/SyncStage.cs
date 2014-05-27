using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Synchronization.ClientServices
{
    public enum SyncStage
    {
        StartingSync,
        ReadingConfiguration,
        ApplyingConfiguration,
        CreatingScope,
        CheckingTables,
        UploadingChanges,
        DownloadingChanges,
        ApplyingChanges,
        GetChanges,
        EndingSync
    }
}
