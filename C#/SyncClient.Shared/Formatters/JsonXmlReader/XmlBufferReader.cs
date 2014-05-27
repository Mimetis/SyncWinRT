using System;
using System.IO;
using System.Xml;

namespace Microsoft.Synchronization.ClientServices
{
    /// <summary>
    /// XmlBufferReader reads a buffer of 128 Byte
    /// It parse it and can get various values as Int, string etc...
    /// It parse too Ampersand, Quote, DoubleQuote etc ...
    /// </summary>
    public class XmlBufferReader
    {
        private const int BufferAllocation = 128;

        private static readonly byte[] EmptyByteArray = new byte[0];
        private static readonly XmlBufferReader EmptyBufferReader = new XmlBufferReader();

        private byte[] buffer;
        private char[] chars;
        // Represent a byte array of a guid
        private byte[] guid;
        private int offset;
        private int offsetMax;
        private Stream stream;
        private byte[] streamBuffer;

        private int windowOffsetMax;

        /// <summary>
        /// Get an empty XmlBufferReader
        /// </summary>
        public static XmlBufferReader Empty
        {
            get { return EmptyBufferReader; }
        }

        /// <summary>
        /// Get Buffer array
        /// </summary>
        public byte[] Buffer
        {
            get { return buffer; }
        }

        /// <summary>
        /// Get boolean indicating if stream is positionned
        /// </summary>
        public bool IsStreamed
        {
            get { return stream != null; }
        }

        /// <summary>
        /// Get if end of file reached
        /// </summary>
        public bool EndOfFile
        {
            get
            {
                if (offset == offsetMax)
                    return !TryEnsureByte();

                return false;
            }
        }

        public int Offset
        {
            get { return offset; }
            set { offset = value; }
        }

        /// <summary>
        /// Set XmlBufferReader with a stream
        /// </summary>
        public void SetBuffer(Stream iStream)
        {
            if (streamBuffer == null)
                streamBuffer = new byte[BufferAllocation];

            SetBuffer(iStream, streamBuffer, 0, 0);

            windowOffsetMax = streamBuffer.Length;
        }


        /// <summary>
        /// Internal Set Buffer method
        /// </summary>
        private void SetBuffer(Stream iStream, byte[] iBuffer, int iOffset, int count)
        {
            this.stream = iStream;
            this.buffer = iBuffer;
            this.offset = iOffset;
            this.offsetMax = iOffset + count;
        }

        /// <summary>
        /// CLose the XmlBufferReader
        /// </summary>
        public void Close()
        {
            if (streamBuffer != null && streamBuffer.Length > 4096)
                streamBuffer = null;

            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
            buffer = EmptyByteArray;

            offset = 0;
            offsetMax = 0;
            windowOffsetMax = 0;
        }

        /// <summary>
        /// Get the current byte
        /// </summary>
        public byte GetByte()
        {
            // Get the current index
            int index = offset;

            if (index < offsetMax)
                return buffer[index];

            return GetByteHard();
        }

        /// <summary>
        /// Skip a byte
        /// </summary>
        public void SkipByte()
        {
            Advance(1);
        }

        /// <summary>
        /// Get the current Byte
        /// </summary>
        /// <returns></returns>
        private byte GetByteHard()
        {
            EnsureByte();
            return buffer[offset];
        }

        /// <summary>
        /// Get an array of byte and return the array and the offset
        /// </summary>
        public byte[] GetBuffer(int count, out int outOffset)
        {
            outOffset = this.offset;

            if (outOffset <= offsetMax - count)
                return buffer;

            return GetBufferHard(count, out outOffset);
        }

        /// <summary>
        /// Return the buffer, the current offset and the good offsetMax possible
        /// </summary>
        public byte[] GetBuffer(int count, out int outOffset, out int outOffsetMax)
        {
            // Set the current offset
            outOffset = this.offset;

            // Set outOffsetMax
            if (outOffset <= this.offsetMax - count)
            {
                outOffsetMax = this.offset + count;
            }
            else
            {
                // if we need a count of byte > offsetMax possible , we try ensure Bytes
                TryEnsureBytes(Math.Min(count, windowOffsetMax - outOffset));

                outOffsetMax = this.offsetMax;
            }

            return buffer;
        }

