using System;
using System.Globalization;
using System.Text;
using System.Xml;

namespace Microsoft.Synchronization.ClientServices
{
    internal static class XmlConverter
    {
        public const int MaxDateTimeChars = 64;
        public const int MaxInt32Chars = 16;
        public const int MaxInt64Chars = 32;
        public const int MaxBoolChars = 5;
        public const int MaxFloatChars = 16;
        public const int MaxDoubleChars = 32;
        public const int MaxDecimalChars = 40;
        public const int MaxUInt64Chars = 32;
        public const int MaxPrimitiveChars = 64;

        private static UTF8Encoding utf8Encoding;
        private static UnicodeEncoding unicodeEncoding;

        private static UTF8Encoding UTF8Encoding
        {
            get
            {
                if (utf8Encoding == null)
                    utf8Encoding = new UTF8Encoding(false, true);
                return utf8Encoding;
            }
        }

        private static UnicodeEncoding UnicodeEncoding
        {
            get
            {
                if (unicodeEncoding == null)
                    unicodeEncoding = new UnicodeEncoding(false, false, true);
                return unicodeEncoding;
            }
        }

        public static bool ToBoolean(string value)
        {
            return XmlConvert.ToBoolean(value);
        }

        public static bool ToBoolean(byte[] buffer, int offset, int count)
        {
            if (count == 1)
            {
                switch (buffer[offset])
                {
                    case Keys.One:
                        return true;
                    case Keys.Zero:
                        return false;
                }
            }
            return ToBoolean(ToString(buffer, offset, count));
        }

        public static int ToInt32(string value)
        {
            return XmlConvert.ToInt32(value);
        }

        public static int ToInt32(byte[] buffer, int offset, int count)
        {
            int result;

            if (TryParseInt32(buffer, offset, count, out result))
                return result;

            return ToInt32(ToString(buffer, offset, count));
        }

        public static long ToInt64(string value)
        {
            return XmlConvert.ToInt64(value);
        }

        public static long ToInt64(byte[] buffer, int offset, int count)
        {
            long result;

            if (TryParseInt64(buffer, offset, count, out result))
                return result;

            return ToInt64(ToString(buffer, offset, count));
        }

        public static float ToSingle(string value)
        {
            return XmlConvert.ToSingle(value);
        }

        public static float ToSingle(byte[] buffer, int offset, int count)
        {
            float result;

            if (TryParseSingle(buffer, offset, count, out result))
                return result;

            return ToSingle(ToString(buffer, offset, count));
        }

        public static double ToDouble(string value)
        {
            return XmlConvert.ToDouble(value);
        }

        public static double ToDouble(byte[] buffer, int offset, int count)
        {
            double result;
            if (TryParseDouble(buffer, offset, count, out result))
                return result;
            
            return ToDouble(ToString(buffer, offset, count));
        }

        public static Decimal ToDecimal(string value)
        {
            return XmlConvert.ToDecimal(value);
        }

        public static Decimal ToDecimal(byte[] buffer, int offset, int count)
        {
            return ToDecimal(ToString(buffer, offset, count));
        }

        public static DateTime ToDateTime(long value)
        {
            return DateTime.FromBinary(value);
        }

        public static DateTime ToDateTime(string value)
        {
            DateTime d;
            if (DateTime.TryParse(value, out d))
                return d;

            throw new ArgumentException("Cant parse String value to DateTime");
            //return XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.RoundtripKind);
        }

        public static DateTime ToDateTime(byte[] buffer, int offset, int count)
        {
            DateTime result;

            if (TryParseDateTime(buffer, offset, count, out result))
                return result;

            return ToDateTime(ToString(buffer, offset, count));
        }

        public static UniqueId ToUniqueId(string value)
        {
            return new UniqueId(Trim(value));
        }

        public static UniqueId ToUniqueId(byte[] buffer, int offset, int count)
        {
            return ToUniqueId(ToString(buffer, offset, count));
        }

        public static TimeSpan ToTimeSpan(string value)
        {
            return XmlConvert.ToTimeSpan(value);
        }

        public static TimeSpan ToTimeSpan(byte[] buffer, int offset, int count)
        {
            return ToTimeSpan(ToString(buffer, offset, count));
        }

        public static Guid ToGuid(string value)
        {
            return new Guid(Trim(value));
        }

