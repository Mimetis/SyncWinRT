// Copyright © Microsoft Corporation. All rights reserved.

// Microsoft Limited Permissive License (Ms-LPL)

// This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.

// 1. Definitions
// The terms “reproduce,” “reproduction,” “derivative works,” and “distribution” have the same meaning here as under U.S. copyright law.
// A “contribution” is the original software, or any additions or changes to the software.
// A “contributor” is any person that distributes its contribution under this license.
// “Licensed patents” are a contributor’s patent claims that read directly on its contribution.

// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any contributors’ name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
// (E) The software is licensed “as-is.” You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.
// (F) Platform Limitation- The licenses granted in sections 2(A) & 2(B) extend only to the software or derivative works that you create that run on a Microsoft Windows operating system product.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace Microsoft.Synchronization.Services
{
    /// <summary>
    /// This class writes trace output for the sync service.
    /// </summary>
    public static class SyncServiceTracer
    {
        #region Members

        private static readonly TraceSwitch _traceSwitch = new TraceSwitch("SyncServiceTracer", null);
        private const string DATE_FORMAT = "MM/dd/yyyy HH:mm:ss";

        #endregion

        #region Methods

        /// <summary>
        /// Output info trace
        /// </summary>
        public static void TraceInfo(string format, params object[] args)
        {
            if (_traceSwitch.TraceInfo)
            {
                TraceLine(TraceLevel.Info, FormatErrorString(args, format));
            }
        }

        /// <summary>
        /// Output error trace
        /// </summary>
        public static void TraceError(string format, params object[] args)
        {
            if (_traceSwitch.TraceError)
            {
                TraceLine(TraceLevel.Error, FormatErrorString(args, format));
            }
        }

        /// <summary>
        /// Output warning trace
        /// </summary>
        public static void TraceWarning(string format, params object[] args)
        {
            if (_traceSwitch.TraceWarning)
            {
                TraceLine(TraceLevel.Warning, FormatErrorString(args, format));
            }
        }

        /// <summary>
        /// Output verbose trace
        /// </summary>
        public static void TraceVerbose(string format, params object[] args)
        {
            if (_traceSwitch.TraceVerbose)
            {
                TraceLine(TraceLevel.Verbose, FormatErrorString(args, format));
            }
        }

        private static void TraceLine(TraceLevel traceLevel, string message)
        {
            string traceLevelString;

            switch (traceLevel)
            {
                case TraceLevel.Error:
                    traceLevelString = "ERROR  ";
                    break;
                case TraceLevel.Warning:
                    traceLevelString = "WARNING";
                    break;
                case TraceLevel.Info:
                    traceLevelString = "INFO   ";
                    break;
                case TraceLevel.Verbose:
                    traceLevelString = "VERBOSE";
                    break;
                case TraceLevel.Off:
                    return;
                default:
                    traceLevelString = "DEFAULT";
                    break;
            }

            string finalMessage = string.Format(
                CultureInfo.InvariantCulture,
                "{0}, {1}, {2}, {3}", traceLevelString, Thread.CurrentThread.ManagedThreadId,
                DateTime.UtcNow.ToString(DATE_FORMAT, CultureInfo.InvariantCulture), message);


            try
            {
                Trace.WriteLine(finalMessage);
            }
            catch 
            {
                // Ignore tracing exceptions
            }
        }

        private static string FormatErrorString(object[] args, string format)
        {
            if (null != args && args.Length > 0)
            {
                return String.Format(CultureInfo.InvariantCulture, format, args);
            }
            
            return format;
        }

        #endregion
    }
}