        /// <summary>
        /// Return the buffer and the current offset and offsetMax
        /// </summary>
        public byte[] GetBuffer(out int outOffset, out int outOffsetMax)
        {
            outOffset = this.offset;
            outOffsetMax = this.offsetMax;
            return buffer;
        }

        /// <summary>
        /// This method try to get a buffer wehn offset + count is potentially > offsetMax
        /// </summary>
        private byte[] GetBufferHard(int count, out int outOffset)
        {
            outOffset = this.offset;
            EnsureBytes(count);
            return buffer;
        }

        /// <summary>
        /// Ensure byte is present and we can get byte
        /// </summary>
        private void EnsureByte()
        {
            if (TryEnsureByte())
                return;

            throw new EndOfStreamException();
        }

        /// <summary>
        /// Ensure Bytes are present and we can get bytes (count of byte)
        /// </summary>
        /// <param name="count"></param>
        private void EnsureBytes(int count)
        {
            if (TryEnsureBytes(count))
                return;

            throw new Exception("XmlExceptionHelper.ThrowUnexpectedEndOfFile(this.reader)");
        }

        /// <summary>
        /// Try ensure byte on the stream
        /// </summary>
        /// <returns></returns>
        private bool TryEnsureByte()
        {

            if (stream == null)
                return false;

            if (offsetMax >= windowOffsetMax)
                throw new Exception("ThrowMaxBytesPerReadExceeded");

            if (offsetMax >= buffer.Length)
                return TryEnsureBytes(1);

            int b = stream.ReadByte();

            if (b == -1)
                return false;

            buffer[offsetMax++] = (byte)b;

            return true;
        }

        /// <summary>
        /// Try ensure bytes (of count) in the stream
        /// </summary>
        private bool TryEnsureBytes(int count)
        {
            if (stream == null)
                return false;

            if (offset > int.MaxValue - count)
                throw new Exception(
                    "XmlExceptionHelper.ThrowMaxBytesPerReadExceeded(this.reader, this.windowOffsetMax - this.windowOffset);");

            int newOffsetMax = offset + count;

            if (newOffsetMax < offsetMax)
                return true;

            if (newOffsetMax > windowOffsetMax)
                throw new Exception(
                    "XmlExceptionHelper.ThrowMaxBytesPerReadExceeded(this.reader, this.windowOffsetMax - this.windowOffset);");

            if (newOffsetMax > buffer.Length)
            {
                var numBuffer = new byte[Math.Max(newOffsetMax, buffer.Length * 2)];
                System.Buffer.BlockCopy(buffer, 0, numBuffer, 0, offsetMax);
                buffer = numBuffer;
                streamBuffer = numBuffer;
            }

            int needed = newOffsetMax - offsetMax;

            while (needed > 0)
            {
                int actual = stream.Read(buffer, offsetMax, needed);
                if (actual == 0)
                    return false;
                offsetMax += actual;
                needed -= actual;
            }

            return true;
        }

        /// <summary>
        /// Advance the offset
        /// </summary>
        public void Advance(int count)
        {
            offset += count;
        }

        /// <summary>
        /// Set Window offset and legnth
        /// </summary>
        public void SetWindow(int iWindowOffset, int iWindowLength)
        {
            // Set the limit
            if (iWindowOffset > int.MaxValue - iWindowLength)
                iWindowLength = int.MaxValue - iWindowOffset;


            if (this.offset != iWindowOffset)
            {
                System.Buffer.BlockCopy(buffer, offset, buffer, iWindowOffset, offsetMax - offset);
                offsetMax = iWindowOffset + (offsetMax - offset);
                offset = iWindowOffset;
            }

            windowOffsetMax = Math.Max(iWindowOffset + iWindowLength, offsetMax);
        }