        public static Guid ToGuid(byte[] buffer, int offset, int count)
        {
            return ToGuid(ToString(buffer, offset, count));
        }

        public static ulong ToUInt64(string value)
        {
            return ulong.Parse(value, NumberFormatInfo.InvariantInfo);
        }

        public static ulong ToUInt64(byte[] buffer, int offset, int count)
        {
            return ToUInt64(ToString(buffer, offset, count));
        }

        public static string ToString(byte[] buffer, int offset, int count)
        {
            return UTF8Encoding.GetString(buffer, offset, count);
        }

        public static string ToStringUnicode(byte[] buffer, int offset, int count)
        {
            return UnicodeEncoding.GetString(buffer, offset, count);
        }

        public static byte[] ToBytes(string value)
        {
            return UTF8Encoding.GetBytes(value);
        }

        public static int ToChars(byte[] buffer, int offset, int count, char[] chars, int charOffset)
        {
            return UTF8Encoding.GetChars(buffer, offset, count, chars, charOffset);
        }

        public static string ToString(bool value)
        {
            return !value ? "false" : "true";
        }

        public static string ToString(int value)
        {
            return XmlConvert.ToString(value);
        }

        public static string ToString(long value)
        {
            return XmlConvert.ToString(value);
        }

        public static string ToString(float value)
        {
            return XmlConvert.ToString(value);
        }

        public static string ToString(double value)
        {
            return XmlConvert.ToString(value);
        }

        public static string ToString(Decimal value)
        {
            return XmlConvert.ToString(value);
        }

        public static string ToString(TimeSpan value)
        {
            return XmlConvert.ToString(value);
        }

        public static string ToString(UniqueId value)
        {
            return value.ToString();
        }

        public static string ToString(Guid value)
        {
            return value.ToString();
        }

        public static string ToString(ulong value)
        {
            return value.ToString(NumberFormatInfo.InvariantInfo);
        }

        public static string ToString(DateTime value)
        {
            var numArray = new byte[64];
            int count = ToChars(value, numArray, 0);
            return ToString(numArray, 0, count);
        }

        private static string ToString(object value)
        {
            if (value is int)
                return ToString((int) value);
            if (value is long)
                return ToString((long) value);
            if (value is float)
                return ToString((float) value);
            if (value is double)
                return ToString((double) value);
            if (value is Decimal)
                return ToString((Decimal) value);
            if (value is TimeSpan)
                return ToString((TimeSpan) value);
            if (value is UniqueId)
                return ToString((UniqueId) value);
            if (value is Guid)
                return ToString((Guid) value);
            if (value is ulong)
                return ToString((ulong) value);
            if (value is DateTime)
                return ToString((DateTime) value);
            if (value is bool)
                return ToString((bool) value);

            return value.ToString();
        }

        public static string ToString(object[] objects)
        {
            if (objects.Length == 0)
                return string.Empty;

            string str = ToString(objects[0]);

            if (objects.Length > 1)
            {
                var stringBuilder = new StringBuilder(str);
                for (int index = 1; index < objects.Length; ++index)
                {
                    stringBuilder.Append(' ');
                    stringBuilder.Append(ToString(objects[index]));
                }
                str = stringBuilder.ToString();
            }
            return str;
        }

        public static void ToQualifiedName(string qname, out string prefix, out string localName)
        {
            int length = qname.IndexOf(':');
            if (length < 0)
            {
                prefix = string.Empty;
                localName = Trim(qname);
            }
            else if (length == qname.Length - 1)
            {
                throw new XmlException("XmlInvalidQualifiedName");
            }
            else
            {
                prefix = Trim(qname.Substring(0, length));
                localName = Trim(qname.Substring(length + 1));
            }
        }
        private static bool TryParseInt32(byte[] chars, int offset, int count, out int result)
        {
            result = 0;

            if (count == 0)
                return false;

            int value = 0;
            int offsetMax = offset + count;

            if (chars[offset] == '-')
            {
                if (count == 1)
                    return false;

                for (int i = offset + 1; i < offsetMax; i++)
                {
                    int digit = (chars[i] - '0');

                    if ((uint)digit > 9)
                        return false;

                    if (value < int.MinValue / 10)
                        return false;

                    value *= 10;
                    if (value < int.MinValue + digit)
                        return false;

                    value -= digit;
                }
            }
            else
            {

                for (int i = offset; i < offsetMax; i++)
                {
                    int digit = (chars[i] - '0');

                    if ((uint)digit > 9)
                        return false;

                    if (value > int.MaxValue / 10)
                        return false;

                    value *= 10;

                    if (value > int.MaxValue - digit)
                        return false;

                    value += digit;
                }
            }

            result = value;
            return true;
        }

