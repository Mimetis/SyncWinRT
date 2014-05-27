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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace Microsoft.Synchronization.Services.Batching
{
    /// <summary>
    /// An implementation of a file based batch handler that is capable of saving and retriving change batches.
    /// 
    /// Files are cleaned up as they are retrieved. Otherwise the general guideline is to implement a job which periodically 
    /// cleans up files based on the age.
    /// </summary>
    internal class FileBasedBatchHandler : IBatchHandler
    {
        const string BATCH_HEADER_FILENAME = "header.sync";
        private readonly string _batchSpoolDirectory;

        #region Constructor

        internal FileBasedBatchHandler(string spoolDirectory)
        {
            _batchSpoolDirectory = spoolDirectory;
        }

        #endregion

        #region IBatchHandler Implementation

        /// <summary>
        /// Save batches and the header.
        /// </summary>
        /// <param name="batchList">BatchList to save</param>
        /// <param name="header">Header information</param>
        public void SaveBatches(List<Batch> batchList, BatchHeader header)
        {
            // Create the batch directory.
            var path = Path.Combine(_batchSpoolDirectory, header.BatchCode.ToString());
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            foreach (var b in batchList)
            {
                string filePath = Path.Combine(path, b.FileName);
                var formatter = new BinaryFormatter();
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
                {
                    formatter.Serialize(fileStream, b);
                    fileStream.Flush();
                }
            }

            string headerFilePath = Path.Combine(path, BATCH_HEADER_FILENAME);
            var headerFormatter = new BinaryFormatter();
            using (var fileStream = new FileStream(headerFilePath, FileMode.Create, FileAccess.ReadWrite))
            {
                headerFormatter.Serialize(fileStream, header);
                fileStream.Flush();
            }
        }

        /// <summary>
        /// Gets the next batch in a sequence.
        /// </summary>
        /// <param name="batchCode">Batch code value</param>
        /// <param name="nextBatchSequenceNumber">Sequence number of the next batch</param>
        /// <returns>Batch information</returns>
        public Batch GetNextBatch(Guid batchCode, Guid nextBatchSequenceNumber)
        {
            // Create the batch directory.
            var path = Path.Combine(_batchSpoolDirectory, batchCode.ToString());
            if (!Directory.Exists(path))
            {
                // no batch found.
                return null;
            }

            string headerFilePath = Path.Combine(path, BATCH_HEADER_FILENAME);
            BatchHeader header = null;
            if (File.Exists(headerFilePath))
            {
                var headerFormatter = new BinaryFormatter();
                using (var fileStream = new FileStream(headerFilePath, FileMode.Open, FileAccess.Read))
                {
                    header = headerFormatter.Deserialize(fileStream) as BatchHeader;
                }
            }

            if (null == header)
            {
                // no header
                return null;
            }

            // Get the batch file name from the header.
            string batchFile = header.BatchFileNames.Where(b => new Guid(b) == nextBatchSequenceNumber).FirstOrDefault();

            if (String.IsNullOrEmpty(batchFile))
            {
                // missing batch file.
                return null;
            }

            string batchFilePath = Path.Combine(path, batchFile);
            Batch batch = null;

            if (File.Exists(batchFilePath))
            {
                var formatter = new BinaryFormatter();
                using (var fileStream = new FileStream(batchFilePath, FileMode.Open, FileAccess.Read))
                {
                    batch = formatter.Deserialize(fileStream) as Batch;
                }
            }

            if (null != batch)
            {
                string filePath = Path.Combine(path, batch.FileName);

                // Can raise an error if the client disconnect during a session
                //if (File.Exists(filePath))
                //{
                //    File.Delete(filePath);
                //}

                if (batch.IsLastBatch)
                {
                    // If this is the last batch then try to cleanup the directory.
                    CleanupBatchDirectory(path, headerFilePath, header);
                }
            }

            return batch;
        }

        private static void CleanupBatchDirectory(string path, string headerFilePath, BatchHeader header)
        {
            try
            {

                if (Directory.Exists(path))
                {
                    // First delete all the batch related files.
                    foreach (var batchFileName in header.BatchFileNames)
                    {
                        string filePath = Path.Combine(path, batchFileName);
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }

                    // Delete the header file.
                    if (File.Exists(headerFilePath))
                    {
                        File.Delete(headerFilePath);
                    }

                    // If there are no other files in the directory then delete the directory.
                    if (0 == Directory.GetFiles(path).Length)
                    {
                        Directory.Delete(path, true);
                    }
                }
            }
            catch (Exception exception)
            {
                SyncServiceTracer.TraceWarning("Error cleaning up batch directory. " + WebUtil.GetExceptionMessage(exception));
            }
        }

        #endregion
    }
}