        /// <summary>
        /// Read a number of bytes
        /// </summary>
        public int ReadBytes(int count)
        {
            int iOffset = offset;

            if (iOffset > offsetMax - count)
                EnsureBytes(count);

            offset += count;

            return iOffset;
        }

        /// <summary>
        /// Read UInt31
        /// </summary>
        /// <returns></returns>
        public int ReadMultiByteUInt31()
        {
            int i = GetByte();

            Advance(1);

            if ((i & BufferAllocation) == 0)
                return i;

            int num2 = i & sbyte.MaxValue;

            int j = GetByte();

            Advance(1);

            int num4 = num2 | (j & sbyte.MaxValue) << 7;

            if ((j & BufferAllocation) == 0)
                return num4;

            int k = GetByte();

            Advance(1);

            int num6 = num4 | (k & sbyte.MaxValue) << 14;

            if ((k & BufferAllocation) == 0)
                return num6;

            int l = GetByte();

            Advance(1);

            int num8 = num6 | (l & sbyte.MaxValue) << 21;

            if ((l & BufferAllocation) == 0)
                return num8;

            int m = GetByte();

            Advance(1);

            int num10 = num8 | m << 28;

            if ((m & 248) != 0)
                throw new Exception("XmlExceptionHelper.ThrowInvalidBinaryFormat(this.reader);");

            return num10;
        }

        /// <summary>
        /// Read an UInt8 (a byte)
        /// </summary>
        public int ReadUInt8()
        {
            byte currentByte = GetByte();
            Advance(1);
            return currentByte;
        }

        /// <summary>
        /// Read a Int8
        /// </summary>
        /// <returns></returns>
        public int ReadInt8()
        {
            return (sbyte)ReadUInt8();
        }

        /// <summary>
        /// Read an UInt16
        /// </summary>
        public int ReadUInt16()
        {
            int currentOffset;

            //Get the nex two Byte
            byte[] currentBuffer = GetBuffer(2, out currentOffset);

            int num = currentBuffer[currentOffset] + (currentBuffer[currentOffset + 1] << 8);

            // Advance the offset on 2
            Advance(2);

            return num;
        }

        /// <summary>
        /// Read an Int16
        /// </summary>
        /// <returns></returns>
        public int ReadInt16()
        {
            return (Int16)ReadUInt16();
        }

        /// <summary>
        /// Read an Int32 from the current Ofset
        /// </summary>
        public int ReadInt32()
        {
            int currentOffset;

            // Get the buffer, get the currentOffset and Ensure 4 bytes are available
            byte[] currentBuffer = GetBuffer(4, out currentOffset);

            byte b1 = currentBuffer[currentOffset];
            byte b2 = currentBuffer[currentOffset + 1];
            byte b3 = currentBuffer[currentOffset + 2];
            byte b4 = currentBuffer[currentOffset + 3];

            // so we have 4 byte, we can advance the offset of 4
            Advance(4);

            // Compute and return Int32
            return (((b4 << 8) + b3 << 8) + b2 << 8) + b1;
        }

        /// <summary>
        /// Read a UInt31
        /// </summary>
        public int ReadUInt31()
        {
            int i = ReadInt32();

            if (i < 0)
                throw new Exception("XmlExceptionHelper.ThrowInvalidBinaryFormat(this.reader);");

            return i;
        }

        /// <summary>
        /// Read an int64 from the current Offset Position
        /// </summary>
        /// <returns></returns>
        public long ReadInt64()
        {
            Int64 lo = (UInt32)ReadInt32();
            Int64 hi = (UInt32)ReadInt32();

            return (hi << 32) + lo;
        }

        /// <summary>
        /// Return A unique identifier optimized for Guids
        /// </summary>
        public UniqueId ReadUniqueId()
        {
            int currentOffset;

            // Get the buffer and ensure 16 bytes are available, then get the currentOffset
            var b = GetBuffer(16, out currentOffset);

            // Mahe the UniqeId
            var uniqueId = new UniqueId(b, currentOffset);

            // Advance of 16 position
            Advance(16);

            return uniqueId;
        }