        private static bool TryParseInt64(byte[] chars, int offset, int count, out long result)
        {
            result = 0;

            if (count < 11)
            {
                int value;

                if (!TryParseInt32(chars, offset, count, out value))
                    return false;

                result = value;
                return true;
            }
            else
            {
                long value = 0;
                int offsetMax = offset + count;

                if (chars[offset] == '-')
                {
                    if (count == 1)
                        return false;

                    for (int i = offset + 1; i < offsetMax; i++)
                    {
                        int digit = (chars[i] - '0');

                        if ((uint)digit > 9)
                            return false;

                        if (value < long.MinValue / 10)
                            return false;

                        value *= 10;

                        if (value < long.MinValue + digit)
                            return false;

                        value -= digit;
                    }
                }
                else
                {
                    for (int i = offset; i < offsetMax; i++)
                    {
                        int digit = (chars[i] - '0');

                        if ((uint)digit > 9)
                            return false;

                        if (value > long.MaxValue / 10)
                            return false;

                        value *= 10;

                        if (value > long.MaxValue - digit)
                            return false;

                        value += digit;
                    }
                }

                result = value;
                return true;

            } 
        }

        private static bool TryParseSingle(byte[] chars, int offset, int count, out float result)
        {
            result = 0;
            int offsetMax = offset + count;
            bool negative = false;

            if (offset < offsetMax && chars[offset] == '-')
            {
                negative = true;
                offset++;
                count--;
            }

            if (count < 1 || count > 10)
                return false;

            int value = 0;

            while (offset < offsetMax)
            {
                int ch = (chars[offset] - '0');

                if (ch == ('.' - '0'))
                {
                    offset++;
                    int pow10 = 1;

                    while (offset < offsetMax)
                    {
                        ch = chars[offset] - '0';

                        if (((uint)ch) >= 10)
                            return false;

                        pow10 *= 10;
                        value = value * 10 + ch;
                        offset++;

                    }

                    // More than 8 characters (7 sig figs and a decimal) and int -> float conversion is lossy, so use double 
                    if (count > 8)
                        result = (float) (value/(double) pow10);
                    else
                        result = (float) value/pow10;

                    if (negative)
                        result = -result;

                    return true;

                }
                if (((uint)ch) >= 10)
                    return false;

                value = value * 10 + ch;
                offset++;

            }

            // Ten digits w/out a decimal point might've overflowed the int 
            if (count == 10)
                return false;

            if (negative)
                result = -value;
            else
                result = value;

            return true;
        }

        private static bool TryParseDouble(byte[] chars, int offset, int count, out double result)
        {
            result = 0;
            int offsetMax = offset + count;
            bool negative = false;

            if (offset < offsetMax && chars[offset] == '-')
            {
                negative = true;
                offset++;
                count--;
            }

            if (count < 1 || count > 10)
                return false;

            int value = 0;

            while (offset < offsetMax)
            {
                int ch = (chars[offset] - '0');

                if (ch == ('.' - '0'))
                {
                    offset++;
                    int pow10 = 1;
                    while (offset < offsetMax)
                    {
                        ch = chars[offset] - '0';

                        if (((uint)ch) >= 10)
                            return false;

                        pow10 *= 10;
                        value = value * 10 + ch;
                        offset++;
                    }

                    if (negative)
                        result = -(double)value / pow10;
                    else
                        result = (double)value / pow10;

                    return true;
                }

                if (((uint)ch) >= 10)
                    return false;

                value = value * 10 + ch;
                offset++;

            }

            // Ten digits w/out a decimal point might've overflowed the int
            if (count == 10)
                return false;

            if (negative)
                result = -value;
            else
                result = value;

            return true;
        }

        private static int ToInt32D2(byte[] chars, int offset)
        {
            byte ch1 = (byte)(chars[offset + 0] - '0');
            byte ch2 = (byte)(chars[offset + 1] - '0');

            if (ch1 > 9 || ch2 > 9)
                return -1;

            return 10 * ch1 + ch2;
        }

