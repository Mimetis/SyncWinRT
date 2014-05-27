using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
//using Newtonsoft.Json;

using WriteState = System.Xml.WriteState;

namespace Microsoft.Synchronization.ClientServices
{
    internal class XmlJsonWriter : XmlWriter
    {
        private string attributeText;
        private JsonDataType dataType;
        private int depth;
        private bool endElementBuffer;
        private bool isWritingDataTypeAttribute;
        private bool isWritingServerTypeAttribute;
        private bool isWritingXmlnsAttribute;
        private NameState nameState;
        private JsonNodeType nodeType;
        private StreamWriter nodeWriter;
        private JsonNodeType[] scopes;
        private string serverTypeValue;
        private WriteState writeState;
        private bool wroteServerTypeAttribute;

        const char HIGH_SURROGATE_START = (char)55296;
        const char LOW_SURROGATE_END = (char)57343;
        const char MAX_CHAR = (char)65534;

    
        public XmlJsonWriter(Stream stream)
        {
            if (nodeWriter == null)
                nodeWriter = new StreamWriter(stream);

            InitializeWriter();
        }

        public override XmlWriterSettings Settings
        {
            get { return null; }
        }

        public override WriteState WriteState
        {
            get
            {
                if (writeState == WriteState.Closed)
                    return WriteState.Closed;
                if (HasOpenAttribute)
                    return WriteState.Attribute;
                switch (nodeType)
                {
                    case JsonNodeType.None:
                        return WriteState.Start;
                    case JsonNodeType.Element:
                        return WriteState.Element;
                    case JsonNodeType.EndElement:
                    case JsonNodeType.QuotedText:
                    case JsonNodeType.StandaloneText:
                        return WriteState.Content;
                    default:
                        return WriteState.Error;
                }
            }
        }

        public override string XmlLang
        {
            get { return null; }
        }

        public override XmlSpace XmlSpace
        {
            get { return XmlSpace.None; }
        }

        private bool HasOpenAttribute
        {
            get
            {
                if (!isWritingDataTypeAttribute && !isWritingServerTypeAttribute && !IsWritingNameAttribute)
                    return isWritingXmlnsAttribute;
                return true;
            }
        }

        private bool IsClosed
        {
            get { return WriteState == WriteState.Closed; }
        }

        private bool IsWritingCollection
        {
            get
            {
                if (depth > 0)
                    return scopes[depth] == JsonNodeType.Collection;
                return false;
            }
        }

        private bool IsWritingNameAttribute
        {
            get { return (nameState & NameState.IsWritingNameAttribute) == NameState.IsWritingNameAttribute; }
        }

        private bool IsWritingNameWithMapping
        {
            get { return (nameState & NameState.IsWritingNameWithMapping) == NameState.IsWritingNameWithMapping; }
        }

        private bool WrittenNameWithMapping
        {
            get { return (nameState & NameState.WrittenNameWithMapping) == NameState.WrittenNameWithMapping; }
        }

        public void Close()
        {
            if (IsClosed)
                return;
            try
            {

                WriteEndDocument();
            }
            finally
            {
                try
                {
                    nodeWriter.Flush();
                    //nodeWriter.Close();
                }
                finally
                {
                    writeState = WriteState.Closed;
                    depth = 0;
                }
            }
        }

        public override void Flush()
        {
            if (IsClosed)
                ThrowClosed();
            nodeWriter.Flush();
        }

        public override string LookupPrefix(string ns)
        {
            if (ns == null)
                throw new ArgumentException("ns");
            if (ns == "http://www.w3.org/2000/xmlns/")
                return "xmlns";
            if (ns == "http://www.w3.org/XML/1998/namespace")
                return "xml";

            return ns == string.Empty ? string.Empty : null;
        }


        public override void WriteBase64(byte[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentException("buffer");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", "ValueMustBeNonNegative");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "ValueMustBeNonNegative");
            if (count > buffer.Length - index)
                throw new ArgumentOutOfRangeException("count", "JsonSizeExceedsRemainingBufferSpace");

            StartText();

            nodeWriter.Write(buffer);
        }

        public override void WriteBinHex(byte[] buffer, int index, int count)
        {
            throw new NotSupportedException("WriteBinHex");
        }

        public override void WriteCData(string text)
        {
            this.WriteString(text);
        }

        public override void WriteCharEntity(char ch)
        {
            this.WriteString(ch.ToString());
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentException("buffer");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", "ValueMustBeNonNegative");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "ValueMustBeNonNegative");
            if (count > buffer.Length - index)
                throw new ArgumentOutOfRangeException("count", "JsonSizeExceedsRemainingBufferSpace");

            this.WriteString(new string(buffer, index, count));
        }

        public override void WriteComment(string text)
        {
            throw new NotSupportedException("WriteComment");
        }

        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
            throw new NotSupportedException("WriteDocType");
        }

