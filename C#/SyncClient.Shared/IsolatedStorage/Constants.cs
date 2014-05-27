using System;
using System.Net;
using System.Windows;
using System.Text.RegularExpressions;

namespace Microsoft.Synchronization.ClientServices.IsolatedStorage
{
    /// <summary>
    /// Contains contants used by the context
    /// </summary>
    internal static class Constants
    {
        // File name used for storing scopeName info
        public static readonly string SCOPE_INFO = "scopeinfo.txt";

        // File name used for lock for the cache path
        public static readonly string LOCKFILE = "lock.txt";

        private static readonly Regex regex = new Regex("^[a-fA-F0-9]{8}[.]((-?[0-9]+[.][CE])|([ADSU]))$");

        public static readonly int TIMER_MINUTES_INTERVAL = 1;
        

        // Returns whether or not the requested file is one of the special files.
        public static bool SpecialFile(string fileName)
        {
            return fileName == SCOPE_INFO || fileName == LOCKFILE;
        }

        // Returns whether or not the file name is one of ours.
        public static bool IsCacheFile(string fileName)
        {
            return regex.IsMatch(fileName);
        }
    }
}                                                                