        private static int ToInt32D4(byte[] chars, int offset, int count)
        {
            return ToInt32D7(chars, offset, count);
        }

        private static int ToInt32D7(byte[] chars, int offset, int count)
        {
            int value = 0; 

            for (int i = 0; i < count; i++)
            { 
                byte ch = (byte)(chars[offset + i] - '0'); 

                if (ch > 9)
                    return -1; 

                value = value * 10 + ch;
            }

            return value;
        }

        private static bool TryParseDateTime(byte[] chars, int offset, int count, out DateTime result)
        {
            int offsetMax = offset + count;

            result = DateTime.MaxValue;

            if (count < 19)
                return false;

            //            1         2         3
            //  012345678901234567890123456789012 
            // "yyyy-MM-ddTHH:mm:ss" 
            // "yyyy-MM-ddTHH:mm:ss.fffffff"
            // "yyyy-MM-ddTHH:mm:ss.fffffffZ" 
            // "yyyy-MM-ddTHH:mm:ss.fffffff+xx:yy"
            // "yyyy-MM-ddTHH:mm:ss.fffffff-xx:yy"

            if (chars[offset + 4] != '-' || chars[offset + 7] != '-' || chars[offset + 10] != 'T' ||
                chars[offset + 13] != ':' || chars[offset + 16] != ':')
                return false;


            int year = ToInt32D4(chars, offset + 0, 4);
            int month = ToInt32D2(chars, offset + 5);
            int day = ToInt32D2(chars, offset + 8);
            int hour = ToInt32D2(chars, offset + 11);
            int minute = ToInt32D2(chars, offset + 14);
            int second = ToInt32D2(chars, offset + 17);

           if ((year | month | day | hour | minute | second) < 0)
                return false;

            DateTimeKind kind = DateTimeKind.Unspecified;
            offset += 19;

            int ticks = 0;

            if (offset < offsetMax && chars[offset] == '.')
            {
                offset++;

                int digitOffset = offset;
                while (offset < offsetMax)
                {
                    byte ch = chars[offset];

                    if (ch < '0' || ch > '9')
                        break;

                    offset++;
                }

                int digitCount = offset - digitOffset;
                if (digitCount < 1 || digitCount > 7)
                    return false;

                ticks = ToInt32D7(chars, digitOffset, digitCount);

                if (ticks < 0)
                    return false;

                for (int i = digitCount; i < 7; ++i)
                    ticks *= 10;
            }

            bool isLocal = false;
            int hourDelta = 0;
            int minuteDelta = 0;

            if (offset < offsetMax)
            {
                byte ch = chars[offset];

                if (ch == 'Z')
                {
                    offset++;
                    kind = DateTimeKind.Utc;
                }

                else if (ch == '+' || ch == '-')
                {
                    offset++;

                    if (offset + 5 > offsetMax || chars[offset + 2] != ':')
                        return false;

                    kind = DateTimeKind.Utc;
                    isLocal = true;
                    hourDelta = ToInt32D2(chars, offset);
                    minuteDelta = ToInt32D2(chars, offset + 3);

                    if ((hourDelta | minuteDelta) < 0)
                        return false;

                    if (ch == '+')
                    {
                        hourDelta = -hourDelta;
                        minuteDelta = -minuteDelta;
                    }

                    offset += 5;
                }
            }

            if (offset < offsetMax)
                return false;

            DateTime value;

            try
            {
                value = new DateTime(year, month, day, hour, minute, second, kind);
            }
            catch (ArgumentException)
            {
                return false;
            }

            if (ticks > 0)
                value = value.AddTicks(ticks);

            if (isLocal)
            {
                try
                {
                    TimeSpan ts = new TimeSpan(hourDelta, minuteDelta, 0);

                    if (hourDelta >= 0 && (value < DateTime.MaxValue - ts) ||
                        hourDelta < 0 && (value > DateTime.MinValue - ts))
                        value = value.Add(ts).ToLocalTime();
                    else
                        value = value.ToLocalTime().Add(ts);
                }

                catch (ArgumentOutOfRangeException) // Overflow
                {
                    return false;
                }
            }

            result = value;
            return true;
        }

