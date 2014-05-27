using System;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace Microsoft.Synchronization.ClientServices
{

    public enum ValueHandleType
    {
        Empty,
        True,
        False,
        Zero,
        One,
        Int8,
        Int16,
        Int32,
        Int64,
        UInt64,
        Single,
        Double,
        Decimal,
        DateTime,
        TimeSpan,
        Guid,
        UniqueId,
        UTF8,
        EscapedUTF8,
        Base64,
        Dictionary,
        List,
        Char,
        Unicode,
        QName,
        ConstString,
    }

    public enum ValueHandleConstStringType
    {
        String,
        Number,
        Array,
        Object,
        Boolean,
        Null,
    }


    public class ValueHandle
    {
        private static string[] constStrings = new[]
        {
          "string",
          "number",
          "array",
          "object",
          "boolean",
          "null"
        };
        private XmlBufferReader bufferReader;
        private ValueHandleType type;
        private int offset;
        private int length;

        static ValueHandle()
        {
        }

        public ValueHandle(XmlBufferReader bufferReader)
        {
            this.bufferReader = bufferReader;
            this.type = ValueHandleType.Empty;
        }

        public void SetConstantValue(ValueHandleConstStringType constStringType)
        {
            this.type = ValueHandleType.ConstString;
            this.offset = (int)constStringType;
        }

        public void SetValue(ValueHandleType valueHandleType)
        {
            this.type = valueHandleType;
        }

        public void SetDictionaryValue(int key)
        {
            this.SetValue(ValueHandleType.Dictionary, key, 0);
        }

        public void SetCharValue(int ch)
        {
            this.SetValue(ValueHandleType.Char, ch, 0);
        }

        public void SetQNameValue(int prefix, int key)
        {
            this.SetValue(ValueHandleType.QName, key, prefix);
        }

        public void SetValue(ValueHandleType iType, int iOffset, int iLength)
        {
            this.type = iType;
            this.offset = iOffset;
            this.length = iLength;
        }

        public bool IsWhitespace()
        {
            switch (this.type)
            {
                case ValueHandleType.True:
                case ValueHandleType.False:
                case ValueHandleType.Zero:
                case ValueHandleType.One:
                    return false;
                case ValueHandleType.UTF8:
                    throw new NotSupportedException();
                case ValueHandleType.EscapedUTF8:
                    throw new NotSupportedException();
                case ValueHandleType.Dictionary:
                    throw new NotSupportedException();
                case ValueHandleType.Char:
                    int @char = this.GetChar();
                    if (@char > ushort.MaxValue)
                        return false;

                    return XmlConverter.IsWhitespace((char)@char);
                case ValueHandleType.Unicode:
                    throw new NotSupportedException();
                case ValueHandleType.ConstString:
                    return ValueHandle.constStrings[this.offset].Length == 0;
                default:
                    return this.length == 0;
            }
        }

        public Type ToType()
        {
            switch (this.type)
            {
                case ValueHandleType.Empty:
                case ValueHandleType.UTF8:
                case ValueHandleType.EscapedUTF8:
                case ValueHandleType.Dictionary:
                case ValueHandleType.Char:
                case ValueHandleType.Unicode:
                case ValueHandleType.QName:
                case ValueHandleType.ConstString:
                    return typeof(string);
                case ValueHandleType.True:
                case ValueHandleType.False:
                    return typeof(bool);
                case ValueHandleType.Zero:
                case ValueHandleType.One:
                case ValueHandleType.Int8:
                case ValueHandleType.Int16:
                case ValueHandleType.Int32:
                    return typeof(int);
                case ValueHandleType.Int64:
                    return typeof(long);
                case ValueHandleType.UInt64:
                    return typeof(ulong);
                case ValueHandleType.Single:
                    return typeof(float);
                case ValueHandleType.Double:
                    return typeof(double);
                case ValueHandleType.Decimal:
                    return typeof(Decimal);
                case ValueHandleType.DateTime:
                    return typeof(DateTime);
                case ValueHandleType.TimeSpan:
                    return typeof(TimeSpan);
                case ValueHandleType.Guid:
                    return typeof(Guid);
                case ValueHandleType.UniqueId:
                    return typeof(UniqueId);
                case ValueHandleType.Base64:
                    return typeof(byte[]);
                case ValueHandleType.List:
                    return typeof(object[]);
                default:
                    throw new InvalidOperationException();
            }
        }

        public bool ToBoolean()
        {
            switch (this.type)
            {
                case ValueHandleType.False:
                    return false;
                case ValueHandleType.True:
                    return true;
                case ValueHandleType.UTF8:
                    return XmlConverter.ToBoolean(this.bufferReader.Buffer, this.offset, this.length);
                case ValueHandleType.Int8:
                    switch (this.GetInt8())
                    {
                        case 0:
                            return false;
                        case 1:
                            return true;
                    }
                    break;
            }

            return XmlConverter.ToBoolean(this.GetString());
        }

        public int ToInt()
        {
            ValueHandleType valueHandleType = this.type;
            if (valueHandleType == ValueHandleType.Zero)
                return 0;
            if (valueHandleType == ValueHandleType.One)
                return 1;
            if (valueHandleType == ValueHandleType.Int8)
                return this.GetInt8();
            if (valueHandleType == ValueHandleType.Int16)
                return this.GetInt16();
            if (valueHandleType == ValueHandleType.Int32)
                return this.GetInt32();
            if (valueHandleType == ValueHandleType.Int64)
            {
                long int64 = this.GetInt64();
                if (int64 >= int.MinValue && int64 <= int.MaxValue)
                    return (int)int64;
            }
            if (valueHandleType == ValueHandleType.UInt64)
            {
                ulong uint64 = this.GetUInt64();
                if (uint64 <= int.MaxValue)
                    return (int)uint64;
            }
            if (valueHandleType == ValueHandleType.UTF8)
                return XmlConverter.ToInt32(this.bufferReader.Buffer, this.offset, this.length);

            return XmlConverter.ToInt32(this.GetString());
        }

        public long ToLong()
        {
            ValueHandleType valueHandleType = this.type;
            if (valueHandleType == ValueHandleType.Zero)
                return 0L;
            if (valueHandleType == ValueHandleType.One)
                return 1L;
            if (valueHandleType == ValueHandleType.Int8)
                return this.GetInt8();
            if (valueHandleType == ValueHandleType.Int16)
                return this.GetInt16();
            if (valueHandleType == ValueHandleType.Int32)
                return this.GetInt32();
            if (valueHandleType == ValueHandleType.Int64)
                return this.GetInt64();
            if (valueHandleType == ValueHandleType.UInt64)
                return (long)this.GetUInt64();
            if (valueHandleType == ValueHandleType.UTF8)
                return XmlConverter.ToInt64(this.bufferReader.Buffer, this.offset, this.length);

            return XmlConverter.ToInt64(this.GetString());
        }

        public ulong ToULong()
        {
            ValueHandleType valueHandleType = this.type;
            switch (valueHandleType)
            {
                case ValueHandleType.Zero:
                    return 0UL;
                case ValueHandleType.One:
                    return 1UL;
                default:
                    if (valueHandleType >= ValueHandleType.Int8 && valueHandleType <= ValueHandleType.Int64)
                    {
                        long num = this.ToLong();
                        if (num >= 0L)
                            return (ulong)num;
                    }
                    if (valueHandleType == ValueHandleType.UInt64)
                        return this.GetUInt64();
                    if (valueHandleType == ValueHandleType.UTF8)
                        return XmlConverter.ToUInt64(this.bufferReader.Buffer, this.offset, this.length);

                    return XmlConverter.ToUInt64(this.GetString());
            }
        }

        public float ToSingle()
        {
            ValueHandleType valueHandleType = this.type;
            if (valueHandleType == ValueHandleType.Single)
                return this.GetSingle();
            if (valueHandleType == ValueHandleType.Double)
            {
                double value = GetDouble();
                if ((value >= Single.MinValue && value <= Single.MaxValue) || double.IsInfinity(value) || double.IsNaN(value))
                    return (Single)value; 
            }
            if (valueHandleType == ValueHandleType.Zero)
                return 0.0f;
            if (valueHandleType == ValueHandleType.One)
                return 1f;
            if (valueHandleType == ValueHandleType.Int8)
                return this.GetInt8();
            if (valueHandleType == ValueHandleType.Int16)
                return this.GetInt16();
            if (valueHandleType == ValueHandleType.UTF8)
                return XmlConverter.ToSingle(this.bufferReader.Buffer, this.offset, this.length);

            return XmlConverter.ToSingle(this.GetString());
        }

        public double ToDouble()
        {
            switch (this.type)
            {
                case ValueHandleType.Double:
                    return this.GetDouble();
                case ValueHandleType.Single:
                    return this.GetSingle();
                case ValueHandleType.Zero:
                    return 0.0;
                case ValueHandleType.One:
                    return 1.0;
                case ValueHandleType.Int8:
                    return this.GetInt8();
                case ValueHandleType.Int16:
                    return this.GetInt16();
                case ValueHandleType.Int32:
                    return this.GetInt32();
                case ValueHandleType.UTF8:
                    return XmlConverter.ToDouble(this.bufferReader.Buffer, this.offset, this.length);
                default:
                    return XmlConverter.ToDouble(this.GetString());
            }
        }

        public Decimal ToDecimal()
        {
            ValueHandleType valueHandleType = this.type;
            switch (valueHandleType)
            {
                case ValueHandleType.Decimal:
                    return this.GetDecimal();
                case ValueHandleType.Zero:
                    return new Decimal(0);
                case ValueHandleType.One:
                    return new Decimal(1);
                default:
                    if (valueHandleType >= ValueHandleType.Int8 && valueHandleType <= ValueHandleType.Int64)
                        return this.ToLong();
                    if (valueHandleType == ValueHandleType.UInt64)
                        return this.GetUInt64();
                    if (valueHandleType == ValueHandleType.UTF8)
                        return XmlConverter.ToDecimal(this.bufferReader.Buffer, this.offset, this.length);

                    return XmlConverter.ToDecimal(this.GetString());
            }
        }

        public DateTime ToDateTime()
        {
            if (this.type == ValueHandleType.DateTime)
                return XmlConverter.ToDateTime(this.GetInt64());
            if (this.type == ValueHandleType.UTF8)
                return XmlConverter.ToDateTime(this.bufferReader.Buffer, this.offset, this.length);

            return XmlConverter.ToDateTime(this.GetString());
        }

        public UniqueId ToUniqueId()
        {
            if (this.type == ValueHandleType.UniqueId)
                return this.GetUniqueId();
            if (this.type == ValueHandleType.UTF8)
                return XmlConverter.ToUniqueId(this.bufferReader.Buffer, this.offset, this.length);

            return XmlConverter.ToUniqueId(this.GetString());
        }

        public TimeSpan ToTimeSpan()
        {
            if (this.type == ValueHandleType.TimeSpan)
                return new TimeSpan(this.GetInt64());
            if (this.type == ValueHandleType.UTF8)
                return XmlConverter.ToTimeSpan(this.bufferReader.Buffer, this.offset, this.length);

            return XmlConverter.ToTimeSpan(this.GetString());
        }

        public Guid ToGuid()
        {
            if (this.type == ValueHandleType.Guid)
                return this.GetGuid();
            if (this.type == ValueHandleType.UTF8)
                return XmlConverter.ToGuid(this.bufferReader.Buffer, this.offset, this.length);

            return XmlConverter.ToGuid(this.GetString());
        }

        public override string ToString()
        {
            return this.GetString();
        }

        public byte[] ToByteArray()
        {
            if (this.type == ValueHandleType.Base64)
            {
                byte[] buffer = new byte[this.length];
                this.GetBase64(buffer, 0, this.length);
                return buffer;
            }

            throw new NotSupportedException();
            //if (this.type == ValueHandleType.UTF8)
            //{
            //    if (this.length % 4 == 0)
            //    {
            //        try
            //        {
            //            int length = this.length / 4 * 3;
            //            if (this.length > 0 && (int)this.bufferReader.Buffer[this.offset + this.length - 1] == 61)
            //            {
            //                --length;
            //                if ((int)this.bufferReader.Buffer[this.offset + this.length - 2] == 61)
            //                    --length;
            //            }
            //            byte[] bytes1 = new byte[length];
            //            int bytes2 = ValueHandle.Base64Encoding.GetBytes(this.bufferReader.Buffer, this.offset, this.length, bytes1, 0);
            //            if (bytes2 != bytes1.Length)
            //            {
            //                byte[] numArray = new byte[bytes2];
            //                System.Buffer.BlockCopy((Array)bytes1, 0, (Array)numArray, 0, bytes2);
            //                bytes1 = numArray;
            //            }
            //            return bytes1;
            //        }
            //        catch (FormatException ex)
            //        {
            //        }
            //    }
            //}
            //try
            //{
            //    return ((Encoding)ValueHandle.Base64Encoding).GetBytes(XmlConverter.StripWhitespace(this.GetString()));
            //}
            //catch (FormatException ex)
            //{
            //    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception)new XmlException(ex.Message, ex.InnerException));

            //}
        }

        public string GetString()
        {
            ValueHandleType valueHandleType = this.type;

            if (valueHandleType == ValueHandleType.UTF8)
                return this.GetCharsText();

            switch (valueHandleType)
            {
                case ValueHandleType.Empty:
                    return string.Empty;
                case ValueHandleType.True:
                    return "true";
                case ValueHandleType.False:
                    return "false";
                case ValueHandleType.Zero:
                    return "0";
                case ValueHandleType.One:
                    return "1";
                case ValueHandleType.Int8:
                case ValueHandleType.Int16:
                case ValueHandleType.Int32:
                    return XmlConverter.ToString(this.ToInt());
                case ValueHandleType.Int64:
                    return XmlConverter.ToString(this.GetInt64());
                case ValueHandleType.UInt64:
                    return XmlConverter.ToString(this.GetUInt64());
                case ValueHandleType.Single:
                    return XmlConverter.ToString(this.GetSingle());
                case ValueHandleType.Double:
                    return XmlConverter.ToString(this.GetDouble());
                case ValueHandleType.Decimal:
                    return XmlConverter.ToString(this.GetDecimal());
                case ValueHandleType.DateTime:
                    return XmlConverter.ToString(this.ToDateTime());
                case ValueHandleType.TimeSpan:
                    return XmlConverter.ToString(this.ToTimeSpan());
                case ValueHandleType.Guid:
                    return XmlConverter.ToString(this.ToGuid());
                case ValueHandleType.UniqueId:
                    return XmlConverter.ToString(this.ToUniqueId());
                case ValueHandleType.UTF8:
                    return this.GetCharsText();
                case ValueHandleType.EscapedUTF8:
                    return this.GetEscapedCharsText();
                case ValueHandleType.Base64:
                    throw new NotSupportedException();
                case ValueHandleType.List:
                    throw new NotSupportedException();
                //return XmlConverter.ToString(this.ToList());
                case ValueHandleType.Char:
                    return this.GetCharText();
                case ValueHandleType.Unicode:
                    return this.GetUnicodeCharsText();
                case ValueHandleType.ConstString:
                    return ValueHandle.constStrings[this.offset];
                default:
                    throw new InvalidOperationException();
            }
        }

        public bool Equals2(string str, bool checkLower)
        {
            if (this.type != ValueHandleType.UTF8)
                return this.GetString() == str;
            if (this.length != str.Length)
                return false;
            byte[] buffer = this.bufferReader.Buffer;
            for (int index = 0; index < this.length; ++index)
            {
                byte num = buffer[index + this.offset];
                if (num != str[index] && (!checkLower || char.ToLowerInvariant((char)num) != str[index]))
                    return false;
            }
            return true;
        }

        //public object[] ToList()
        //{
        //    return this.bufferReader.GetList(this.offset, this.length);
        //}

        public object ToObject()
        {
            switch (this.type)
            {
                case ValueHandleType.Empty:
                case ValueHandleType.UTF8:
                case ValueHandleType.EscapedUTF8:
                case ValueHandleType.Dictionary:
                case ValueHandleType.Char:
                case ValueHandleType.Unicode:
                case ValueHandleType.ConstString:
                    return this.ToString();
                case ValueHandleType.True:
                case ValueHandleType.False:
                    return (this.ToBoolean() ? 1 : 0);
                case ValueHandleType.Zero:
                case ValueHandleType.One:
                case ValueHandleType.Int8:
                case ValueHandleType.Int16:
                case ValueHandleType.Int32:
                    return this.ToInt();
                case ValueHandleType.Int64:
                    return this.ToLong();
                case ValueHandleType.UInt64:
                    return this.GetUInt64();
                case ValueHandleType.Single:
                    return this.ToSingle();
                case ValueHandleType.Double:
                    return this.ToDouble();
                case ValueHandleType.Decimal:
                    return this.ToDecimal();
                case ValueHandleType.DateTime:
                    return this.ToDateTime();
                case ValueHandleType.TimeSpan:
                    return this.ToTimeSpan();
                case ValueHandleType.Guid:
                    return this.ToGuid();
                case ValueHandleType.UniqueId:
                    return this.ToUniqueId();
                case ValueHandleType.Base64:
                    return this.ToByteArray();
                case ValueHandleType.List:
                    throw new NotSupportedException("ToObject List");
                //return (object)this.ToList();
                default:
                    throw new InvalidOperationException();
            }
        }

        public bool TryReadBase64(byte[] buffer, int iOffset, int count, out int actual)
        {
            if (this.type == ValueHandleType.Base64)
            {
                actual = Math.Min(this.length, count);
                this.GetBase64(buffer, iOffset, actual);
                this.offset += actual;
                this.length -= actual;
                return true;
            }
            //if (this.type == ValueHandleType.UTF8 && count >= 3)
            //{
            //    if (this.length % 4 == 0)
            //    {
            //        try
            //        {
            //            int charCount = Math.Min(count / 3 * 4, this.length);
            //            actual = ValueHandle.Base64Encoding.GetBytes(this.bufferReader.Buffer, this.offset, charCount, buffer, offset);
            //            this.offset += charCount;
            //            this.length -= charCount;
            //            return true;
            //        }
            //        catch (FormatException ex)
            //        {
            //        }
            //    }
            //}
            actual = 0;
            return false;
        }

         public bool TryReadChars(char[] chars, int iOffset, int count, out int actual)
        {
            if (this.type == ValueHandleType.Unicode)
                return this.TryReadUnicodeChars(chars, iOffset, count, out actual);
            if (this.type != ValueHandleType.UTF8)
            {
                actual = 0;
                return false;
            }

            int charOffset = offset;
            int charCount = count;
            byte[] bytes = bufferReader.Buffer;
            int byteOffset = this.offset;
            int byteCount = this.length;

            while (true)
            {
                while (charCount > 0 && byteCount > 0)
                {

                    byte b = bytes[byteOffset];

                    if (b >= 0x80)
                        break;

                    chars[charOffset] = (char)b;
                    byteOffset++;
                    byteCount--;
                    charOffset++;
                    charCount--;
                }

                if (charCount == 0 || byteCount == 0)
                    break;

                int actualByteCount;
                int actualCharCount;

                UTF8Encoding encoding = new UTF8Encoding(false, true);
                // If we're asking for more than are possibly available, or more than are truly available then we can return the entire thing
                if (charCount >= encoding.GetMaxCharCount(byteCount) || charCount >= encoding.GetCharCount(bytes, byteOffset, byteCount))
                {
                    actualCharCount = encoding.GetChars(bytes, byteOffset, byteCount, chars, charOffset);
                    actualByteCount = byteCount;
                }
                else
                {
                    Decoder decoder = encoding.GetDecoder();
                    // Since x bytes can never generate more than x characters this is a safe estimate as to what will fit
                    actualByteCount = Math.Min(charCount, byteCount);

                    // We use a decoder so we don't error if we fall across a character boundary
                    actualCharCount = decoder.GetChars(bytes, byteOffset, actualByteCount, chars, charOffset);

                    // We might've gotten zero characters though if < 3 chars were requested
                    // (e.g. 1 char requested, 1 char in the buffer represented in 3 bytes) 
                    while (actualCharCount == 0)
                    {
                        // Request a few more bytes to get at least one character
                        actualCharCount = decoder.GetChars(bytes, byteOffset + actualByteCount, 1, chars, charOffset);
                        actualByteCount++;
                    }

                    // Now that we actually retrieved some characters, figure out how many bytes it actually was
                    actualByteCount = encoding.GetByteCount(chars, charOffset, actualCharCount);
                }

                // Advance
                byteOffset += actualByteCount;
                byteCount -= actualByteCount;
                charOffset += actualCharCount;
                charCount -= actualCharCount;

            }
            this.offset = byteOffset;
            this.length = byteCount;

            actual = (count - charCount);

            return true;
        }

        private bool TryReadUnicodeChars(char[] chars, int iOffset, int count, out int actual)
        {
            int charCount = Math.Min(count, this.length / 2);
            for (int index = 0; index < charCount; ++index)
                chars[iOffset + index] = (char)this.bufferReader.GetInt16(this.offset + index * 2);
            this.offset += charCount * 2;
            this.length -= charCount * 2;
            actual = charCount;
            return true;
        }


        public bool TryGetByteArrayLength(out int oLength)
        {
            if (this.type == ValueHandleType.Base64)
            {
                oLength = this.length;
                return true;
            }

            oLength = 0;
            return false;
        }

        private string GetCharsText()
        {
            return this.bufferReader.GetString(this.offset, this.length);
        }

        private string GetUnicodeCharsText()
        {
            return this.bufferReader.GetUnicodeString(this.offset, this.length);
        }

        private string GetEscapedCharsText()
        {
            return this.bufferReader.GetEscapedString(this.offset, this.length);
        }

        private string GetCharText()
        {
            int @char = this.GetChar();
            return @char <= ushort.MaxValue ? ((char)@char).ToString() : null;
        }

        private int GetChar()
        {
            return this.offset;
        }

        private int GetInt8()
        {
            return this.bufferReader.GetInt8(this.offset);
        }

        private int GetInt16()
        {
            return this.bufferReader.GetInt16(this.offset);
        }

        private int GetInt32()
        {
            return this.bufferReader.GetInt32(this.offset);
        }

        private long GetInt64()
        {
            return this.bufferReader.GetInt64(this.offset);
        }

        private ulong GetUInt64()
        {
            return this.bufferReader.GetUInt64(this.offset);
        }

        private float GetSingle()
        {
            return this.bufferReader.GetSingle(this.offset);
        }

        private double GetDouble()
        {
            return this.bufferReader.GetDouble(this.offset);
        }

        private Decimal GetDecimal()
        {
            return this.bufferReader.GetDecimal(this.offset);
        }

        private UniqueId GetUniqueId()
        {
            return this.bufferReader.GetUniqueId(this.offset);
        }

        private Guid GetGuid()
        {
            return this.bufferReader.GetGuid(this.offset);
        }

        private void GetBase64(byte[] buffer, int iOffset, int count)
        {
            this.bufferReader.GetBase64(this.offset, buffer, iOffset, count);
        }

    }
}