        public override void WriteEndAttribute()
        {
            if (IsClosed)
                ThrowClosed();

            if (!HasOpenAttribute)
                throw new XmlException("JsonNoMatchingStartAttribute");

            if (isWritingDataTypeAttribute)
            {
                switch (attributeText)
                {
                    case "number":
                        ThrowIfServerTypeWritten("number");
                        dataType = JsonDataType.Number;
                        break;
                    case "string":
                        ThrowIfServerTypeWritten("string");
                        dataType = JsonDataType.String;
                        break;
                    case "array":
                        ThrowIfServerTypeWritten("array");
                        dataType = JsonDataType.Array;
                        break;
                    case "object":
                        dataType = JsonDataType.Object;
                        break;
                    case "null":
                        ThrowIfServerTypeWritten("null");
                        dataType = JsonDataType.Null;
                        break;
                    case "boolean":
                        ThrowIfServerTypeWritten("boolean");
                        dataType = JsonDataType.Boolean;
                        break;
                    default:
                        throw new XmlException("JsonUnexpectedAttributeValue");
                }
                attributeText = null;

                isWritingDataTypeAttribute = false;

                if (IsWritingNameWithMapping && !WrittenNameWithMapping)
                    return;

                WriteDataTypeServerType();
            }
            else if (isWritingServerTypeAttribute)
            {
                serverTypeValue = attributeText;

                attributeText = null;

                isWritingServerTypeAttribute = false;

                if (IsWritingNameWithMapping && !WrittenNameWithMapping || dataType != JsonDataType.Object)
                    return;

                WriteServerTypeAttribute();
            }
            else if (IsWritingNameAttribute)
            {
                WriteJsonElementName(attributeText);
                attributeText = null;
                nameState = NameState.IsWritingNameWithMapping | NameState.WrittenNameWithMapping;
                WriteDataTypeServerType();
            }
            else
            {
                if (!isWritingXmlnsAttribute)
                    return;
                attributeText = null;
                isWritingXmlnsAttribute = false;
            }
        }

        public override void WriteEndDocument()
        {
            if (IsClosed)
                ThrowClosed();
            if (nodeType == JsonNodeType.None)
                return;
            while (depth > 0)
                WriteEndElement();
        }