        public static int ToChars(bool value, byte[] buffer, int offset)
        {
            if (value)
            {
                buffer[offset + 0] = (byte)'t';
                buffer[offset + 1] = (byte)'r';
                buffer[offset + 2] = (byte)'u';
                buffer[offset + 3] = (byte)'e';
                return 4;
            }
            buffer[offset + 0] = (byte)'f';
            buffer[offset + 1] = (byte)'a';
            buffer[offset + 2] = (byte)'l';
            buffer[offset + 3] = (byte)'s';
            buffer[offset + 4] = (byte)'e';
            return 5;
        }

        public static int ToCharsR(int value, byte[] chars, int offset)
        {
            int count = 0;

            if (value >= 0)
            {
                while (value >= 10)
                {
                    int valueDiv10 = value / 10;
                    count++;
                    chars[--offset] = (byte)('0' + (value - valueDiv10 * 10));
                    value = valueDiv10;
                }
                chars[--offset] = (byte)('0' + value);
                count++;
            }
            else
            {
                while (value <= -10)
                {
                    int valueDiv10 = value / 10;
                    count++;
                    chars[--offset] = (byte)('0' - (value - valueDiv10 * 10));
                    value = valueDiv10;
                }
                chars[--offset] = (byte)('0' - value);
                chars[--offset] = (byte)'-';
                count += 2;
            }

            return count;
        }

        public static int ToChars(int value, byte[] chars, int offset)
        {
            int count = ToCharsR(value, chars, offset + 16);
            Buffer.BlockCopy(chars, offset + 16 - count, chars, offset, count);
            return count;
        }

        public static int ToCharsR(long value, byte[] chars, int offset)
        {
            int count = 0;
            if (value >= 0)
            {
                while (value > int.MaxValue)
                {
                    long valueDiv10 = value / 10;
                    count++;
                    chars[--offset] = (byte)('0' + (int)(value - valueDiv10 * 10));
                    value = valueDiv10;
                }
            }
            else
            {
                while (value < int.MinValue)
                {
                    long valueDiv10 = value / 10;
                    count++;
                    chars[--offset] = (byte)('0' - (int)(value - valueDiv10 * 10));
                    value = valueDiv10;
                }
            }
            return count + ToCharsR((int)value, chars, offset); 
        }

        public static int ToChars(long value, byte[] chars, int offset)
        {
            int count = ToCharsR(value, chars, offset + 32);
            Buffer.BlockCopy(chars, offset + 32 - count, chars, offset, count);
            return count;
        }

        private static int ToInfinity(bool isNegative, byte[] buffer, int offset)
        {
            if (isNegative)
            {
                buffer[offset + 0] = (byte)'-';
                buffer[offset + 1] = (byte)'I';
                buffer[offset + 2] = (byte)'N';
                buffer[offset + 3] = (byte)'F';
                return 4;
            }
            buffer[offset + 0] = (byte)'I';
            buffer[offset + 1] = (byte)'N';
            buffer[offset + 2] = (byte)'F';

            return 3;
        }

        private static int ToZero(bool isNegative, byte[] buffer, int offset)
        {
            if (isNegative)
            {
                buffer[offset + 0] = (byte)'-';
                buffer[offset + 1] = (byte)'0';
                return 2;
            }
            buffer[offset] = (byte)'0';
            return 1;
        }

        public static int ToChars(double value, byte[] buffer, int offset)
        {
            if (double.IsInfinity(value))
                return ToInfinity(double.IsNegativeInfinity(value), buffer, offset);
            if (value == 0.0)
                return ToZero(value < 0, buffer, offset);

            return ToAsciiChars(value.ToString("R", NumberFormatInfo.InvariantInfo), buffer, offset);
        }

        public static int ToChars(float value, byte[] buffer, int offset)
        {
            if (float.IsInfinity(value))
                return ToInfinity(float.IsNegativeInfinity(value), buffer, offset);

            if (value == 0.0)
                return ToZero(value < 0, buffer, offset);

            return ToAsciiChars(value.ToString("R", NumberFormatInfo.InvariantInfo), buffer, offset);
        }

        public static int ToChars(Decimal value, byte[] buffer, int offset)
        {
            return ToAsciiChars(value.ToString(null, NumberFormatInfo.InvariantInfo), buffer, offset);
        }

        public static int ToChars(ulong value, byte[] buffer, int offset)
        {
            return ToAsciiChars(value.ToString(null, NumberFormatInfo.InvariantInfo), buffer, offset);
        }

