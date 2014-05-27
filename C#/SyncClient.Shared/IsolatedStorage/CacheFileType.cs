using System;
using System.Net;
using System.Windows;

namespace Microsoft.Synchronization.ClientServices.IsolatedStorage
{
    internal enum CacheFileType
    {
        DownloadResponse,
        SaveChanges,
        UploadResponse,
        Conflicts,
        Errors,
        Archive,
        Unknown
    }
}