        /// <summary>
        /// Read a DateTime
        /// </summary>
        public DateTime ReadDateTime()
        {
            // Read an Int64
            long dateData = ReadInt64();
            // Parse the data
            return DateTime.FromBinary(dateData);
        }

        /// <summary>
        /// Read a TimeSpan
        /// </summary>
        public TimeSpan ReadTimeSpan()
        {
            long l = ReadInt64();
            return TimeSpan.FromTicks(l);
        }

        /// <summary>
        /// Read a guid
        /// </summary>
        public Guid ReadGuid()
        {
            int currentOffset;
            GetBuffer(16, out currentOffset);

            Guid currentGuid = GetGuid(currentOffset);

            Advance(16);
            return currentGuid;
        }

        /// <summary>
        /// Rad an UTF 8 String
        /// </summary>
        public string ReadUTF8String(int length)
        {
            int currentOffset;

            this.GetBuffer(length, out currentOffset);

            char[] charBuffer = this.GetCharBuffer(length);

            int intChars = this.GetChars(currentOffset, length, charBuffer);
            string str = new string(charBuffer, 0, intChars);

            this.Advance(length);

            return str;
        }


        private char[] GetCharBuffer(int count)
        {
            if (count > 1024)
                return new char[count];
            if (chars == null || chars.Length < count)
                chars = new char[count];
            return chars;
        }

        private int GetChars(int iOffset, int iLength, char[] iChars)
        {
            byte[] currentBuffer = this.buffer;
            for (int charOffset = 0; charOffset < iLength; ++charOffset)
            {
                byte num = currentBuffer[iOffset + charOffset];

                if (num >= BufferAllocation)
                    return charOffset + XmlConverter.ToChars(currentBuffer, iOffset + charOffset, iLength - charOffset, iChars, charOffset);

                iChars[charOffset] = (char)num;
            }
            return iLength;
        }

        private int GetChars(int iOffset, int iLength, char[] iChars, int iCharOffset)
        {
            byte[] currentBuffer = this.buffer;
            for (int index = 0; index < iLength; ++index)
            {
                byte num = currentBuffer[offset + index];

                if (num >= BufferAllocation)
                    return index + XmlConverter.ToChars(currentBuffer, offset + index, iLength - index, iChars, iCharOffset + index);

                iChars[iCharOffset + index] = (char)num;
            }
            return iLength;
        }

        public string GetString(int iOffset, int iLength)
        {
            char[] charBuffer = this.GetCharBuffer(iLength);
            int charsLength = this.GetChars(iOffset, iLength, charBuffer);
            return new string(charBuffer, 0, charsLength);
        }

        public string GetUnicodeString(int iOffset, int iLength)
        {
            return XmlConverter.ToStringUnicode(this.buffer, iOffset, iLength);
        }

        public string GetString(int iOffset, int iLength, XmlNameTable nameTable)
        {
            char[] charBuffer = this.GetCharBuffer(iLength);
            int charsLength = this.GetChars(iOffset, iLength, charBuffer);
            return nameTable.Add(charBuffer, 0, charsLength);
        }