        public override void WriteEndElement()
        {
            if (IsClosed)
                ThrowClosed();
            if (depth == 0)
                throw new XmlException("JsonEndElementNoOpenNodes");
            if (HasOpenAttribute)
                throw new XmlException("JsonOpenAttributeMustBeClosedFirst");

            endElementBuffer = false;

            JsonNodeType jsonNodeType = ExitScope();

            if (jsonNodeType == JsonNodeType.Collection)
            {
                //nodeWriter.WriteEndArray();
                nodeWriter.Write("]");
                jsonNodeType = ExitScope();
            }
            else if (nodeType == JsonNodeType.QuotedText)
                WriteJsonQuote();
            else if (nodeType == JsonNodeType.Element)
            {
                if (dataType == JsonDataType.None && serverTypeValue != null)
                    throw new XmlException("JsonMustSpecifyDataType");
                if (IsWritingNameWithMapping && !WrittenNameWithMapping)
                    throw new XmlException("JsonMustSpecifyDataType");
                if (dataType == JsonDataType.None || dataType == JsonDataType.String)
                {
                    nodeWriter.Write(@"""");
                    nodeWriter.Write(@"""");
                }
            }
            if (depth != 0)
            {
                if (jsonNodeType == JsonNodeType.Element)
                    endElementBuffer = true;
                else if (jsonNodeType == JsonNodeType.Object)
                {
                    nodeWriter.Write("}");

                    if (depth > 0 && scopes[depth] == JsonNodeType.Element)
                    {
                        ExitScope();
                        endElementBuffer = true;
                    }
                }
            }
            dataType = JsonDataType.None;
            nodeType = JsonNodeType.EndElement;
            nameState = NameState.None;
            wroteServerTypeAttribute = false;
        }

        public override void WriteEntityRef(string name)
        {
            throw new NotSupportedException("JsonMethodNotSupported");
        }

        public override void WriteFullEndElement()
        {
            WriteEndElement();
        }

        public override void WriteProcessingInstruction(string name, string text)
        {
            if (IsClosed)
                ThrowClosed();
            if (!name.Equals("xml", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("JsonXmlProcessingInstructionNotSupported");
            if (WriteState != WriteState.Start)
                throw new XmlException("JsonXmlInvalidDeclaration");
        }

        public override void WriteQualifiedName(string localName, string ns)
        {
            if (localName == null)
                throw new ArgumentException("localName");
            if (localName.Length == 0)
                throw new ArgumentException("localName", "JsonInvalidLocalNameEmpty");
            if (ns == null)
                ns = string.Empty;
            base.WriteQualifiedName(localName, ns);
        }

        public override void WriteRaw(string data)
        {
            this.nodeWriter.Write(data);
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            throw new NotSupportedException("WriteRaw (char[] buffer, int index, int count)");
        }

        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            if (IsClosed)
                ThrowClosed();
            if (!string.IsNullOrEmpty(prefix))
            {
                if (IsWritingNameWithMapping && prefix == "xmlns")
                {
                    if (ns != null && ns != "http://www.w3.org/2000/xmlns/")
                        throw new ArgumentException("XmlPrefixBoundToNamespace");
                }
                else
                    throw new ArgumentException("prefix", "JsonPrefixMustBeNullOrEmpty");
            }
            else if (IsWritingNameWithMapping && ns == "http://www.w3.org/2000/xmlns/" && localName != "xmlns")
                prefix = "xmlns";
            if (!string.IsNullOrEmpty(ns))
            {
                if (IsWritingNameWithMapping && ns == "http://www.w3.org/2000/xmlns/")
                    prefix = "xmlns";
                else
                    throw new ArgumentException("ns", "JsonNamespaceMustBeEmpty");
            }
            if (localName == null)
                throw new ArgumentException("localName");
            if (localName.Length == 0)
                throw new ArgumentException("localName", "JsonInvalidLocalNameEmpty");
            if (nodeType != JsonNodeType.Element && !wroteServerTypeAttribute)
                throw new XmlException("JsonAttributeMustHaveElement");
            if (HasOpenAttribute)
                throw new XmlException("JsonOpenAttributeMustBeClosedFirst");

            if (prefix == "xmlns")
                isWritingXmlnsAttribute = true;
            else switch (localName)
                {
                    case "type":
                        if (dataType != JsonDataType.None)
                            throw new XmlException("JsonAttributeAlreadyWritten");

                        isWritingDataTypeAttribute = true;

                        break;
                    case "__type":
                        if (serverTypeValue != null)
                            throw new XmlException("JsonAttributeAlreadyWritten");

                        if (dataType != JsonDataType.None && dataType != JsonDataType.Object)
                            throw new XmlException("JsonServerTypeSpecifiedForInvalidDataType");
                        isWritingServerTypeAttribute = true;
                        break;
                    case "item":
                        if (WrittenNameWithMapping)
                            throw new XmlException("JsonAttributeAlreadyWritten");

                        if (!IsWritingNameWithMapping)
                            throw new XmlException("JsonEndElementNoOpenNodes");

                        nameState |= NameState.IsWritingNameAttribute;
                        break;
                    default:
                        throw new ArgumentException("localName", "JsonUnexpectedAttributeLocalName");
                }
        }

        public override void WriteStartDocument(bool standalone)
        {
            WriteStartDocument();
        }

        public override void WriteStartDocument()
        {
            if (IsClosed)
                ThrowClosed();
            if (WriteState == WriteState.Start)
                return;

            throw new XmlException("JsonInvalidWriteState");
        }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            if (localName == null)
                throw new ArgumentException("localName");
            if (localName.Length == 0)
                throw new ArgumentException("localName", "JsonInvalidLocalNameEmpty");
            if (!string.IsNullOrEmpty(prefix) &&
                (string.IsNullOrEmpty(ns) || !TrySetWritingNameWithMapping(localName, ns)))
                throw new ArgumentException("prefix", "JsonPrefixMustBeNullOrEmpty");
            if (!string.IsNullOrEmpty(ns) && !TrySetWritingNameWithMapping(localName, ns))
                throw new ArgumentException("ns", "JsonNamespaceMustBeEmpty");
            if (IsClosed)
                ThrowClosed();
            if (HasOpenAttribute)
                throw new XmlException("JsonOpenAttributeMustBeClosedFirst");
            if (nodeType != JsonNodeType.None && depth == 0)
                throw new XmlException("JsonMultipleRootElementsNotAllowedOnWriter");

            switch (nodeType)
            {
                case JsonNodeType.None:
                    if (!localName.Equals("root"))
                        throw new XmlException("JsonInvalidRootElementName");

                    EnterScope(JsonNodeType.Element);
                    break;
                case JsonNodeType.Element:
                    if (dataType != JsonDataType.Array && dataType != JsonDataType.Object)
                        throw new XmlException("JsonNodeTypeArrayOrObjectNotSpecified");
                    if (!IsWritingCollection)
                    {
                        if (nameState != NameState.IsWritingNameWithMapping)
                            WriteJsonElementName(localName);
                    }
                    else if (!localName.Equals("item"))
                        throw new XmlException("JsonInvalidItemNameForArrayElement");
                    EnterScope(JsonNodeType.Element);
                    break;
                case JsonNodeType.EndElement:
                    if (endElementBuffer)
                        nodeWriter.Write(",");
                    if (!IsWritingCollection)
                    {
                        if (nameState != NameState.IsWritingNameWithMapping)
                            WriteJsonElementName(localName);
                    }
                    else if (!localName.Equals("item"))
                        throw new XmlException("JsonInvalidItemNameForArrayElement");
                    EnterScope(JsonNodeType.Element);
                    break;
                default:
                    // ISSUE: reference to a compiler-generated method
                    throw new XmlException("JsonInvalidStartElementCall");
            }
            isWritingDataTypeAttribute = false;
            isWritingServerTypeAttribute = false;
            isWritingXmlnsAttribute = false;
            wroteServerTypeAttribute = false;
            serverTypeValue = null;
            dataType = JsonDataType.None;
            nodeType = JsonNodeType.Element;
        }

        public override void WriteString(string text)
        {
            if (HasOpenAttribute && text != null)
            {
                string str = this.attributeText + text;
                this.attributeText = str;
            }
            else
            {
                StartText();
                WriteEscapedJsonString(text);
            }
        }

        
        private void WriteEscapedJsonString(string str)
        {

            char[] buffer = Encoding.UTF8.GetChars(Encoding.UTF8.GetBytes(str));

            int num = 0;
            int index;
            for (index = 0; index < buffer.Length; ++index)
            {
                char ch = str[index];
                if (ch <= Keys.SlashForward)
                {
                    if (ch == Keys.SlashForward || ch == Keys.DoubleQuote)
                    {
                        this.nodeWriter.Write(buffer, num, index - num);
                        this.nodeWriter.Write((char)Keys.BackSlash);
                        this.nodeWriter.Write(ch);
                        num = index + 1;
                    }
                    else if (ch < Keys.Space)
                    {
                        this.nodeWriter.Write(buffer, num, index - num);
                        this.nodeWriter.Write((char)Keys.BackSlash);
                        this.nodeWriter.Write((char)Keys.LowerU);
                        this.nodeWriter.Write(string.Format(CultureInfo.InvariantCulture, "{0:x4}", ch));
                        num = index + 1;
                    }
                }
                else if (ch == Keys.BackSlash)
                {
                    this.nodeWriter.Write(buffer, num, index - num);
                    this.nodeWriter.Write((char)Keys.BackSlash);
                    this.nodeWriter.Write(ch);
                    num = index + 1;
                }
                else if (ch >= HIGH_SURROGATE_START && (ch <= LOW_SURROGATE_END || ch >= MAX_CHAR))
                {
                    this.nodeWriter.Write(buffer, num, index - num);
                    this.nodeWriter.Write((char)Keys.BackSlash);
                    this.nodeWriter.Write((char)Keys.LowerU);
                    this.nodeWriter.Write(string.Format(CultureInfo.InvariantCulture, "{0:x4}", ch));
                    num = index + 1;
                }
            }
            if (num < index)
                this.nodeWriter.Write(buffer, num, index - num);
        }


        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            this.WriteString((string)(object)highChar + lowChar);
        }

