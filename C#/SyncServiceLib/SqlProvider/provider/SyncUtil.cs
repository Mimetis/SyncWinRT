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
using System.Globalization;
using System.Text;

namespace Microsoft.Synchronization.Services.SqlProvider
{
    internal static class SyncUtil
    {
        internal static SyncId InitRowId(string tableName, IEnumerable<object> pkVals)
        {
            var idBuilder = new StringBuilder(tableName.ToLower(CultureInfo.InvariantCulture));
            const string sep = "-";
            foreach (object pk in pkVals)
            {
                idBuilder.Append(sep);
                idBuilder.Append(pk.ToString());
            }
            return new SyncId(idBuilder.ToString());
        }

        internal static long GetRowSizeForObjectArray(Object[] row)
        {
            long rowSize = 0;
            foreach (object o in row)
            {
                string rowSizeInString = o as string;
                byte[] rowSizeInBytes = o as byte[];

                if (o == null)
                {
                    rowSize += 1;
                }
                else if (o is Guid)
                {
                    rowSize += 16;
                }
                else if (rowSizeInString != null)
                {
                    //By default all .NET strings are in Unicode encoding. So always get the byte count
                    //as opposed to string length  as the string length varies for different encodings
                    rowSize += Encoding.Unicode.GetByteCount(rowSizeInString);
                }
                else if (rowSizeInBytes != null)
                {
                    rowSize += (rowSizeInBytes).Length;
                }
                else
                {
                    rowSize += GetSizeForType(o.GetType());
                }
            }
            return rowSize;
        }

        internal static long GetSizeForType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Empty:
                    return 0;
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.SByte:
                    return 1;
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Char:
                    return 2;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Single:
                    return 4;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Double:
                case TypeCode.DateTime:
                case TypeCode.Object: //Treat it as a reference (pointer)
                    return 8;
                case TypeCode.Decimal:
                    return 16;
                default:
                    //Should never get here. So return 0
                    return 0;
            }
        }
    }


}