        public int GetEscapedChars(int iOffset, int iLength, char[] iChars)
        {
            byte[] numArray = this.buffer;
            int charCount = 0;
            int textOffset = iOffset;
            int iOffsetMax = iOffset + iLength;

            while (true)
            {
                while (iOffset < iOffsetMax && IsAttrChar(buffer[offset]))
                    iOffset++;

                charCount += GetChars(textOffset, iOffset - textOffset, iChars, charCount);

                if (iOffset == iOffsetMax)
                    break;

                textOffset = iOffset;

                switch ((int)numArray[iOffset])
                {
                    case Keys.Ampersand:
                        {

                            while (iOffset < iOffsetMax && numArray[iOffset] != Keys.SemiColon)
                                ++iOffset;

                            ++iOffset;

                            int charEntity = this.GetCharEntity(textOffset, iOffset - textOffset);

                            textOffset = iOffset;

                            if (charEntity > ushort.MaxValue)
                                throw new Exception("Not a good Character");

                            iChars[charCount++] = (char)charEntity;

                        }
                        break;
                    case Keys.HorizontalTab:
                    case Keys.LineFeed:
                        {
                            iChars[charCount++] = ' ';
                            iOffset++;
                            textOffset = iOffset;
                        }
                        break;
                    default: // '\r'
                        {
                            iChars[charCount++] = ' ';
                            iOffset++;
                            if (iOffset < iOffsetMax && numArray[offset] == '\n')
                                iOffset++;
                            textOffset = iOffset;
                        }
                        break;
                }
            }
            return charCount;

        }

        private bool IsAttrChar(int ch)
        {
            switch (ch)
            {
                case Keys.HorizontalTab:
                case Keys.LineFeed:
                case Keys.CarriageReturn:
                case Keys.Ampersand:
                    return false;
                default:
                    return true;
            }
        }

        public string GetEscapedString(int iOffset, int iLength)
        {
            char[] charBuffer = this.GetCharBuffer(iLength);
            int escapedChars = this.GetEscapedChars(iOffset, iLength, charBuffer);
            return new string(charBuffer, 0, escapedChars);
        }

        public string GetEscapedString(int iOffset, int length, XmlNameTable nameTable)
        {
            char[] charBuffer = this.GetCharBuffer(length);
            int escapedChars = this.GetEscapedChars(iOffset, length, charBuffer);
            return nameTable.Add(charBuffer, 0, escapedChars);
        }


        /// <summary>
        /// Get Char entity
        /// If char entity like &....;
        /// And length >= 3
        /// </summary>
        public int GetCharEntity(int iOffset, int length)
        {

            if (length < 3)
                throw new Exception(" XmlExceptionHelper.ThrowInvalidCharRef(this.reader);");

            byte[] numArray = buffer;

            switch (buffer[offset + 1])
            {
                case Keys.LowerA:
                    // if it's an "&amp;"
                    if (numArray[iOffset + 2] == Keys.LowerM)
                        return GetAmpersandCharEntity(iOffset, length);

                    return GetApostropheCharEntity(iOffset, length);
                case Keys.Diese:
                    if (numArray[iOffset + 2] == Keys.LowerX)
                        return GetHexCharEntity(iOffset, length);
                    return GetDecimalCharEntity(iOffset, length);
                case Keys.LowerG:
                    return GetGreaterThanCharEntity(length);
                case Keys.LowerL:
                    return GetLessThanCharEntity(iOffset, length);
                case Keys.LowerQ:
                    return GetQuoteCharEntity(iOffset, length);
                default:
                    throw new Exception("XmlExceptionHelper.ThrowInvalidCharRef(this.reader);");
            }
        }

        /// <summary>
        /// Get <!-- < --> if Buffer = &lt;
        /// </summary>
        private int GetLessThanCharEntity(int iOffset, int length)
        {
            byte[] iBuffer = this.buffer;

            if (length != 4 ||
                iBuffer[offset + 1] != (byte)'l' ||
                iBuffer[offset + 2] != (byte)'t')

                throw new Exception("XmlExceptionHelper.ThrowInvalidCharRef(this.reader);");

            return Keys.Inferior;
        }

        /// <summary>
        /// Get > if buffer == &gt;
        /// </summary>
        private int GetGreaterThanCharEntity(int length)
        {
            byte[] iBuffer = this.buffer;

            if (length != 4 ||
                iBuffer[offset + 1] != (byte)'g' ||
                iBuffer[offset + 2] != (byte)'t')

                throw new Exception("XmlExceptionHelper.ThrowInvalidCharRef(this.reader);");

            return Keys.Superior;
        }