        private static int ToAsciiChars(string s, byte[] buffer, int offset)
        {
            for (int index = 0; index < s.Length; ++index)
                buffer[offset++] = (byte) s[index];
            return s.Length;
        }

        private static int ToCharsD2(int value, byte[] chars, int offset)
        {
            if (value < 10)
            {
                chars[offset + 0] = (byte)'0';
                chars[offset + 1] = (byte)('0' + value);
            }
            else
            {
                int valueDiv10 = value / 10;
                chars[offset + 0] = (byte)('0' + valueDiv10);
                chars[offset + 1] = (byte)('0' + value - valueDiv10 * 10);
            }
            return 2;
        }

        private static int ToCharsD4(int value, byte[] chars, int offset)
        {
            ToCharsD2(value/100, chars, offset);
            ToCharsD2(value%100, chars, offset + 2);
            return 4;
        }

        private static int ToCharsD7(int value, byte[] chars, int offset)
        {
            int zeroCount = 7 - ToCharsR(value, chars, offset + 7);
            for (int i = 0; i < zeroCount; i++)
                chars[offset + i] = (byte)'0';

            int count = 7;
            while (count > 0 && chars[offset + count - 1] == '0')
                count--;

            return count;
        }

        public static int ToChars(DateTime value, byte[] chars, int offset)
        {
            const long TicksPerMillisecond = 10000;
            const long TicksPerSecond = TicksPerMillisecond * 1000;
            int offsetMin = offset;
            // "yyyy-MM-ddTHH:mm:ss.fffffff"; 
            offset += ToCharsD4(value.Year, chars, offset);
            chars[offset++] = (byte)'-';
            offset += ToCharsD2(value.Month, chars, offset);
            chars[offset++] = (byte)'-';
            offset += ToCharsD2(value.Day, chars, offset);
            chars[offset++] = (byte)'T';
            offset += ToCharsD2(value.Hour, chars, offset);
            chars[offset++] = (byte)':';
            offset += ToCharsD2(value.Minute, chars, offset);
            chars[offset++] = (byte)':';
            offset += ToCharsD2(value.Second, chars, offset);
            int ms = (int)(value.Ticks % TicksPerSecond);

            if (ms != 0)
            {
                chars[offset++] = (byte)'.';
                offset += ToCharsD7(ms, chars, offset);
            }

            switch (value.Kind)
            {
                case DateTimeKind.Unspecified:
                    break;
                case DateTimeKind.Local:
                    // +"zzzzzz";
                    TimeSpan ts = TimeZoneInfo.Local.GetUtcOffset(value);
                    if (ts.Ticks < 0)
                        chars[offset++] = (byte)'-';
                    else
                        chars[offset++] = (byte)'+';
                    offset += ToCharsD2(Math.Abs(ts.Hours), chars, offset);
                    chars[offset++] = (byte)':';
                    offset += ToCharsD2(Math.Abs(ts.Minutes), chars, offset);
                    break;
                case DateTimeKind.Utc:
                    // +"Z" 
                    chars[offset++] = (byte)'Z';
                    break;
                default:
                    throw new InvalidOperationException();
            }

            return offset - offsetMin; 
        }

        public static bool IsWhitespace(string s)
        {
            for (int index = 0; index < s.Length; ++index)
            {
                if (!IsWhitespace(s[index]))
                    return false;
            }
            return true;
        }

        public static bool IsWhitespace(char ch)
        {
            return (ch <= ' ' && (ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n'));
        }

        public static string StripWhitespace(string s)
        {
            int count = s.Length;

            for (int i = 0; i < s.Length; i++)
            {
                if (IsWhitespace(s[i]))
                    count--;
            }

            if (count == s.Length)
                return s;

            char[] chars = new char[count];

            count = 0;

            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];

                if (!IsWhitespace(ch))
                    chars[count++] = ch;
            }

            return new string(chars);
        }

        private static string Trim(string s)
        {
            int startIndex = 0;
            while (startIndex < s.Length && IsWhitespace(s[startIndex]))
                ++startIndex;
            int length = s.Length;
            while (length > 0 && IsWhitespace(s[length - 1]))
                --length;
            if (startIndex == 0 && length == s.Length)
                return s;
            if (length == 0)
                return string.Empty;

            return s.Substring(startIndex, length - startIndex);
        }
    }
}