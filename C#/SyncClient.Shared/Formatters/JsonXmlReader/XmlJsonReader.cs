using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.Synchronization.ClientServices
{
    public class XmlJsonReader2 : XmlReader
    {

        public override int AttributeCount
        {
            get { throw new NotImplementedException(); }
        }

        public override string BaseURI
        {
            get { throw new NotImplementedException(); }
        }

        public override int Depth
        {
            get { throw new NotImplementedException(); }
        }

        public override bool EOF
        {
            get { throw new NotImplementedException(); }
        }

        public override string GetAttribute(string name, string namespaceURI)
        {
            throw new NotImplementedException();
        }

        public override string GetAttribute(string name)
        {
            throw new NotImplementedException();
        }

        public override string GetAttribute(int i)
        {
            throw new NotImplementedException();
        }

        public override bool IsEmptyElement
        {
            get { throw new NotImplementedException(); }
        }

        public override string LocalName
        {
            get { throw new NotImplementedException(); }
        }

        public override string LookupNamespace(string prefix)
        {
            throw new NotImplementedException();
        }

        public override bool MoveToAttribute(string name, string ns)
        {
            throw new NotImplementedException();
        }

        public override bool MoveToAttribute(string name)
        {
            throw new NotImplementedException();
        }

        public override bool MoveToElement()
        {
            throw new NotImplementedException();
        }

        public override bool MoveToFirstAttribute()
        {
            throw new NotImplementedException();
        }

        public override bool MoveToNextAttribute()
        {
            throw new NotImplementedException();
        }

        public override XmlNameTable NameTable
        {
            get { throw new NotImplementedException(); }
        }

        public override string NamespaceURI
        {
            get { throw new NotImplementedException(); }
        }

        public override XmlNodeType NodeType
        {
            get { throw new NotImplementedException(); }
        }

        public override string Prefix
        {
            get { throw new NotImplementedException(); }
        }

        public override bool Read()
        {
            throw new NotImplementedException();
        }

        public override bool ReadAttributeValue()
        {
            throw new NotImplementedException();
        }

        public override ReadState ReadState
        {
            get { throw new NotImplementedException(); }
        }

        public override void ResolveEntity()
        {
            throw new NotImplementedException();
        }

        public override string Value
        {
            get { throw new NotImplementedException(); }
        }
    }
    public class XmlJsonReader : XmlReader
    {
        private static XmlInitialNode initialNode = new XmlInitialNode(XmlBufferReader.Empty);
        private static XmlEndOfFileNode endOfFileNode = new XmlEndOfFileNode(XmlBufferReader.Empty);
        private static XmlClosedNode closedNode = new XmlClosedNode(XmlBufferReader.Empty);

        private const string Xmlns = "xmlns";
        private const string Xml = "xml";
        private const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";
        private const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";

        // Memory Allocation
        private const int BufferAllocation = 128;
        private const int MaxTextChunk = 2048;

        // all elements in buffer
        private Dictionary<int, XmlElementNode> elementNodes = new Dictionary<int, XmlElementNode>();
        // all attributes in buffer
        private Dictionary<int, XmlAttributeNode> attributeNodes = new Dictionary<int, XmlAttributeNode>();

        // Dictionary containing the JsonNodeType of each depth 
        private readonly Dictionary<int, JsonNodeType> scopes = new Dictionary<int, JsonNodeType>();

        // Specifics Nodes
        private XmlAtomicTextNode atomicTextNode;
        private XmlComplexTextNode complexTextNode;
        private XmlWhitespaceTextNode whitespaceTextNode;
        private XmlCDataNode cdataNode;
        private XmlCommentNode commentNode;
        private XmlElementNode rootElementNode;


        private int depth;
        private int attributeCount;
        private int attributeStart;
        private XmlDictionaryReaderQuotas quotas;
        private XmlNameTable nameTable;
        private int attributeIndex;
        private string localName;
        private string value;
        private bool rootElement;
        private bool readingElement;

        private byte[] charactersToSkipOnNextRead;
        private JsonComplexTextMode complexTextMode = JsonComplexTextMode.None;

        // mark if first element is a complex one (beetween "{" and "}" )
        private bool expectingFirstElementInNonPrimitiveChild;

        // Gets the maximum allowed bytes returned for each read
        private int maxBytesPerRead;

        // Get the current scope depth
        private int scopeDepth;


        /// <summary>
        /// Current Xml Buffer Reader
        /// </summary>
        protected XmlBufferReader BufferReader { get; private set; }

        /// <summary>
        /// Get the current Node
        /// </summary>
        protected XmlNode Node { get; private set; }

        /// <summary>
        /// Get the current element node
        /// </summary>
        protected XmlElementNode ElementNode
        {
            get
            {
                if (this.depth == 0)
                    return this.rootElementNode;

                return this.elementNodes[this.depth];
            }
        }


        public override bool CanReadBinaryContent
        {
            get
            {
                return true;
            }
        }

        public override bool CanReadValueChunk
        {
            get
            {
                return true;
            }
        }

        public override string BaseURI
        {
            get
            {
                return string.Empty;
            }
        }

        public override bool HasValue
        {
            get
            {
                return this.Node.HasValue;
            }
        }

        public override bool IsDefault
        {
            get
            {
                return false;
            }
        }

        public override string this[int index]
        {
            get
            {
                return this.GetAttribute(index);
            }
        }

        public override string this[string name]
        {
            get
            {
                return this.GetAttribute(name);
            }
        }

        public override string this[string iLocalName, string namespaceUri]
        {
            get
            {
                return this.GetAttribute(iLocalName, namespaceUri);
            }
        }

        public override int AttributeCount
        {
            get
            {
                return this.Node.CanGetAttribute ? this.attributeCount : 0;
            }
        }

        public override sealed int Depth
        {
            get
            {
                return this.depth + this.Node.DepthDelta;
            }
        }

        public override bool EOF
        {
            get
            {
                return this.Node.ReadState == ReadState.EndOfFile;
            }
        }

        public override sealed bool IsEmptyElement
        {
            get
            {
                return this.Node.IsEmptyElement;
            }
        }

        public override string LocalName
        {
            get
            {
                if (this.localName == null)
                    this.localName = this.Node.LocalName.GetString(this.NameTable);

                return this.localName;
            }
        }

        public override string NamespaceURI
        {
            get
            {
                //if (this.ns == null)
                //    this.ns = this.Node.Namespace.Uri.GetString(this.NameTable);

                return "";
                //return this.ns;
            }
        }

        public override XmlNameTable NameTable
        {
            get
            {
                if (this.nameTable == null)
                {
                    this.nameTable = new NameTable();
                    this.nameTable.Add(Xml);
                    this.nameTable.Add(Xmlns);
                    this.nameTable.Add(XmlnsNamespace);
                    this.nameTable.Add(XmlNamespace);
                }
                return this.nameTable;
            }
        }

        public override sealed XmlNodeType NodeType
        {
            get
            {
                return this.Node.NodeType;
            }
        }

        public override string Prefix
        {
            get
            {
                return string.Empty;

            }
        }


        public override ReadState ReadState
        {
            get
            {
                return this.Node.ReadState;
            }
        }


        private bool IsAttributeValue
        {
            get
            {
                if (this.Node.NodeType != XmlNodeType.Attribute)
                    return this.Node is XmlAttributeTextNode;
                return true;
            }
        }

        private bool IsReadingCollection
        {
            get
            {
                if (scopeDepth > 0)
                    return scopes[scopeDepth] == JsonNodeType.Collection;

                return false;
            }
        }

        /// <summary>
        /// Get if current node is an XmlNodeType.Text
        /// </summary>
        private bool IsReadingComplexText
        {
            get
            {
                if (!this.Node.IsAtomicValue)
                    return this.Node.NodeType == XmlNodeType.Text;

                return false;
            }
        }


        public override string Value
        {
            get
            {
                return this.value ?? (this.value = this.Node.ValueAsString);
            }
        }

        public override Type ValueType
        {
            get
            {
                if (this.value == null)
                {
                    Type type = this.Node.Value.ToType();

                    if (this.Node.IsAtomicValue || type == typeof(byte[]))
                        return type;
                }

                return typeof(string);
            }
        }

        public override string XmlLang
        {
            get
            {
                return String.Empty;
            }
        }

        public override XmlSpace XmlSpace
        {
            get
            {
                return XmlSpace.None;
            }
        }




        /// <summary>
        /// Internal Constructor
        /// </summary>
        internal XmlJsonReader()
        {
            this.BufferReader = new XmlBufferReader();

            this.rootElementNode = new XmlElementNode(this.BufferReader);
            this.atomicTextNode = new XmlAtomicTextNode(this.BufferReader);
            this.Node = closedNode;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="stream">Stream to read</param>
        /// <param name="quotas"></param>
        public XmlJsonReader(Stream stream, XmlDictionaryReaderQuotas quotas)
            : this()
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            // Place on initial internal Node
            this.MoveToInitial(quotas);

            // Setting BufferReader to current Stream
            this.BufferReader.SetBuffer(stream);

            // Reset the State
            this.ResetState();
        }

        /// <summary>
        /// Reset State 
        /// </summary>
        private void ResetState()
        {

            complexTextMode = JsonComplexTextMode.None;

            // Set this bool to false
            // indicate than the first child is a complex type (beetween "{" and "}" )
            expectingFirstElementInNonPrimitiveChild = false;

            charactersToSkipOnNextRead = new byte[2];

            // Reset depth to 0
            scopeDepth = 0;

            // Clear the scopes dictionary
            scopes.Clear();
        }
        #region Move To ...

        /// <summary>
        /// Set Node of reader to this node
        /// Reset ns, localName, prefix, value
        /// </summary>
        /// <param name="xmlNode"></param>
        protected void MoveToNode(XmlNode xmlNode)
        {
            this.Node = xmlNode;
            this.localName = null;
            this.value = null;
        }


        /// <summary>
        /// Move to initial
        /// </summary>
        /// <param name="xmlQuotas"></param>
        protected void MoveToInitial(XmlDictionaryReaderQuotas xmlQuotas)
        {
            if (xmlQuotas == null)
                throw new ArgumentNullException("quotas");

            this.quotas = xmlQuotas;
            this.maxBytesPerRead = xmlQuotas.MaxBytesPerRead;
            this.depth = 0;
            this.attributeCount = 0;
            this.attributeStart = -1;
            this.attributeIndex = -1;
            this.rootElement = false;
            this.readingElement = false;
            this.MoveToNode(initialNode);
        }


        protected XmlCommentNode MoveToComment()
        {
            if (this.commentNode == null)
                this.commentNode = new XmlCommentNode(this.BufferReader);

            this.MoveToNode(this.commentNode);

            return this.commentNode;
        }

        protected XmlCDataNode MoveToCData()
        {
            if (this.cdataNode == null)
                this.cdataNode = new XmlCDataNode(this.BufferReader);

            this.MoveToNode(this.cdataNode);

            return this.cdataNode;
        }

        /// <summary>
        /// Tests whether the current content node is a start element or an empty element.
        /// </summary>
        public void MoveToStartElement()
        {
            if (base.IsStartElement())
                return;
            throw new XmlException("StartElementExpected(this)");
        }

        /// <summary>
        /// Tests whether the current content node is a start element or an empty element and if the <see cref="P:System.Xml.XmlReader.Name"/> property of the element matches the given argument.
        /// </summary>
        /// <param name="name">The <see cref="P:System.Xml.XmlReader.Name"/> property of the element.</param>
        public void MoveToStartElement(string name)
        {
            if (base.IsStartElement(name))
                return;
            throw new XmlException("StartElementExpected(this)");
        }


        protected XmlAtomicTextNode MoveToAtomicText()
        {
            XmlAtomicTextNode xmlAtomicTextNode = this.atomicTextNode;

            this.MoveToNode(xmlAtomicTextNode);

            return xmlAtomicTextNode;
        }

        protected XmlComplexTextNode MoveToComplexText()
        {
            if (this.complexTextNode == null)
                this.complexTextNode = new XmlComplexTextNode(this.BufferReader);

            this.MoveToNode(this.complexTextNode);

            return this.complexTextNode;
        }

        /// <summary>
        /// Move to Whitespace node
        /// </summary>
        /// <returns></returns>
        protected XmlTextNode MoveToWhitespaceText()
        {
            if (this.whitespaceTextNode == null)
                this.whitespaceTextNode = new XmlWhitespaceTextNode(this.BufferReader);

            this.whitespaceTextNode.NodeType = XmlNodeType.Whitespace;

            this.MoveToNode(this.whitespaceTextNode);

            return this.whitespaceTextNode;
        }


        /// <summary>
        /// Move to the last element
        /// </summary>
        protected void MoveToEndElement()
        {
            ExitJsonScope();

            if (this.depth == 0)
                throw new XmlException("ThrowInvalidBinaryFormat");

            XmlElementNode xmlElementNode = this.elementNodes[this.depth];

            XmlEndElementNode endElement = xmlElementNode.EndElement;

            this.MoveToNode(endElement);
        }

        /// <summary>
        /// Move to end of file
        /// </summary>
        protected void MoveToEndOfFile()
        {
            if (this.depth != 0)
                throw new XmlException("ThrowUnexpectedEndOfFile");

            this.MoveToNode(endOfFileNode);
        }
        #endregion


        /// <summary>
        ///  Enter a level and get ElementNode
        /// </summary>
        /// <returns></returns>
        protected XmlElementNode EnterScope()
        {
            // Just check if there is no multiple root elements
            if (this.depth == 0)
            {
                if (this.rootElement)
                    throw new XmlException("ThrowMultipleRootElements");

                this.rootElement = true;
            }

            // Increase depth
            ++this.depth;

            if (this.depth > this.quotas.MaxDepth)
                throw new XmlException("ThrowMaxDepthExceeded");

            // try get the element node
            XmlElementNode xmlElementNode;

            // Try to get elementNode from elementNodes
            // If not get it from BufferReader
            if (!this.elementNodes.TryGetValue(this.depth, out xmlElementNode))
            {
                xmlElementNode = new XmlElementNode(this.BufferReader);
                this.elementNodes[this.depth] = xmlElementNode;
            }

            this.attributeCount = 0;
            this.attributeStart = -1;
            this.attributeIndex = -1;

            // Move to this node
            this.MoveToNode(xmlElementNode);

            return xmlElementNode;
        }

        /// <summary>
        /// Exit a level and get back
        /// </summary>
        protected void ExitScope()
        {
            if (this.depth == 0)
                throw new XmlException("ThrowUnexpectedEndElement");

            --this.depth;
        }


        /// <summary>
        /// Add an attribute to the dictionary of attributes
        /// </summary>
        private XmlAttributeNode AddAttribute(bool isAtomicValue)
        {
            // Create a first tab of 4 elements
            XmlAttributeNode xmlAttributeNode;
            if (!this.attributeNodes.TryGetValue(this.attributeCount, out xmlAttributeNode))
            {
                xmlAttributeNode = new XmlAttributeNode(this.BufferReader);
                this.attributeNodes[this.attributeCount] = xmlAttributeNode;
            }

            xmlAttributeNode.IsAtomicValue = isAtomicValue;
            xmlAttributeNode.AttributeText.IsAtomicValue = isAtomicValue;

            ++this.attributeCount;

            return xmlAttributeNode;
        }


        protected XmlAttributeNode AddAttribute()
        {
            return this.AddAttribute(true);
        }

        protected XmlAttributeNode AddXmlAttribute()
        {
            return this.AddAttribute(true);
        }


        /// <summary>
        /// Close the XmlJsonReader
        /// </summary>
        public void Close()
        {
            // Position to closed node
            this.MoveToNode(closedNode);
            this.nameTable = null;

            this.attributeNodes.Clear();
            this.attributeNodes = null;

            this.elementNodes.Clear();
            this.elementNodes = null;

            // Close the buffer reader
            this.BufferReader.Close();

            this.ResetState();
        }

        /// <summary>
        /// Get the attribute node
        /// </summary>
        private XmlAttributeNode GetAttributeNode(int index)
        {
            if (!this.Node.CanGetAttribute)
                throw new ArgumentOutOfRangeException("index");

            if (index < 0)
                throw new ArgumentOutOfRangeException("index");

            if (index < this.attributeCount)
                return this.attributeNodes[index];

            throw new ArgumentOutOfRangeException("index");
        }

        private XmlAttributeNode GetAttributeNode(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            if (!this.Node.CanGetAttribute)
                return null;

            int length = name.IndexOf(':');
            string cPrefix;
            string cLocalName;

            if (length == -1)
            {
                if (name == Xmlns)
                {
                    cPrefix = Xmlns;
                    cLocalName = string.Empty;
                }
                else
                {
                    cPrefix = string.Empty;
                    cLocalName = name;
                }
            }
            else
            {
                cPrefix = name.Substring(0, length);
                cLocalName = name.Substring(length + 1);
            }

            Dictionary<int, XmlAttributeNode> xmlAttributeNodeArray = this.attributeNodes;

            int indexStart = this.attributeStart;
            for (int index2 = 0; index2 < this.attributeCount; ++index2)
            {
                if (++indexStart >= this.attributeCount)
                    indexStart = 0;

                XmlAttributeNode xmlAttributeNode = xmlAttributeNodeArray[indexStart];

                if (xmlAttributeNode.IsPrefixAndLocalName(cPrefix, cLocalName))
                {
                    this.attributeStart = indexStart;
                    return xmlAttributeNode;
                }
            }
            return null;
        }

        /// <summary>
        /// Internal method to get attribute
        /// </summary>
        private XmlAttributeNode GetAttributeNode(string iLocalName, string namespaceUri)
        {
            if (iLocalName == null)
                throw new ArgumentNullException("localName");

            if (namespaceUri == null)
                namespaceUri = string.Empty;

            if (!this.Node.CanGetAttribute)
                return null;

            Dictionary<int, XmlAttributeNode> xmlAttributeNodeArray = this.attributeNodes;

            int indAttributeStart = this.attributeStart;

            // Foreach attribute in array
            for (int indexAttribute = 0; indexAttribute < this.attributeCount; ++indexAttribute)
            {
                indAttributeStart += 1;

                if (indAttributeStart >= this.attributeCount)
                    indAttributeStart = 0;

                XmlAttributeNode xmlAttributeNode = xmlAttributeNodeArray[indAttributeStart];

                if (xmlAttributeNode.IsLocalNameAndNamespaceUri(iLocalName, namespaceUri))
                {
                    this.attributeStart = indAttributeStart;
                    return xmlAttributeNode;
                }
            }
            return null;
        }


        /// <summary>
        /// Get attribute from index
        /// </summary>
        public override string GetAttribute(int index)
        {
            var val = this.GetAttributeNode(index).ValueAsString;

            return UnescapeJsonString(val);
        }

        /// <summary>
        /// Get attribute from name
        /// </summary>
        public override string GetAttribute(string name)
        {
            XmlAttributeNode attributeNode = this.GetAttributeNode(name);

            if (attributeNode == null)
                return null;

            if (name != "type")
                return UnescapeJsonString(attributeNode.ValueAsString);

            return attributeNode.ValueAsString;
        }

        public override string GetAttribute(string iLocalName, string namespaceUri)
        {
            XmlAttributeNode attributeNode = this.GetAttributeNode(iLocalName, namespaceUri);

            if (attributeNode == null)
                return null;

            if (iLocalName != "type")
                return UnescapeJsonString(attributeNode.ValueAsString);

            return attributeNode.ValueAsString;
        }

        public override string LookupNamespace(string iPrefix)
        {
            return XmlnsNamespace;
        }

        /// <summary>
        /// Breaking Text
        /// </summary>
        private static int BreakText(byte[] buffer, int offset, int length)
        {
            // See if we might be breaking a utf8 sequence
            if (length > 0 && (buffer[offset + length - 1] & BufferAllocation) == BufferAllocation)
            {
                // Find the lead char of the utf8 sequence (0x11xxxxxx) 
                int originalLength = length;
                do
                {
                    --length;
                } while (length > 0 && (buffer[offset + length] & 192) != 192);

                // Couldn't find the lead char
                if (length == 0)
                    return originalLength;

                // Count how many bytes follow the lead char 
                byte b = (byte)(buffer[offset + length] << 2);

                int byteCount = 2;
                while ((b & BufferAllocation) == BufferAllocation)
                {
                    b <<= 1;
                    ++byteCount;

                    // There shouldn't be more than 3 bytes following the lead char 
                    if (byteCount > 4)
                        return originalLength; // Invalid utf8 sequence - can't break 
                }

                if (length + byteCount == originalLength)
                    return originalLength; // sequence fits exactly

                if (length == 0)
                    return originalLength; // Quota too small to read a char 
            }
            return length;
        }

        /// <summary>
        /// Get text length until "," or "]" or "}" or " "
        /// </summary>
        private static int ComputeNumericalTextLength(byte[] buffer, int offset, int offsetMax)
        {
            int beginOffset = offset;
            while (offset < offsetMax)
            {
                // Get ch from buffer
                byte ch = buffer[offset];

                // if Ch is , or ] or }  or space return length
                if (ch == Keys.Comma || ch == Keys.RightClosingBrace || ch == Keys.RightClosingBracket || IsWhitespace(ch))
                    return offset - beginOffset;

                offset++;
            }

            return offset - beginOffset;
        }

        /// <summary>
        /// Return the length of a Text until iOffsetMax or a BackSlash
        /// </summary>
        private static int ComputeQuotedTextLengthUntilEndQuote(byte[] iBuffer, int iOffset, int iOffsetMax,
                                                                out bool oEscaped)
        {
            // Assumes that for quoted text "someText", the first " has been consumed. 
            // For original text "someText", buffer passed in is someText".
            // This method returns return 8 for someText" (s, o, m, e, T, e, x, t). 
            oEscaped = false;
            int currentOffset;

            // Check for an escaped char
            for (currentOffset = iOffset; currentOffset < iOffsetMax; currentOffset++)
            {
                // Get Byte from Buffer
                byte cKey = iBuffer[currentOffset];

                // if < Keys.Space, not a valid Character
                if (cKey < Keys.Space)
                    throw new FormatException("InvalidCharacterEncountered");

                if (cKey == Keys.BackSlash || cKey == Keys.Unknown)
                {
                    oEscaped = true;
                    break;
                }
                if (cKey == Keys.DoubleQuote)
                    break;
            }

            return currentOffset - iOffset;
        }

        public override void MoveToAttribute(int index)
        {
            this.MoveToNode(this.GetAttributeNode(index));
        }

        public override bool MoveToAttribute(string name)
        {
            XmlNode node = this.GetAttributeNode(name);
            if (node == null)
                return false;
            this.MoveToNode(node);
            return true;
        }

        public override bool MoveToAttribute(string slocalName, string namespaceUri)
        {
            XmlNode node = this.GetAttributeNode(slocalName, namespaceUri);
            if (node == null)
                return false;
            this.MoveToNode(node);
            return true;
        }

        public override bool MoveToElement()
        {
            if (!this.Node.CanMoveToElement)
                return false;

            this.MoveToNode(this.elementNodes[this.depth]);
            this.attributeIndex = -1;
            return true;
        }

        public override XmlNodeType MoveToContent()
        {
            do
            {
                if (this.Node.HasContent)
                {
                    if (this.Node.NodeType == XmlNodeType.Text || this.Node.NodeType == XmlNodeType.CDATA)
                    {
                        if (this.value == null)
                        {
                            if (!this.Node.Value.IsWhitespace())
                                break;
                        }
                        else if (!XmlConverter.IsWhitespace(this.value))
                            break;
                    }
                    else
                        break;
                }
                else if (this.Node.NodeType == XmlNodeType.Attribute)
                {
                    this.MoveToElement();
                    break;
                }
            }
            while (this.Read());
            return this.Node.NodeType;
        }

        public override bool MoveToFirstAttribute()
        {
            if (!this.Node.CanGetAttribute || this.attributeCount == 0)
                return false;
            this.MoveToNode(this.GetAttributeNode(0));
            this.attributeIndex = 0;
            return true;
        }

        public override bool MoveToNextAttribute()
        {
            if (!this.Node.CanGetAttribute)
                return false;
            int index = this.attributeIndex + 1;
            if (index >= this.attributeCount)
                return false;
            this.MoveToNode(this.GetAttributeNode(index));
            this.attributeIndex = index;
            return true;
        }


        public override sealed bool IsStartElement()
        {
            switch (this.Node.NodeType)
            {
                case XmlNodeType.Element:
                    return true;
                case XmlNodeType.EndElement:
                    return false;
                case XmlNodeType.None:
                    this.Read();
                    if (this.Node.NodeType == XmlNodeType.Element)
                        return true;
                    break;
            }
            return this.MoveToContent() == XmlNodeType.Element;
        }

        public override bool IsStartElement(string name)
        {
            if (name == null)
                return false;

            int length = name.IndexOf(':');

            string strName = length == -1 ? name : name.Substring(length + 1);

            if ((this.Node.NodeType == XmlNodeType.Element || this.IsStartElement()))
                return this.Node.LocalName == strName;

            return false;
        }

        public override bool IsStartElement(string slocalName, string namespaceUri)
        {
            if (slocalName == null || namespaceUri == null || this.Node.NodeType != XmlNodeType.Element && !this.IsStartElement() || !(this.Node.LocalName == slocalName))
                return false;

            return this.Node.IsNamespaceUri(namespaceUri);
        }

        /// <summary>
        /// Buffer element
        /// </summary>
        private void BufferElement()
        {
            // Get currentOffset to remake position at the end
            int currentOffset = this.BufferReader.Offset;

            bool isColon = false;

            byte quoteOrDoubleQuote = 0;

            // While not isColon (:)
            while (!isColon)
            {
                int offset2;
                int offsetMax;

                // Get a BufferAllocation Buffer, the current Offset, and the OffsetMax
                byte[] buffer = this.BufferReader.GetBuffer(BufferAllocation, out offset2, out offsetMax);

                // if BufferReader returns a byte array < BufferAllocation bytes, leave
                // because we are at the end of the stream
                if (offset2 + BufferAllocation != offsetMax)
                    break;

                // Analyse the next BufferAllocation bytes
                for (int index = offset2; index < offsetMax && !isColon; ++index)
                {
                    byte currentChar = buffer[index];

                    // If it's a "\" advance of One and dont analyse even if it's a colon
                    if (currentChar == Keys.BackSlash)
                    {
                        // Advance index, if it's a quote or double quote after, it's not a delimiter, just a char
                        ++index;

                        // Check if we are not at the end
                        if (index >= offsetMax)
                            break;
                    }

                    // If i am in a text beetwen quote or double quote, dont analyse, because we can have a Colon in a text
                    // First Pass
                    else if (quoteOrDoubleQuote == 0)
                    {
                        if (currentChar == Keys.SingleQuote || currentChar == Keys.DoubleQuote)
                            quoteOrDoubleQuote = currentChar;

                        // Check if Current Char is not a Colon
                        if (currentChar == Keys.Colon)
                            isColon = true;
                    }
                    else if (currentChar == quoteOrDoubleQuote)
                        quoteOrDoubleQuote = 0;
                }


                // Advance the Buffer Reader of BufferAllocation
                this.BufferReader.Advance(BufferAllocation);

            }

            // reset Offset position
            this.BufferReader.Offset = currentOffset;
        }

        /// <summary>
        /// Enter a new Scope
        /// Increase scopeDepth
        /// </summary>
        /// <param name="currentNodeType"></param>
        private void EnterJsonScope(JsonNodeType currentNodeType)
        {
            // Increase Depth
            ++scopeDepth;

            // Set the JsonNodeType of the current depth 
            scopes[scopeDepth] = currentNodeType;
        }

        /// <summary>
        /// Exit the current scope and go to the last scope
        /// ScopeDepth decrement
        /// </summary>
        /// <returns></returns>
        private JsonNodeType ExitJsonScope()
        {
            // Get the Json Node Type of the current scope depth
            JsonNodeType jsonNodeType = scopes[scopeDepth];

            // Reset this Depth to JsonNodeType.None
            scopes[scopeDepth] = JsonNodeType.None;

            // Decrease depth
            --scopeDepth;

            // return the node type of the depth exited
            return jsonNodeType;
        }



        /// <summary>
        /// Return if a Char is a whitespace
        /// </summary>
        private static bool IsWhitespace(byte ch)
        {
            return (ch == Keys.Space || ch == Keys.HorizontalTab || ch == Keys.LineFeed || ch == Keys.CarriageReturn);
        }

        /// <summary>
        /// Parse a char
        /// </summary>
        private static char ParseChar(string value, NumberStyles style)
        {
            int num = ParseInt(value, style);
            return Convert.ToChar(num);
        }

        /// <summary>
        /// Parse an Integer
        /// </summary>
        private static int ParseInt(string value, NumberStyles style)
        {
            return int.Parse(value, style, NumberFormatInfo.InvariantInfo);
        }


        private void ParseAndSetLocalName()
        {
            XmlElementNode elementNode = this.EnterScope();

            elementNode.NameOffset = this.BufferReader.Offset;
            do
            {
                if (this.BufferReader.GetByte() == Keys.BackSlash)
                    ReadEscapedCharacter(false);
                else
                    ReadQuotedText(false);

            } while (complexTextMode == JsonComplexTextMode.QuotedText);

            int num1 = this.BufferReader.Offset - 1;
            elementNode.LocalName.SetValue(elementNode.NameOffset, num1 - elementNode.NameOffset);
            elementNode.NameLength = num1 - elementNode.NameOffset;
            elementNode.IsEmptyElement = false;
            elementNode.ExitScope = false;
            elementNode.BufferOffset = num1;

        }


        /// <summary>
        /// Parse Start Element
        /// </summary>
        private void ParseStartElement()
        {
            // We buffer element
            BufferElement();

            // Maybe the first element in this start element is a complex element (beetween { and })
            // we need to mark it for later
            expectingFirstElementInNonPrimitiveChild = false;

            // Get Byte
            byte currentByte = this.BufferReader.GetByte();

            // If it's a DoubleQuote
            if (currentByte != Keys.DoubleQuote)
                throw new Exception("TokenExpected");

            // Skip this Double quote Byte
            this.BufferReader.SkipByte();

            // Parse and set name
            ParseAndSetLocalName();

            // Skip white space
            SkipWhitespaceInBufferReader();

            // Skip expecter char
            SkipExpectedByteInBufferReader(Keys.Colon);

            // Again Skip White space
            SkipWhitespaceInBufferReader();

            // If we have a "{" so first element is a complex one
            if (this.BufferReader.GetByte() == Keys.LeftOpeningBrace)
            {
                // skip this byte
                this.BufferReader.SkipByte();

                // so true
                expectingFirstElementInNonPrimitiveChild = true;
            }

            // now read attributes
            ReadAttributes();
        }

        public override int ReadValueChunk(char[] iChars, int offset, int count)
        {

            if (iChars == null)
                throw new ArgumentNullException("chars");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset");
            if (offset > iChars.Length)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            if (count > iChars.Length - offset)
                throw new ArgumentOutOfRangeException("count");

            if (IsAttributeValue)
            {
                int actual;
                if (this.value == null && this.Node.Value.TryReadChars(iChars, offset, count, out actual))
                    return actual;

                string str = this.Value;

                actual = Math.Min(count, str.Length);

                str.CopyTo(0, iChars, offset, actual);

                this.value = str.Substring(actual);
                return actual;
            }

            string valueString = UnescapeJsonString(this.Node.ValueAsString);
            int actual2 = Math.Min(count, valueString.Length);
            if (actual2 > 0)
            {
                valueString.CopyTo(0, iChars, offset, actual2);

                this.Node.Value.SetValue(ValueHandleType.UTF8, 0, 0);
            }
            return actual2;
        }

        public override string ReadElementContentAsString()
        {

            if (this.Node.NodeType != XmlNodeType.Element)
                this.MoveToStartElement();

            if (this.Node.IsEmptyElement)
            {
                this.Read();
                return string.Empty;
            }

            this.Read();
            string str = this.ReadContentAsString();
            this.ReadEndElement();
            return str;
        }

        public string ReadElementString()
        {
            this.MoveToStartElement();
            if (this.IsEmptyElement)
            {
                this.Read();
                return string.Empty;
            }

            this.Read();
            string str = this.ReadElementContentAsString();
            this.ReadEndElement();
            return str;
        }

        public string ReadElementString(string name)
        {
            this.MoveToStartElement(name);
            return this.ReadElementString();
        }


        public override void ReadStartElement()
        {
            if (this.Node.NodeType != XmlNodeType.Element)
                this.MoveToStartElement();
            this.Read();
        }

        public override void ReadStartElement(string name)
        {
            this.MoveToStartElement(name);
            this.Read();
        }

        public override void ReadStartElement(string slocalName, string namespaceUri)
        {
            throw new NotSupportedException("namespace URI is not supported in this version");
        }

        public override void ReadEndElement()
        {
            if (this.Node.NodeType != XmlNodeType.EndElement && this.MoveToContent() != XmlNodeType.EndElement)
            {
                int index = this.Node.NodeType == XmlNodeType.Element ? this.depth - 1 : this.depth;

                if (index == 0)
                    throw new InvalidOperationException("XmlEndElementNoOpenNodes");

                throw new XmlException("EndElementExpected");
            }

            this.Read();
        }


        /// <summary>
        /// Read 
        /// </summary>
        public override bool Read()
        {

            // If we're positioned on an attribute or attribute text on an empty element, we need to move back
            // to the element in order to get the correct setting of ExitScope 
            if (this.Node.CanMoveToElement)
                MoveToElement();

            if (this.Node.ReadState == ReadState.Closed)
                return false;

            if (this.Node.ExitScope)
                this.ExitScope();

            this.BufferReader.SetWindow(this.ElementNode.BufferOffset, maxBytesPerRead);

            byte ch;

            // IF we are not reading a complex text, we are maybe at the end of a collection
            if (!IsReadingComplexText)
            {
                // Skip next white spaces
                SkipWhitespaceInBufferReader();

                // We get the next char
                // Maybe this char is a char to skip
                if (TryGetByte(out ch) && (charactersToSkipOnNextRead[0] == ch || charactersToSkipOnNextRead[1] == ch))
                {
                    // Skip this byte
                    this.BufferReader.SkipByte();
                    charactersToSkipOnNextRead[0] = 0;
                    charactersToSkipOnNextRead[1] = 0;
                }

                // Skip next white spaces
                SkipWhitespaceInBufferReader();

                // We are reading a collection and this collection is closed
                if (TryGetByte(out ch) && ch == Keys.RightClosingBracket && IsReadingCollection)
                {
                    // skip this Byte
                    this.BufferReader.SkipByte();

                    // Skip next white spaces
                    SkipWhitespaceInBufferReader();

                    // Exit current scope
                    ExitJsonScope();
                }
                if (this.BufferReader.EndOfFile)
                {
                    if (scopeDepth > 0)
                    {
                        MoveToEndElement();
                        return true;
                    }

                    this.MoveToEndOfFile();
                    return false;
                }
            }

            // Get next character
            ch = this.BufferReader.GetByte();

            // if scopeDepth == 0 so we are at the beginning
            // The element is not existent for the moment, so read it as non existent
            if (scopeDepth == 0)
            {
                ReadNonExistentElementName(StringHandleConstStringType.Root);
            }
            else if (IsReadingComplexText)
            {
                switch (complexTextMode)
                {
                    case JsonComplexTextMode.QuotedText:
                        if (ch == Keys.BackSlash)
                        {
                            ReadEscapedCharacter(true);
                            break;
                        }

                        ReadQuotedText(true);
                        break;
                    case JsonComplexTextMode.NumericalText:
                        ReadNumericalText();
                        break;
                    case JsonComplexTextMode.None:
                        throw new XmlException("JsonEncounteredUnexpectedCharacter");
                }
            }
            else if (IsReadingCollection)
                ReadNonExistentElementName(StringHandleConstStringType.Item);

            else switch (ch)
                {
                    case Keys.RightClosingBracket:
                        this.BufferReader.SkipByte();
                        MoveToEndElement();
                        ExitJsonScope();
                        break;
                    case Keys.LeftOpeningBrace:
                        this.BufferReader.SkipByte();
                        SkipWhitespaceInBufferReader();
                        ch = this.BufferReader.GetByte();
                        if (ch == Keys.RightClosingBrace)
                        {
                            // Skip this byte
                            this.BufferReader.SkipByte();
                            // Skip white spaces
                            SkipWhitespaceInBufferReader();

                            // try get next char
                            if (TryGetByte(out ch))
                            {
                                if (ch == Keys.Comma)
                                    this.BufferReader.SkipByte();
                            }
                            else
                            {
                                // We are at end of buffer, just mark Comma to be skipped next time
                                charactersToSkipOnNextRead[0] = Keys.Comma;
                            }

                            // We set to End Element
                            MoveToEndElement();
                        }
                        else
                        {
                            // Enter a new scope
                            EnterJsonScope(JsonNodeType.Object);
                            // Parse start element
                            ParseStartElement();
                        }
                        break;
                    case Keys.RightClosingBrace:
                        this.BufferReader.SkipByte();
                        if (expectingFirstElementInNonPrimitiveChild)
                        {
                            SkipWhitespaceInBufferReader();
                            ch = this.BufferReader.GetByte();
                            switch (ch)
                            {
                                case Keys.Comma:
                                case Keys.RightClosingBrace:
                                    this.BufferReader.SkipByte();
                                    break;
                                default:
                                    throw new XmlException("JsonEncounteredUnexpectedCharacter");
                            }
                            expectingFirstElementInNonPrimitiveChild = false;
                        }
                        MoveToEndElement();
                        break;
                    case Keys.Comma:
                        this.BufferReader.SkipByte();
                        MoveToEndElement();
                        break;
                    case Keys.DoubleQuote:
                        if (this.Node.NodeType == XmlNodeType.Element)
                        {
                            if (expectingFirstElementInNonPrimitiveChild)
                            {
                                EnterJsonScope(JsonNodeType.Object);
                                ParseStartElement();
                            }
                            else
                            {
                                this.BufferReader.SkipByte();
                                ReadQuotedText(true);
                            }
                        }
                        else if (this.Node.NodeType == XmlNodeType.EndElement)
                        {
                            EnterJsonScope(JsonNodeType.Element);
                            ParseStartElement();
                        }
                        else
                            throw new XmlException("JsonEncounteredUnexpectedCharacter");
                        break;
                    case Keys.LowerF:
                        {
                            int offset;
                            byte[] buffer = this.BufferReader.GetBuffer(5, out offset);

                            // Check if it's "false"
                            if (buffer[offset + 1] != Keys.LowerA
                                || buffer[offset + 2] != Keys.LowerL
                                || buffer[offset + 3] != Keys.LowerS
                                || buffer[offset + 4] != Keys.LowerE)
                                throw new Exception("Expected False");

                            // Advance of 5 byte
                            this.BufferReader.Advance(5);

                            // Check if char after is a closing one
                            if (TryGetByte(out ch) && !IsWhitespace(ch) && (ch != Keys.Comma && ch != Keys.RightClosingBrace) && ch != Keys.RightClosingBracket)
                                throw new Exception("TokenExpected");

                            // Move
                            this.MoveToAtomicText().Value.SetValue(ValueHandleType.UTF8, offset, 5);
                        }
                        break;
                    case Keys.LowerT:
                        {
                            int offset;

                            // check if it's "true"
                            byte[] buffer = this.BufferReader.GetBuffer(4, out offset);
                            if (buffer[offset + 1] != Keys.LowerR
                                || buffer[offset + 2] != Keys.LowerU
                                || buffer[offset + 3] != Keys.LowerE)
                                throw new Exception("expected true");

                            this.BufferReader.Advance(4);

                            // Check if char after is a closing one
                            if (TryGetByte(out ch) && !IsWhitespace(ch) && (ch != Keys.Comma && ch != Keys.RightClosingBrace) && ch != Keys.RightClosingBracket)
                                throw new Exception("TokenExpected");

                            // move atomic
                            this.MoveToAtomicText().Value.SetValue(ValueHandleType.UTF8, offset, 4);
                        }
                        break;
                    case Keys.LowerN:
                        {
                            int offset;
                            // check if it's "null"
                            byte[] buffer = this.BufferReader.GetBuffer(4, out offset);

                            if (buffer[offset + 1] != Keys.LowerU
                                || buffer[offset + 2] != Keys.LowerL
                                || buffer[offset + 3] != Keys.LowerL)
                                throw new Exception("Expected null");

                            // advance of 4 chars
                            this.BufferReader.Advance(4);

                            SkipWhitespaceInBufferReader();

                            if (TryGetByte(out ch))
                            {
                                if (ch == Keys.Comma || ch == Keys.RightClosingBrace)
                                    this.BufferReader.SkipByte();
                                else if (ch != Keys.RightClosingBracket)
                                    throw new Exception("TokenExpected");
                            }
                            else
                            {
                                charactersToSkipOnNextRead[0] = Keys.Comma;
                                charactersToSkipOnNextRead[1] = Keys.RightClosingBrace;
                            }
                            MoveToEndElement();
                        }
                        break;
                    default:
                        if ((ch == (byte)'-')
                            || (((byte)'0' <= ch) && (ch <= (byte)'9'))
                            || (ch == (byte)'I')
                            || (ch == (byte)'N'))
                            ReadNumericalText();
                        else
                            throw new XmlException("JsonEncounteredUnexpectedCharacter");
                        break;
                }

            return true;
        }



        private void ReadAttributes()
        {
            XmlAttributeNode xmlAttributeNode = this.AddAttribute();
            xmlAttributeNode.LocalName.SetConstantValue(StringHandleConstStringType.Type);

            // Skip White spaces
            SkipWhitespaceInBufferReader();

            byte nextByte = this.BufferReader.GetByte();

            switch (nextByte)
            {
                case Keys.DoubleQuote:
                    if (!expectingFirstElementInNonPrimitiveChild)
                        xmlAttributeNode.Value.SetConstantValue(ValueHandleConstStringType.String);
                    else
                        xmlAttributeNode.Value.SetConstantValue(ValueHandleConstStringType.Object);
                    break;
                case (byte)'n':
                    xmlAttributeNode.Value.SetConstantValue(ValueHandleConstStringType.Null);
                    break;
                case (byte)'t':
                case (byte)'f':
                    xmlAttributeNode.Value.SetConstantValue(ValueHandleConstStringType.Boolean);
                    break;
                case Keys.LeftOpeningBrace:
                    xmlAttributeNode.Value.SetConstantValue(ValueHandleConstStringType.Object);
                    break;
                case Keys.RightClosingBrace:
                    if (expectingFirstElementInNonPrimitiveChild)
                        xmlAttributeNode.Value.SetConstantValue(ValueHandleConstStringType.Object);
                    else
                        throw new XmlException("JsonEncounteredUnexpectedCharacter");
                    break;
                case Keys.LeftOpeningBracket:
                    // We enter a collection so set that value of this attribute is an array
                    xmlAttributeNode.Value.SetConstantValue(ValueHandleConstStringType.Array);
                    // Skip [ byte
                    BufferReader.SkipByte();
                    // Enter a new scope
                    EnterJsonScope(JsonNodeType.Collection);
                    // no other attributes to analyze, so return
                    break;
                default:
                    if (nextByte == '-' ||
                        (nextByte <= '9' && nextByte >= '0') ||
                        nextByte == 'N' ||
                        nextByte == 'I')
                    {
                        xmlAttributeNode.Value.SetConstantValue(ValueHandleConstStringType.Number);
                    }
                    else
                    {
                        throw new XmlException("JsonEncounteredUnexpectedCharacter");
                    }
                    break;
            }
        }

        /// <summary>
        /// Read Escaped Character and set this.complexTextMode = JsonComplexTextMode.Non or QuotedText
        /// </summary>
        /// <param name="moveToText"></param>
        private void ReadEscapedCharacter(bool moveToText)
        {
            // Skip current byte
            this.BufferReader.SkipByte();

            // Get Byte
            var ch1 = (char)this.BufferReader.GetByte();

            //The Unicode character with encoding xxxx, where xxxx is one to four hexidecimal digits. 
            //Unicode escapes are distinct from the other escape types listed here; they are described below in more detail.
            if (ch1 == Keys.LowerU)
            {
                // Skip the lower u
                this.BufferReader.SkipByte();

                int offset;
                // Get next 5 char

                byte[] buffer = this.BufferReader.GetBuffer(5, out offset);

                string string1 = Encoding.UTF8.GetString(buffer, offset, 4);
                this.BufferReader.Advance(4);

                int ch2 = ParseChar(string1, NumberStyles.HexNumber);

                if (char.IsHighSurrogate((char)ch2) && this.BufferReader.GetByte() == Keys.BackSlash)
                {
                    this.BufferReader.SkipByte();

                    // Skip the lower U
                    this.SkipExpectedByteInBufferReader(Keys.LowerU);

                    buffer = this.BufferReader.GetBuffer(5, out offset);

                    string string2 = Encoding.UTF8.GetString(buffer, offset, 4);

                    this.BufferReader.Advance(4);

                    char ch3 = ParseChar(string2, NumberStyles.HexNumber);

                    if (!char.IsLowSurrogate(ch3))
                        throw new XmlException("XmlInvalidLowSurrogate");

                    // ch2 = new SurrogateChar(ch3, (char)ch2).Char;
                }
                if (buffer[offset + 4] == Keys.DoubleQuote)
                {
                    this.BufferReader.SkipByte();
                    if (moveToText)
                        this.MoveToAtomicText().Value.SetCharValue(ch2);
                    this.complexTextMode = JsonComplexTextMode.None;
                }
                else
                {
                    if (moveToText)
                        this.MoveToComplexText().Value.SetCharValue(ch2);
                    this.complexTextMode = JsonComplexTextMode.QuotedText;
                }

            }
            else
            {
                switch (ch1)
                {
                    case 'n':
                        ch1 = '\n';
                        break;
                    case 'r':
                        ch1 = '\r';
                        break;
                    case 't':
                        ch1 = '\t';
                        break;
                    case 'b':
                        ch1 = '\b';
                        break;
                    case 'f':
                        break;
                    case '"':
                    case '/':
                    case '\\':
                        break;
                    default:
                        throw new XmlException("JsonEncounteredUnexpectedCharacter");
                }

                // skip current byte
                this.BufferReader.SkipByte();

                if (this.BufferReader.GetByte() == Keys.DoubleQuote)
                {
                    this.BufferReader.SkipByte();
                    if (moveToText)
                        this.MoveToAtomicText().Value.SetCharValue(ch1);

                    complexTextMode = JsonComplexTextMode.None;
                }
                else
                {
                    if (moveToText)
                        this.MoveToComplexText().Value.SetCharValue(ch1);

                    complexTextMode = JsonComplexTextMode.QuotedText;
                }
            }
        }

        /// <summary>
        /// Read a non existent element (non existent for the moment ?)
        /// </summary>
        /// <param name="elementName"></param>
        private void ReadNonExistentElementName(StringHandleConstStringType elementName)
        {
            // Enter a scope of object
            EnterJsonScope(JsonNodeType.Object);

            // Create an element Node
            XmlElementNode xmlElementNode = this.EnterScope();
            xmlElementNode.LocalName.SetConstantValue(elementName);
            xmlElementNode.BufferOffset = this.BufferReader.Offset;
            xmlElementNode.IsEmptyElement = false;
            xmlElementNode.ExitScope = false;

            // Read attributes
            ReadAttributes();
        }

        private int ReadNonFFFE()
        {
            int offset;
            byte[] buffer = this.BufferReader.GetBuffer(3, out offset);
            if (buffer[offset + 1] == 191 && (buffer[offset + 2] == 190 || buffer[offset + 2] == 191))
                throw new XmlException("JsonInvalidFFFE");

            return 3;
        }

        private void ReadNumericalText()
        {
            int offset;
            int offsetMax;
            int num;

            byte[] buffer = this.BufferReader.GetBuffer(MaxTextChunk, out offset, out offsetMax);
            int numericalTextLength = ComputeNumericalTextLength(buffer, offset, offsetMax);
            num = BreakText(buffer, offset, numericalTextLength);

            this.BufferReader.Advance(num);
            if (offset <= offsetMax - num)
            {
                this.MoveToAtomicText().Value.SetValue(ValueHandleType.UTF8, offset, num);
                complexTextMode = JsonComplexTextMode.None;
            }
            else
            {
                this.MoveToComplexText().Value.SetValue(ValueHandleType.UTF8, offset, num);
                complexTextMode = JsonComplexTextMode.NumericalText;
            }
        }

        /// <summary>
        /// Read Quoted Text
        /// </summary>
        /// <param name="moveToText"></param>
        private void ReadQuotedText(bool moveToText)
        {
            int offset;
            int offsetMax;
            bool escaped;
            int num;

            byte[] buffer = this.BufferReader.GetBuffer(MaxTextChunk, out offset, out offsetMax);

            int lengthUntilEndQuote = ComputeQuotedTextLengthUntilEndQuote(buffer, offset, offsetMax, out escaped);

            num = BreakText(buffer, offset, lengthUntilEndQuote);

            if (escaped && this.BufferReader.GetByte() == 239)
            {
                offset = this.BufferReader.Offset;
                num = ReadNonFFFE();
            }

            this.BufferReader.Advance(num);

            // If there is no escaped char && offset is not at the end
            if (!escaped && offset < offsetMax - num)
            {
                if (moveToText)
                    this.MoveToAtomicText().Value.SetValue(ValueHandleType.UTF8, offset, num);

                SkipExpectedByteInBufferReader(Keys.DoubleQuote);

                complexTextMode = JsonComplexTextMode.None;
            }
            else if (num == 0 && escaped)
            {
                ReadEscapedCharacter(moveToText);
            }
            else
            {
                if (moveToText)
                    this.MoveToComplexText().Value.SetValue(ValueHandleType.UTF8, offset, num);
                complexTextMode = JsonComplexTextMode.QuotedText;
            }
        }



        /// <summary>
        /// We skip a byte which is expected
        /// if the byte isn't what we expected, we throw an exception because it's not correct
        /// </summary>
        private void SkipExpectedByteInBufferReader(byte characterToSkip)
        {
            if (this.BufferReader.GetByte() != characterToSkip)
                throw new Exception("TokenExpected");

            this.BufferReader.SkipByte();
        }

        /// <summary>
        /// Skip whitee space in the buffer reader
        /// </summary>
        private void SkipWhitespaceInBufferReader()
        {
            byte ch;
            while (TryGetByte(out ch) && IsWhitespace(ch))
                this.BufferReader.SkipByte();
        }

        private bool TryGetByte(out byte ch)
        {
            int offset;
            int offsetMax;
            byte[] buffer = this.BufferReader.GetBuffer(1, out offset, out offsetMax);
            if (offset < offsetMax)
            {
                ch = buffer[offset];
                return true;
            }
            ch = 0;
            return false;
        }

        /// <summary>
        /// Unescape Json String
        /// </summary>
        private string UnescapeJsonString(string val)
        {
            if (val == null)
                return null;

            StringBuilder stringBuilder = new StringBuilder();
            int startIndex = 0;
            int count = 0;
            for (int index = 0; index < val.Length; ++index)
            {
                // If we have a back slash 
                if (val[index] == Keys.BackSlash)
                {
                    // add all the chars before
                    stringBuilder.Append(val, startIndex, count);

                    // pass the back slash
                    ++index;

                    if (index >= val.Length)
                        throw new XmlException("JsonEncounteredUnexpectedCharacter");

                    switch (val[index])
                    {
                        case 'n':
                            stringBuilder.Append('\n');
                            break;
                        case 'r':
                            stringBuilder.Append('\r');
                            break;
                        case 't':
                            stringBuilder.Append('\t');
                            break;
                        case 'u':
                            if (index + 3 >= val.Length)
                                throw new XmlException("JsonEncounteredUnexpectedCharacter");

                            stringBuilder.Append(ParseChar(val.Substring(index + 1, 4), NumberStyles.HexNumber));
                            index += 4;
                            break;
                        case 'b':
                            stringBuilder.Append('\b');
                            break;
                        case 'f':
                            stringBuilder.Append('\f');
                            break;
                        case '/':
                        case '\\':
                        case '"':
                        case '\'':
                            stringBuilder.Append(val[index]);
                            break;
                    }
                    startIndex = index + 1;
                    count = 0;
                }
                else
                    ++count;
            }
            if (stringBuilder.Length == 0)
                return val;

            if (count > 0)
                stringBuilder.Append(val, startIndex, count);

            return (stringBuilder).ToString();
        }


        public override bool ReadAttributeValue()
        {
            XmlAttributeTextNode attributeText = this.Node.AttributeText;

            if (attributeText == null)
                return false;

            this.MoveToNode(attributeText);
            return true;
        }

        private void SkipValue(XmlNode xmlNode)
        {
            if (!xmlNode.SkipValue)
                return;
            this.Read();
        }


        public override int ReadElementContentAsBase64(byte[] buffer, int offset, int count)
        {
            if (!this.readingElement)
            {
                if (this.IsEmptyElement)
                {
                    this.Read();
                    return 0;
                }

                this.ReadStartElement();

                this.readingElement = true;
            }
            int num = this.ReadContentAsBase64(buffer, offset, count);

            if (num == 0)
            {
                this.ReadEndElement();
                this.readingElement = false;
            }
            return num;
        }

        public override int ReadContentAsBase64(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("ReadContentAsBase64(byte[] buffer, int offset, int count)");

        }

        public override int ReadElementContentAsBinHex(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("ReadElementContentAsBinHex(byte[] buffer, int offset, int count)");
        }

        public override int ReadContentAsBinHex(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("ReadContentAsBinHex(byte[] buffer, int offset, int count)");

        }

        public override string ReadContentAsString()
        {
            XmlNode node = this.Node;

            if (!node.IsAtomicValue)
                return base.ReadContentAsString();

            string str;

            if (this.value != null)
            {
                str = this.value;
                if (node.AttributeText == null)
                    this.value = string.Empty;
            }
            else
            {
                str = node.Value.GetString();
                this.SkipValue(node);
                if (str.Length > this.quotas.MaxStringContentLength)
                    throw new XmlException("MaxStringContentLengthExceeded");
            }
            return str;
        }

        public override bool ReadContentAsBoolean()
        {
            XmlNode node = this.Node;
            if (this.value != null || !node.IsAtomicValue)
                return XmlConverter.ToBoolean(this.ReadContentAsString());
            bool flag = node.Value.ToBoolean();
            this.SkipValue(node);
            return flag;
        }

        public override long ReadContentAsLong()
        {
            string s = this.ReadContentAsString();
            return long.Parse(s, NumberStyles.Float, NumberFormatInfo.InvariantInfo);

        }

        public override int ReadContentAsInt()
        {
            return ParseInt(this.ReadContentAsString(), NumberStyles.Float);
        }

        public DateTime ReadContentAsDateTime()
        {
            XmlNode node = this.Node;

            if (this.value != null || !node.IsAtomicValue)
                return XmlConverter.ToDateTime(this.ReadContentAsString());

            DateTime dateTime = node.Value.ToDateTime();
            this.SkipValue(node);
            return dateTime;
        }

        public override double ReadContentAsDouble()
        {
            XmlNode node = this.Node;

            if (this.value != null || !node.IsAtomicValue)
                return XmlConverter.ToDouble(this.ReadContentAsString());

            double num = node.Value.ToDouble();
            this.SkipValue(node);
            return num;
        }

        public override float ReadContentAsFloat()
        {
            XmlNode node = this.Node;
            if (this.value != null || !node.IsAtomicValue)
                return XmlConverter.ToSingle(this.ReadContentAsString());

            float num = node.Value.ToSingle();

            this.SkipValue(node);
            return num;
        }

        public override Decimal ReadContentAsDecimal()
        {
            string s = this.ReadContentAsString();
            return Decimal.Parse(s, NumberStyles.Float, NumberFormatInfo.InvariantInfo);

        }

        public UniqueId ReadContentAsUniqueId()
        {
            XmlNode node = this.Node;

            if (this.value != null || !node.IsAtomicValue)
                return XmlConverter.ToUniqueId(this.ReadContentAsString());

            UniqueId uniqueId = node.Value.ToUniqueId();
            this.SkipValue(node);
            return uniqueId;
        }

        public TimeSpan ReadContentAsTimeSpan()
        {
            XmlNode node = this.Node;
            if (this.value != null || !node.IsAtomicValue)
                return XmlConverter.ToTimeSpan(this.ReadContentAsString());

            TimeSpan timeSpan = node.Value.ToTimeSpan();

            this.SkipValue(node);
            return timeSpan;
        }

        public Guid ReadContentAsGuid()
        {
            XmlNode node = this.Node;

            if (this.value != null || !node.IsAtomicValue)
                return XmlConverter.ToGuid(this.ReadContentAsString());

            Guid guid = node.Value.ToGuid();
            this.SkipValue(node);
            return guid;
        }

        public override object ReadContentAsObject()
        {
            XmlNode node = this.Node;
            if (this.value != null || !node.IsAtomicValue)
                return this.ReadContentAsString();

            object obj = node.Value.ToObject();
            this.SkipValue(node);
            return obj;
        }

        public override object ReadContentAs(Type type, IXmlNamespaceResolver namespaceResolver)
        {
            if (type == typeof(ulong))
            {
                if (this.value != null || !this.Node.IsAtomicValue)
                    return XmlConverter.ToUInt64(this.ReadContentAsString());

                ulong num = this.Node.Value.ToULong();
                this.SkipValue(this.Node);
                return num;
            }
            if (type == typeof(bool))
                return this.ReadContentAsBoolean();
            if (type == typeof(int))
                return this.ReadContentAsInt();
            if (type == typeof(long))
                return this.ReadContentAsLong();
            if (type == typeof(float))
                return this.ReadContentAsFloat();
            if (type == typeof(double))
                return this.ReadContentAsDouble();
            if (type == typeof(Decimal))
                return this.ReadContentAsDecimal();
            if (type == typeof(DateTime))
                return this.ReadContentAsDateTime();
            if (type == typeof(UniqueId))
                return this.ReadContentAsUniqueId();
            if (type == typeof(Guid))
                return this.ReadContentAsGuid();
            if (type == typeof(TimeSpan))
                return this.ReadContentAsTimeSpan();
            if (type == typeof(object))
                return this.ReadContentAsObject();
            if (type == typeof(String))
                return this.ReadContentAsString();

            return base.ReadContentAs(type, namespaceResolver);
        }

        public override void ResolveEntity()
        {
            throw new InvalidOperationException("XmlInvalidOperation");
        }

        public override void Skip()
        {
            if (this.Node.ReadState != ReadState.Interactive)
                return;
            if ((this.Node.NodeType == XmlNodeType.Element || this.MoveToElement()) && !this.IsEmptyElement)
            {
                int currentDepth = this.Depth;
                do
                {
                } while (this.Read() && currentDepth < this.Depth);

                if (this.Node.NodeType != XmlNodeType.EndElement)
                    return;
                this.Read();
            }
            else
                this.Read();
        }




    }
}