        /// <summary>
        /// Get quote if buffer == &quot;
        /// </summary>
        private int GetQuoteCharEntity(int iOffset, int length)
        {
            byte[] iBuffer = this.buffer;

            if (length != 6 ||
                iBuffer[offset + 1] != (byte)'q' ||
                iBuffer[offset + 2] != (byte)'u' ||
                iBuffer[offset + 3] != (byte)'o' ||
                iBuffer[offset + 4] != (byte)'t')

                throw new Exception("XmlExceptionHelper.ThrowInvalidCharRef(this.reader);");

            return Keys.DoubleQuote;
        }

        /// <summary>
        /// Get the Ampersand if buffer == "&amp;"
        /// </summary>
        private int GetAmpersandCharEntity(int iOffset, int length)
        {
            byte[] iBuffer = this.buffer;

            if (length != 5 ||
                iBuffer[offset + 1] != (byte)'a' ||
                iBuffer[offset + 2] != (byte)'m' ||
                iBuffer[offset + 3] != (byte)'p')

                throw new Exception("XmlExceptionHelper.ThrowInvalidCharRef(this.reader);");

            return Keys.Ampersand;
        }

        /// <summary>
        /// Get apostrophe if buffer == &apos;
        /// </summary>
        private int GetApostropheCharEntity(int iOffset, int length)
        {
            byte[] iBuffer = this.buffer;

            if (length != 6 ||
                iBuffer[offset + 1] != (byte)'a' ||
                iBuffer[offset + 2] != (byte)'p' ||
                iBuffer[offset + 3] != (byte)'o' ||
                iBuffer[offset + 4] != (byte)'s')

                throw new Exception("XmlExceptionHelper.ThrowInvalidCharRef(this.reader);");

            return Keys.SingleQuote;
        }

        private int GetDecimalCharEntity(int iOffset, int length)
        {
            byte[] iBuffer = this.buffer;

            int value = 0;

            for (int i = 2; i < length - 1; i++)
            {

                byte ch = iBuffer[offset + i];

                if (ch < (byte)'0' || ch > (byte)'9')
                    throw new Exception("XmlExceptionHelper.ThrowInvalidCharRef(this.reader);");

                value = value * 10 + (ch - '0');

                if (value > 1114111)
                    throw new Exception("XmlExceptionHelper.ThrowInvalidCharRef(this.reader);");

            }

            return value;
        }

        public Double GetDouble(int iOffset)
        {
            return BitConverter.ToDouble(this.Buffer, iOffset);
        }

        public Single GetSingle(int iOffset)
        {
            return BitConverter.ToSingle(this.buffer, iOffset);
        }

        public Decimal GetDecimal(int iOffset)
        {

            byte[] numArray = this.buffer;

            var int1 = numArray[0 + iOffset] | numArray[1 + iOffset] << 8 | numArray[2 + iOffset] << 16 | numArray[3 + iOffset] << 24;
            var int2 = numArray[4 + iOffset] | numArray[5 + iOffset] << 8 | numArray[6 + iOffset] << 16 | numArray[7 + iOffset] << 24;
            var int3 = numArray[8 + iOffset] | numArray[9 + iOffset] << 8 | numArray[10 + iOffset] << 16 | numArray[11 + iOffset] << 24;
            var int4 = numArray[12 + iOffset] | numArray[13 + iOffset] << 8 | numArray[14 + iOffset] << 16 | numArray[15 + iOffset] << 24;

            Decimal d = new decimal(new[] { int1, int2, int3, int4 });

            return d;
        }

        private int GetHexCharEntity(int iOffset, int length)
        {

            byte[] iBuffer = this.buffer;
            int value = 0;

            for (int i = 3; i < length - 1; i++)
            {
                byte ch = iBuffer[offset + i];

                int digit;

                if (ch >= '0' && ch <= '9')
                    digit = (ch - '0');
                else if (ch >= 'a' && ch <= 'f')
                    digit = 10 + (ch - 'a');
                else if (ch >= 'A' && ch <= 'F')
                    digit = 10 + (ch - 'A');
                else
                    throw new Exception(" XmlExceptionHelper.ThrowInvalidCharRef(this.reader);");

                value = value * 16 + digit;

                if (value > 1114111)
                    throw new Exception(" XmlExceptionHelper.ThrowInvalidCharRef(this.reader);");
            }

            return value;
        }



