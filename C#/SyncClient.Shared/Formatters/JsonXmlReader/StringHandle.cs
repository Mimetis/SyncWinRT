using System;
using System.Diagnostics;
using System.Xml;

namespace Microsoft.Synchronization.ClientServices
{
    public class StringHandle
    {
        private enum StringHandleType
        {
            UTF8,
            EscapedUTF8,
            ConstString,
        }
        
        private static readonly string[] ConstStrings = new[]
        {
            "type",
            "root",
            "item"
        };

        private readonly XmlBufferReader bufferReader;
        private int key;
        private int length;
        private int offset;
        private StringHandleType type;

        static StringHandle()
        {
        }

        public StringHandle(XmlBufferReader bufferReader)
        {
            this.bufferReader = bufferReader;
            SetValue(0, 0);
        }

        public bool IsEmpty
        {
            get
            {
                if (type == StringHandleType.UTF8)
                    return length == 0;

                return Equals2(string.Empty);
            }
        }

        public bool IsXmlns
        {
            get
            {
                if (type != StringHandleType.UTF8)
                    return Equals2("xmlns");

                if (length != 5)
                    return false;

                byte[] buffer = bufferReader.Buffer;
                int index = offset;

                //return true if "xmlns"
                return buffer[index] ==     Keys.LowerX 
                    && buffer[index + 1] == Keys.LowerM 
                    && buffer[index + 2] == Keys.LowerL 
                    && buffer[index + 3] == Keys.LowerN 
                    && buffer[index + 4] == Keys.LowerS;
            }
        }

        public static bool operator ==(StringHandle s1, string s2)
        {
            return s1.Equals2(s2);
        }

        public static bool operator !=(StringHandle s1, string s2)
        {
            return !s1.Equals2(s2);
        }

        public static bool operator ==(StringHandle s1, StringHandle s2)
        {
            return s1.Equals2(s2);
        }

        public static bool operator !=(StringHandle s1, StringHandle s2)
        {
            return !s1.Equals2(s2);
        }

        public void SetValue(int iOffset, int iLength)
        {
            type = StringHandleType.UTF8;
            offset = iOffset;
            this.length = iLength;
        }

        public void SetConstantValue(StringHandleConstStringType constStringType)
        {
            type = StringHandleType.ConstString;
            key = (int) constStringType;
        }

        public void SetValue(int iOffset, int iLength, bool escaped)
        {
            type = escaped ? StringHandleType.EscapedUTF8 : StringHandleType.UTF8;
            this.offset = iOffset;
            this.length = iLength;
        }


        public void SetValue(StringHandle value)
        {
            type = value.type;
            key = value.key;
            offset = value.offset;
            length = value.length;
        }


        public string GetString(XmlNameTable nameTable)
        {
            switch (type)
            {
                case StringHandleType.UTF8:
                    return bufferReader.GetString(offset, length, nameTable);
                case StringHandleType.ConstString:
                    return nameTable.Add(ConstStrings[key]);
                default:
                    return bufferReader.GetEscapedString(offset, length, nameTable);
            }
        }

        public string GetString()
        {
            switch (type)
            {
                case StringHandleType.UTF8:
                    return bufferReader.GetString(offset, length);
                case StringHandleType.ConstString:
                    return ConstStrings[key];
                default:
                    return bufferReader.GetEscapedString(offset, length);
            }
        }

        public byte[] GetString(out int iOffset, out int iLength)
        {
            switch (type)
            {
                case StringHandleType.UTF8:
                    iOffset = this.offset;
                    iLength = this.length;
                    return bufferReader.Buffer;
                case StringHandleType.ConstString:
                    byte[] constBytes = XmlConverter.ToBytes(ConstStrings[key]);
                    iOffset = 0;
                    iLength = constBytes.Length;
                    return constBytes;
                default:
                    byte[] bytes = XmlConverter.ToBytes(bufferReader.GetEscapedString(this.offset, this.length));
                    iOffset = 0;
                    iLength = bytes.Length;
                    return bytes;
            }
        }

        public bool TryGetDictionaryString(out XmlDictionaryString value)
        {
            if (IsEmpty)
            {
                value = XmlDictionaryString.Empty;
                return true;
            }

            value = null;
            return false;
        }

        public override string ToString()
        {
            return GetString();
        }


        private bool Equals2(string s2)
        {
            return GetString() == s2;
        }

        private bool Equals2(int offset2, int length2, XmlBufferReader bufferReader2)
        {
            switch (type)
            {
                case StringHandleType.UTF8:
                    return bufferReader.Equals2(offset, length, bufferReader2, offset2, length2);
                default:
                    return GetString() == bufferReader.GetString(offset2, length2);
            }
        }

        private bool Equals2(StringHandle s2)
        {
            switch (s2.type)
            {
                case StringHandleType.UTF8:
                    return Equals2(s2.offset, s2.length, s2.bufferReader);
                default:
                    return Equals2(s2.GetString());
            }
        }

        public int CompareTo(StringHandle that)
        {
            if (type == StringHandleType.UTF8 && that.type == StringHandleType.UTF8)
                return bufferReader.Compare(offset, length, that.offset, that.length);

            return string.Compare(GetString(), that.GetString(), StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            var stringHandle = obj as StringHandle;
            if (ReferenceEquals(stringHandle, null))
                return false;

            return this == stringHandle;
        }

        public override int GetHashCode()
        {
            return GetString().GetHashCode();
        }

    }
}