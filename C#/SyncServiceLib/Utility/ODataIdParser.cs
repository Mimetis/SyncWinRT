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
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.Synchronization.Services
{
    /// <summary>
    /// This code parses an OData KeyValue string and returns the primitive type.
    /// </summary>
    internal static class ODataIdParser
    {
        #region Private Members

        private static char[] XmlWhitespaceChars = new char[] { ' ', '\t', '\n', '\r' };

        #endregion

        internal static bool TryKeyStringToPrimitive(string text, Type targetType, out object targetValue)
        {
            targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            byte[] buffer;
            bool flag = TryKeyStringToByteArray(text, out buffer);

            if (targetType == typeof(byte[]))
            {
                targetValue = buffer;
                return flag;
            }

            if (flag)
            {
                return TryKeyStringToPrimitive(Encoding.UTF8.GetString(buffer), targetType, out targetValue);
            }

            if (targetType == typeof(Guid))
            {
                Guid guid;
                flag = TryKeyStringToGuid(text, out guid);
                targetValue = guid;
                return flag;
            }

            if (targetType == typeof(DateTime))
            {
                DateTime time;
                flag = TryKeyStringToDateTime(text, out time);
                targetValue = time;
                return flag;
            }

            bool flag4 = IsKeyTypeQuoted(targetType);
            if (flag4 != IsKeyValueQuoted(text))
            {
                targetValue = null;
                return false;
            }
            if (flag4)
            {
                text = RemoveQuotes(text);
            }

            try
            {
                if (typeof(string) == targetType)
                {
                    targetValue = text;
                }
                else if (typeof(bool) == targetType)
                {
                    targetValue = XmlConvert.ToBoolean(text);
                }
                else if (typeof(byte) == targetType)
                {
                    targetValue = XmlConvert.ToByte(text);
                }
                else if (typeof(sbyte) == targetType)
                {
                    targetValue = XmlConvert.ToSByte(text);
                }
                else if (typeof(short) == targetType)
                {
                    targetValue = XmlConvert.ToInt16(text);
                }
                else if (typeof(int) == targetType)
                {
                    targetValue = XmlConvert.ToInt32(text);
                }
                else if (typeof(long) == targetType)
                {
                    if (!TryRemoveLiteralSuffix("L", ref text))
                    {
                        targetValue = 0L;
                        return false;
                    }
                    targetValue = XmlConvert.ToInt64(text);
                }
                else if (typeof(float) == targetType)
                {
                    if (!TryRemoveLiteralSuffix("f", ref text))
                    {
                        targetValue = 0f;
                        return false;
                    }
                    targetValue = XmlConvert.ToSingle(text);
                }
                else if (typeof(double) == targetType)
                {
                    TryRemoveLiteralSuffix("D", ref text);
                    targetValue = XmlConvert.ToDouble(text);
                }
                else
                {
                    if (typeof(decimal) == targetType)
                    {
                        if (TryRemoveLiteralSuffix("M", ref text))
                        {
                            try
                            {
                                targetValue = XmlConvert.ToDecimal(text);
                                //goto Label_02E9;
                            }
                            catch (FormatException)
                            {
                                decimal num;
                                if (decimal.TryParse(text, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out num))
                                {
                                    targetValue = num;
                                    //goto Label_02E9;
                                }
                                else
                                {
                                    targetValue = 0M;
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            targetValue = 0M;
                            return false;
                        }
                    }
                    targetValue = XElement.Parse(text, LoadOptions.PreserveWhitespace);
                }

                flag = true;
            }
            catch (FormatException)
            {
                targetValue = null;
                flag = false;
            }
            return flag;
        }

        #region Private Methods

        private static bool TryRemoveLiteralSuffix(string suffix, ref string text)
        {
            text = text.Trim(XmlWhitespaceChars);
            if ((text.Length <= suffix.Length) || !text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            text = text.Substring(0, text.Length - suffix.Length);
            return true;
        }

        private static bool TryKeyStringToDateTime(string text, out DateTime targetValue)
        {
            if (!TryRemoveLiteralPrefix("datetime", ref text))
            {
                targetValue = new DateTime();
                return false;
            }
            if (!TryRemoveQuotes(ref text))
            {
                targetValue = new DateTime();
                return false;
            }
            try
            {
                targetValue = XmlConvert.ToDateTime(text, XmlDateTimeSerializationMode.RoundtripKind);
                return true;
            }
            catch (FormatException)
            {
                targetValue = new DateTime();
                return false;
            }
        }

        private static bool TryKeyStringToGuid(string text, out Guid targetValue)
        {
            if (!TryRemoveLiteralPrefix("guid", ref text))
            {
                targetValue = new Guid();
                return false;
            }
            if (!TryRemoveQuotes(ref text))
            {
                targetValue = new Guid();
                return false;
            }
            try
            {
                targetValue = XmlConvert.ToGuid(text);
                return true;
            }
            catch (FormatException)
            {
                targetValue = new Guid();
                return false;
            }
        }

        private static bool TryKeyStringToByteArray(string text, out byte[] targetValue)
        {
            if (!TryRemoveLiteralPrefix("binary", ref text) && !TryRemoveLiteralPrefix("X", ref text))
            {
                targetValue = null;
                return false;
            }
            if (!TryRemoveQuotes(ref text))
            {
                targetValue = null;
                return false;
            }
            if ((text.Length % 2) != 0)
            {
                targetValue = null;
                return false;
            }
            byte[] buffer = new byte[text.Length / 2];
            int index = 0;
            int num2 = 0;
            while (index < buffer.Length)
            {
                char c = text[num2];
                char ch2 = text[num2 + 1];
                if (!IsCharHexDigit(c) || !IsCharHexDigit(ch2))
                {
                    targetValue = null;
                    return false;
                }
                buffer[index] = (byte)(((byte)(HexCharToNibble(c) << 4)) + HexCharToNibble(ch2));
                num2 += 2;
                index++;
            }
            targetValue = buffer;
            return true;
        }

        private static bool IsCharHexDigit(char c)
        {
            if (((c < '0') || (c > '9')) && ((c < 'a') || (c > 'f')))
            {
                return ((c >= 'A') && (c <= 'F'));
            }
            return true;
        }

        private static byte HexCharToNibble(char c)
        {
            switch (c)
            {
                case '0':
                    return 0;

                case '1':
                    return 1;

                case '2':
                    return 2;

                case '3':
                    return 3;

                case '4':
                    return 4;

                case '5':
                    return 5;

                case '6':
                    return 6;

                case '7':
                    return 7;

                case '8':
                    return 8;

                case '9':
                    return 9;

                case 'A':
                case 'a':
                    return 10;

                case 'B':
                case 'b':
                    return 11;

                case 'C':
                case 'c':
                    return 12;

                case 'D':
                case 'd':
                    return 13;

                case 'E':
                case 'e':
                    return 14;

                case 'F':
                case 'f':
                    return 15;
            }
            throw new InvalidOperationException();
        }

        private static bool TryRemoveLiteralPrefix(string prefix, ref string text)
        {
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                text = text.Remove(0, prefix.Length);
                return true;
            }
            return false;
        }

        private static bool TryRemoveQuotes(ref string text)
        {
            if (text.Length < 2)
            {
                return false;
            }
            char ch = text[0];
            if ((ch != '\'') || (text[text.Length - 1] != ch))
            {
                return false;
            }
            string str = text.Substring(1, text.Length - 2);
            int startIndex = 0;
            while (true)
            {
                int index = str.IndexOf(ch, startIndex);
                if (index < 0)
                {
                    break;
                }
                str = str.Remove(index, 1);
                if ((str.Length < (index + 1)) || (str[index] != ch))
                {
                    return false;
                }
                startIndex = index + 1;
            }
            text = str;
            return true;
        }

        private static string RemoveQuotes(string text)
        {
            char ch = text[0];
            string str = text.Substring(1, text.Length - 2);
            int startIndex = 0;
            while (true)
            {
                int index = str.IndexOf(ch, startIndex);
                if (index < 0)
                {
                    return str;
                }
                str = str.Remove(index, 1);
                startIndex = index + 1;
            }
        }

        private static bool IsKeyValueQuoted(string text)
        {
            int num2;
            if (((text.Length < 2) || (text[0] != '\'')) || (text[text.Length - 1] != '\''))
            {
                return false;
            }
            for (int i = 1; i < (text.Length - 1); i = num2 + 2)
            {
                num2 = text.IndexOf('\'', i, (text.Length - i) - 1);
                if (num2 == -1)
                {
                    break;
                }
                if ((num2 == (text.Length - 2)) || (text[num2 + 1] != '\''))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsKeyTypeQuoted(Type type)
        {
            if (type != typeof(XElement))
            {
                return (type == typeof(string));
            }
            return true;
        }

        #endregion
    }
}