        /// <summary>
        /// Get if a character is a WhiteSpace
        /// </summary>
        public static bool IsWhitespace(char ch)
        {
            if (ch > Keys.Space)
                return false;

            return ch == Keys.Space ||
                    ch == Keys.HorizontalTab ||
                    ch == Keys.CarriageReturn ||
                    ch == Keys.LineFeed;
        }



        public bool Equals2(int offset1, int length1, byte[] buffer2)
        {
            int length = buffer2.Length;
            if (length1 != length)
                return false;
            byte[] numArray = buffer;
            for (int index = 0; index < length1; ++index)
            {
                if (numArray[offset1 + index] != buffer2[index])
                    return false;
            }
            return true;
        }

        public bool Equals2(int offset1, int length1, XmlBufferReader bufferReader2, int offset2, int length2)
        {
            if (length1 != length2)
                return false;
            byte[] numArray1 = buffer;
            byte[] numArray2 = bufferReader2.buffer;
            for (int index = 0; index < length1; ++index)
            {
                if (numArray1[offset1 + index] != numArray2[offset2 + index])
                    return false;
            }
            return true;
        }

        public bool Equals2(int offset1, int length1, int offset2, int length2)
        {
            if (length1 != length2)
                return false;
            if (offset1 == offset2)
                return true;
            byte[] numArray = buffer;
            for (int index = 0; index < length1; ++index)
            {
                if (numArray[offset1 + index] != numArray[offset2 + index])
                    return false;
            }
            return true;
        }


        public int Compare(int offset1, int length1, int offset2, int length2)
        {
            byte[] numArray = buffer;
            int maxLength = Math.Min(length1, length2);

            for (int index = 0; index < maxLength; ++index)
            {
                int compare = numArray[offset1 + index] - numArray[offset2 + index];
                if (compare != 0)
                    return compare;
            }
            return length1 - length2;
        }

        public byte GetByte(int iOffset)
        {
            return buffer[iOffset];
        }

        public int GetInt8(int iOffset)
        {
            return (sbyte)GetByte(iOffset);
        }

        public int GetInt16(int iOffset)
        {
            byte[] numArray = buffer;
            return (short)(numArray[iOffset] + (numArray[iOffset + 1] << 8));
        }

        public int GetInt32(int iOffset)
        {
            byte[] numArray = buffer;

            return BitConverter.ToInt32(numArray, iOffset);
        }

        public long GetInt64(int iOffset)
        {
            byte[] numArray = buffer;

            return BitConverter.ToInt64(numArray, iOffset);
        }

        public ulong GetUInt64(int iOffset)
        {
            return (ulong)GetInt64(iOffset);
        }


        public UniqueId GetUniqueId(int iOffset)
        {
            return new UniqueId(buffer, iOffset);
        }

        /// <summary>
        /// Get a Guid from an offset
        /// </summary>
        public Guid GetGuid(int currentOffset)
        {
            if (guid == null)
                guid = new byte[16];

            // Copy 16 byte from buffer into guid array
            System.Buffer.BlockCopy(buffer, currentOffset, guid, 0, guid.Length);

            // return the guid
            return new Guid(guid);
        }

        /// <summary>
        /// Copy from a part of buffer in outBuffer
        /// </summary>
        public void GetBase64(int srcOffset, byte[] outBuffer, int dstOffset, int count)
        {
            System.Buffer.BlockCopy(this.buffer, srcOffset, outBuffer, dstOffset, count);
        }


        /// <summary>
        /// Skip a byte
        /// </summary>
        public void SkipNodeType()
        {
            SkipByte();
        }


    }
}