        public override void WriteValue(bool value)
        {
            StartText();
            nodeWriter.Write(value);
        }

        public override void WriteValue(Decimal value)
        {
            StartText();
            nodeWriter.Write(value);
        }

        public override void WriteValue(double value)
        {
            StartText();
            nodeWriter.Write(value);
        }

        public override void WriteValue(float value)
        {
            StartText();
            nodeWriter.Write(value);
        }

        public override void WriteValue(int value)
        {
            StartText();
            nodeWriter.Write(value);
        }

        public override void WriteValue(long value)
        {
            StartText();
            nodeWriter.Write(value);
        }

        public void WriteValue(Guid value)
        {
            StartText();
            nodeWriter.Write(value);
        }

        public void WriteValue(DateTime value)
        {
            StartText();
            nodeWriter.Write(value);
        }

        public override void WriteValue(string value)
        {
            (this).WriteString(value);
        }

        public void WriteValue(TimeSpan value)
        {
            StartText();
            nodeWriter.Write(value);
        }

        public void WriteValue(UniqueId value)
        {
            if (value == null)
                throw new ArgumentException("value");
            StartText();
            nodeWriter.Write(value);
        }

        public override void WriteValue(object value)
        {
            if (IsClosed)
                ThrowClosed();
            if (value == null)
                throw new ArgumentException("value");
            if (value is Array)
                WriteValue((Array)value);
            else
                WritePrimitiveValue(value);
        }

