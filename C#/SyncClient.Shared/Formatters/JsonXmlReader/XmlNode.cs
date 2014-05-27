using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.Synchronization.ClientServices
{

    public class XmlNode
    {
        private XmlNodeType nodeType;
        //private PrefixHandle prefix;
        private StringHandle localName;
        private ValueHandle value;
        //private Namespace ns;
        private bool hasValue;
        private bool canGetAttribute;
        private bool canMoveToElement;
        private ReadState readState;
        private XmlAttributeTextNode attributeTextNode;
        private bool exitScope;
        private int depthDelta;
        private bool skipValue;
        private bool hasContent;
        private bool isEmptyElement;
        private char quoteChar;

        public bool HasValue
        {
            get
            {
                return this.hasValue;
            }
        }

        public ReadState ReadState
        {
            get
            {
                return this.readState;
            }
        }

        public StringHandle LocalName
        {
            get
            {
                return this.localName;
            }
        }

        //public PrefixHandle Prefix
        //{
        //    get
        //    {
        //        return this.prefix;
        //    }
        //}

        public bool CanGetAttribute
        {
            get
            {
                return this.canGetAttribute;
            }
        }

        public bool CanMoveToElement
        {
            get
            {
                return this.canMoveToElement;
            }
        }

        public XmlAttributeTextNode AttributeText
        {
            get
            {
                return this.attributeTextNode;
            }
        }

        public bool SkipValue
        {
            get
            {
                return this.skipValue;
            }
        }

        public ValueHandle Value
        {
            get
            {
                return this.value;
            }
        }

        public int DepthDelta
        {
            get
            {
                return this.depthDelta;
            }
        }

        public bool HasContent
        {
            get
            {
                return this.hasContent;
            }
        }

        public XmlNodeType NodeType
        {
            get
            {
                return this.nodeType;
            }
            set
            {
                this.nodeType = value;
            }
        }

        //public Namespace Namespace
        //{
        //    get
        //    {
        //        return this.ns;
        //    }
        //    set
        //    {
        //        this.ns = value;
        //    }
        //}

        private bool isAtomicValue;
        public bool IsAtomicValue
        {
            get { return isAtomicValue; }
            set
            {
                isAtomicValue = value;
            }
        }
        public bool ExitScope
        {
            get
            {
                return this.exitScope;
            }
            set
            {
                this.exitScope = value;
            }
        }

        public bool IsEmptyElement
        {
            get
            {
                return this.isEmptyElement;
            }
            set
            {
                this.isEmptyElement = value;
            }
        }

        public char QuoteChar
        {
            get
            {
                return this.quoteChar;
            }
            set
            {
                this.quoteChar = value;
            }
        }

        public string ValueAsString
        {
            get
            {
                return this.Value.GetString();
            }
        }

       

        protected XmlNode(XmlNodeType nodeType, StringHandle localName, ValueHandle value,
                            XmlNodeFlags nodeFlags, ReadState readState, XmlAttributeTextNode attributeTextNode, int depthDelta)
        {
            this.nodeType = nodeType;
            this.localName = localName;
            this.value = value;
            //this.ns = NamespaceManager.EmptyNamespace;
            this.hasValue = (nodeFlags & XmlNodeFlags.HasValue) != XmlNodeFlags.None;
            this.canGetAttribute = (nodeFlags & XmlNodeFlags.CanGetAttribute) != XmlNodeFlags.None;
            this.canMoveToElement = (nodeFlags & XmlNodeFlags.CanMoveToElement) != XmlNodeFlags.None;
            this.IsAtomicValue = (nodeFlags & XmlNodeFlags.AtomicValue) != XmlNodeFlags.None;
            this.skipValue = (nodeFlags & XmlNodeFlags.SkipValue) != XmlNodeFlags.None;
            this.hasContent = (nodeFlags & XmlNodeFlags.HasContent) != XmlNodeFlags.None;
            this.readState = readState;
            this.attributeTextNode = attributeTextNode;
            this.exitScope = nodeType == XmlNodeType.EndElement;
            this.depthDelta = depthDelta;
            this.isEmptyElement = false;
            this.quoteChar = '"';
        }

        public bool IsLocalName(string name)
        {
            return this.LocalName == name;
        }


        public bool IsNamespaceUri(string iNs)
        {
            return false;
            //return this.Namespace.IsUri(iNs);
        }


        public bool IsLocalNameAndNamespaceUri(string name, string iNs)
        {
            //if (this.LocalName == name)
            //    return this.Namespace.IsUri(iNs);
            return false;
        }


        public bool IsPrefixAndLocalName(string prefix, string slocalName)
        {
            //if (this.Prefix == prefix)
            //    return this.LocalName == slocalName;
            return false;
        }

        [Flags]
        protected enum XmlNodeFlags
        {
            None = 0,
            CanGetAttribute = 1,
            CanMoveToElement = 2,
            HasValue = 4,
            AtomicValue = 8,
            SkipValue = 16,
            HasContent = 32,
        }
    }

    public class XmlElementNode : XmlNode
    {
        private XmlEndElementNode endElementNode;
        private int bufferOffset;
        public int NameOffset;
        public int NameLength;

        public XmlEndElementNode EndElement
        {
            get
            {
                return this.endElementNode;
            }
        }

        public int BufferOffset
        {
            get
            {
                return this.bufferOffset;
            }
            set
            {
                this.bufferOffset = value;
            }
        }

        public XmlElementNode(XmlBufferReader bufferReader)
            : this(new StringHandle(bufferReader), new ValueHandle(bufferReader))
        {
        }

        private XmlElementNode(StringHandle localName, ValueHandle value)
            : base(XmlNodeType.Element, localName, value, (XmlNodeFlags)33, ReadState.Interactive, null, -1)
        {
            this.endElementNode = new XmlEndElementNode(localName, value);
        }
    }

    public class XmlAttributeNode : XmlNode
    {
        public XmlAttributeNode(XmlBufferReader bufferReader)
            : this( new StringHandle(bufferReader), new ValueHandle(bufferReader))
        {
        }

        private XmlAttributeNode(StringHandle localName, ValueHandle value)
            : base(XmlNodeType.Attribute, localName, value, (XmlNodeFlags)15, ReadState.Interactive, new XmlAttributeTextNode(localName, value), 0)
        {
        }
    }

    public class XmlEndElementNode : XmlNode
    {
        public XmlEndElementNode(StringHandle localName, ValueHandle value)
            : base(XmlNodeType.EndElement, localName, value, XmlNodeFlags.HasContent, ReadState.Interactive, null, -1)
        {
        }
    }

    public class XmlTextNode : XmlNode
    {
        protected XmlTextNode(XmlNodeType nodeType, StringHandle localName, ValueHandle value, XmlNodeFlags nodeFlags, ReadState readState, XmlAttributeTextNode attributeTextNode, int depthDelta)
            : base(nodeType, localName, value, nodeFlags, readState, attributeTextNode, depthDelta)
        {
        }
    }

    public class XmlAtomicTextNode : XmlTextNode
    {
        public XmlAtomicTextNode(XmlBufferReader bufferReader)
            : base(XmlNodeType.Text, new StringHandle(bufferReader), new ValueHandle(bufferReader), (XmlNodeFlags)60, ReadState.Interactive, null, 0)
        {
        }
    }

    public class XmlComplexTextNode : XmlTextNode
    {
        public XmlComplexTextNode(XmlBufferReader bufferReader)
            : base(XmlNodeType.Text, new StringHandle(bufferReader), new ValueHandle(bufferReader), (XmlNodeFlags)36, ReadState.Interactive, null, 0)
        {
        }
    }

    public class XmlWhitespaceTextNode : XmlTextNode
    {
        public XmlWhitespaceTextNode(XmlBufferReader bufferReader)
            : base(XmlNodeType.Whitespace, new StringHandle(bufferReader), new ValueHandle(bufferReader), XmlNodeFlags.HasValue, ReadState.Interactive, null, 0)
        {
        }
    }

    public class XmlCDataNode : XmlTextNode
    {
        public XmlCDataNode(XmlBufferReader bufferReader)
            : base(XmlNodeType.CDATA, new StringHandle(bufferReader), new ValueHandle(bufferReader), (XmlNodeFlags)36, ReadState.Interactive, null, 0)
        {
        }
    }

    public class XmlAttributeTextNode : XmlTextNode
    {
        public XmlAttributeTextNode(StringHandle localName, ValueHandle value)
            : base(XmlNodeType.Text, localName, value, (XmlNodeFlags)47, ReadState.Interactive, 
            null, 1)
        {
        }
    }

    public class XmlInitialNode : XmlNode
    {
        public XmlInitialNode(XmlBufferReader bufferReader)
            : base(XmlNodeType.None, new StringHandle(bufferReader), new ValueHandle(bufferReader), XmlNodeFlags.None, ReadState.Initial, null, 0)
        {
        }
    }

    public class XmlDeclarationNode : XmlNode
    {
        public XmlDeclarationNode(XmlBufferReader bufferReader)
            : base(XmlNodeType.XmlDeclaration, new StringHandle(bufferReader), new ValueHandle(bufferReader), XmlNodeFlags.CanGetAttribute, ReadState.Interactive, null, 0)
        {
        }
    }

    public class XmlCommentNode : XmlNode
    {
        public XmlCommentNode(XmlBufferReader bufferReader)
            : base(XmlNodeType.Comment, new StringHandle(bufferReader), new ValueHandle(bufferReader), XmlNodeFlags.HasValue, ReadState.Interactive, null, 0)
        {
        }
    }

    public class XmlEndOfFileNode : XmlNode
    {
        public XmlEndOfFileNode(XmlBufferReader bufferReader)
            : base(XmlNodeType.None, new StringHandle(bufferReader), new ValueHandle(bufferReader), XmlNodeFlags.None, ReadState.EndOfFile, null, 0)
        {
        }
    }

    public class XmlClosedNode : XmlNode
    {
        public XmlClosedNode(XmlBufferReader bufferReader)
            : base(XmlNodeType.None, new StringHandle(bufferReader), new ValueHandle(bufferReader), XmlNodeFlags.None, ReadState.Closed, null, 0)
        {
        }
    }
}