        public override void WriteWhitespace(string ws)
        {
            if (IsClosed)
                ThrowClosed();

            if (ws == null)
                throw new ArgumentException("ws");

            this.nodeWriter.Write(ws);

        }

         internal static bool CharacterNeedsEscaping(char ch)
        {
            if (ch == Keys.SlashForward || ch == Keys.DoubleQuote || ch < Keys.Space || ch == Keys.BackSlash)
                return true;

            if (ch < HIGH_SURROGATE_START)
                return false;

            if (ch > LOW_SURROGATE_END)
                return ch >= MAX_CHAR;

            return true;
        }

        private static void ThrowClosed()
        {
            throw new InvalidOperationException("JsonWriterClosed");
        }

        private void CheckText(JsonNodeType nextNodeType)
        {
            if (IsClosed)
                ThrowClosed();
            if (depth == 0)
                throw new InvalidOperationException("XmlIllegalOutsideRoot");
            if (nextNodeType == JsonNodeType.StandaloneText && nodeType == JsonNodeType.QuotedText)
                throw new XmlException("JsonCannotWriteStandaloneTextAfterQuotedText");
        }

        private void EnterScope(JsonNodeType currentNodeType)
        {
            ++depth;
            if (scopes == null)
                scopes = new JsonNodeType[4];
            else if (scopes.Length == depth)
            {
                var jsonNodeTypeArray = new JsonNodeType[depth * 2];
                Array.Copy(scopes, jsonNodeTypeArray, depth);
                scopes = jsonNodeTypeArray;
            }
            scopes[depth] = currentNodeType;
        }

        private JsonNodeType ExitScope()
        {
            JsonNodeType jsonNodeType = scopes[depth];
            scopes[depth] = JsonNodeType.None;
            --depth;
            return jsonNodeType;
        }

        private void InitializeWriter()
        {
            nodeType = JsonNodeType.None;
            dataType = JsonDataType.None;
            isWritingDataTypeAttribute = false;
            wroteServerTypeAttribute = false;
            isWritingServerTypeAttribute = false;
            serverTypeValue = null;
            attributeText = null;
            depth = 0;

            if (scopes != null && scopes.Length > 25)
                scopes = null;
            writeState = WriteState.Start;
            endElementBuffer = false;
        }

        private void StartText()
        {
            if (HasOpenAttribute)
                throw new InvalidOperationException("JsonMustUseWriteStringForWritingAttributeValues");

            if (dataType == JsonDataType.None && serverTypeValue != null)
                throw new XmlException("JsonMustSpecifyDataType");

            if (IsWritingNameWithMapping && !WrittenNameWithMapping)
                throw new XmlException("JsonMustSpecifyDataType");

            switch (dataType)
            {
                case JsonDataType.None:
                case JsonDataType.String:
                    CheckText(JsonNodeType.QuotedText);

                    if (nodeType != JsonNodeType.QuotedText)
                        WriteJsonQuote();

                    nodeType = JsonNodeType.QuotedText;
                    break;
                case JsonDataType.Boolean:
                case JsonDataType.Number:
                    CheckText(JsonNodeType.StandaloneText);
                    nodeType = JsonNodeType.StandaloneText;
                    break;
            }
        }

        private void ThrowIfServerTypeWritten(string dataTypeSpecified)
        {
            if (serverTypeValue == null)
                return;

            throw new XmlException("JsonInvalidDataTypeSpecifiedForServerType");
        }


        private bool TrySetWritingNameWithMapping(string localName, string ns)
        {
            if (!localName.Equals("item") || !ns.Equals("item"))
                return false;
            nameState = NameState.IsWritingNameWithMapping;
            return true;
        }

        private void WriteDataTypeServerType()
        {
            if (dataType == JsonDataType.None)
                return;
            switch (dataType)
            {
                case JsonDataType.Null:
                    nodeWriter.Write("null");
                    break;
                case JsonDataType.Object:
                    EnterScope(JsonNodeType.Object);
                    nodeWriter.Write("{");
                    break;
                case JsonDataType.Array:
                    EnterScope(JsonNodeType.Collection);
                    nodeWriter.Write("[");
                    break;
            }
            if (serverTypeValue == null)
                return;
            WriteServerTypeAttribute();
        }


        private void WriteJsonElementName(string localName)
        {
            WriteJsonQuote();
            nodeWriter.Write(localName);
            WriteJsonQuote();
            nodeWriter.Write(":");
        }

        private void WriteJsonQuote()
        {
            nodeWriter.Write(@"""");
        }

        private void WritePrimitiveValue(object value)
        {
            if (IsClosed)
                ThrowClosed();
            if (value == null)
                throw new ArgumentNullException("value");
            if (value is ulong)
                WriteValue((ulong)value);
            else if (value is string)
                WriteValue((string)value);
            else if (value is int)
                WriteValue((int)value);
            else if (value is long)
                WriteValue((long)value);
            else if (value is bool)
                WriteValue((bool)value);
            else if (value is double)
                WriteValue((double)value);
            else if (value is DateTime)
                ((XmlWriter)this).WriteValue((DateTime)value);
            else if (value is float)
                (this).WriteValue((float)value);
            else if (value is Decimal)
                (this).WriteValue((Decimal)value);
            else if (value is XmlDictionaryString)
                base.WriteValue(value);
            else if (value is UniqueId)
                base.WriteValue(value);
            else if (value is Guid)
                base.WriteValue((Guid)value);
            else if (value is TimeSpan)
            {
                base.WriteValue((TimeSpan)value);
            }
            else
            {
                if (value.GetType().IsArray)
                    throw new ArgumentException("JsonNestedArraysNotSupported");
                
                base.WriteValue(value);
            }
        }

        private void WriteServerTypeAttribute()
        {
            string str = serverTypeValue;
            JsonDataType jsonDataType = dataType;
            NameState _nameState = this.nameState;
            (this).WriteStartElement("__type");
            (this).WriteValue(str);
            WriteEndElement();
            dataType = jsonDataType;
            this.nameState = _nameState;
            wroteServerTypeAttribute = true;
        }

        private void WriteValue(ulong value)
        {
            StartText();
            nodeWriter.Write(value);
        }

        private void WriteValue(Array array)
        {
            JsonDataType jsonDataType = dataType;
            dataType = JsonDataType.String;
            StartText();
            for (int index = 0; index < array.Length; ++index)
            {
                if (index != 0)
                    nodeWriter.Write(" ");
                WritePrimitiveValue(array.GetValue(index));
            }
            dataType = jsonDataType;
        }

        #region Nested type: JsonDataType

        private enum JsonDataType
        {
            None,
            Null,
            Boolean,
            Number,
            String,
            Object,
            Array,
        }

        #endregion

        #region Nested type: NameState

        [Flags]
        private enum NameState
        {
            None = 0,
            IsWritingNameWithMapping = 1,
            IsWritingNameAttribute = 2,
            WrittenNameWithMapping = 4,
        }

        #endregion
    }
}