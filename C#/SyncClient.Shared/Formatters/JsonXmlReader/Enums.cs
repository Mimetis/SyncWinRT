using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Synchronization.ClientServices
{

    public enum JsonNodeType
    {
        None,
        Object,
        Element,
        EndElement,
        QuotedText,
        StandaloneText,
        Collection,
    }

    public enum JsonComplexTextMode
    {
        QuotedText,
        NumericalText,
        None,
    }


    public enum StringHandleConstStringType
    {
        Type,
        Root,
        Item,
    }

    public class Keys
    {


        /// <summary>
        /// HorizontalTab == 9
        /// </summary>
        internal const byte HorizontalTab = 9;

        /// <summary>
        /// Line Feed == 10
        /// </summary>
        internal const byte LineFeed = 10;

        /// <summary>
        /// Carriage Return == 13
        /// </summary>
        internal const byte CarriageReturn = 13;

        /// <summary>
        /// Space == 32
        /// </summary>
        internal const byte Space = 32;

        /// <summary>
        /// Double Quote == 34
        /// </summary>
        internal const byte DoubleQuote = 34;

        /// <summary>
        /// # == 35
        /// </summary>
        internal const byte Diese = 35;

        /// <summary>
        /// & == 38
        /// </summary>
        internal const byte Ampersand = 38;

        /// <summary>
        /// Single Quote == 39
        /// </summary>
        internal const byte SingleQuote = 39;

        /// <summary>
        /// Comma , == 44
        /// </summary>
        internal const byte Comma = 44;

        /// <summary>
        /// Minus == 45
        /// </summary>
        internal const byte Minus = 45;

        /// <summary>
        /// / == 47
        /// </summary>
        internal const byte SlashForward = 47;

        /// <summary>
        /// 0 == 48
        /// </summary>
        internal const byte Zero = 48;

        /// <summary>
        /// 1 == 49
        /// </summary>
        internal const byte One = 49;

        /// <summary>
        /// 9 == 57
        /// </summary>
        internal const byte Nine = 57;

        /// <summary>
        /// Colon : == 58
        /// </summary>
        internal const byte Colon = 58;

        /// <summary>
        /// ; == 59
        /// </summary>
        internal const byte SemiColon = 59;

        /// <summary>
        /// Inferior == 60
        /// </summary>
        internal const byte Inferior = 60;

        /// <summary>
        /// = == 61
        /// </summary>
        internal const byte Equality = 61;

        /// <summary>
        /// > == 62
        /// </summary>
        internal const byte Superior = 62;

        /// <summary>
        /// I == 73
        /// </summary>
        internal const byte UpperI = 73;

        /// <summary>
        /// N == 78
        /// </summary>
        internal const byte UpperN = 78;

        /// <summary>
        /// Left Opening Bracket [ == 91
        /// </summary>
        internal const byte LeftOpeningBracket = 91;

        /// <summary>
        /// Back Slash == 92
        /// </summary>
        internal const byte BackSlash = 92;

        /// <summary>
        /// Right Closing Bracket ] == 93
        /// </summary>
        internal const byte RightClosingBracket = 93;

        /// <summary>
        /// _ == 95
        /// </summary>
        internal const byte Underscore = 95;

        /// <summary>
        /// a == 97
        /// </summary>
        internal const byte LowerA = 97;

        /// <summary>
        /// e == 101
        /// </summary>
        internal const byte LowerE = 101;

        /// <summary>
        /// f == 102
        /// </summary>
        internal const byte LowerF = 102;
        /// <summary>
        /// g == 103
        /// </summary>
        internal const byte LowerG = 103;

        /// <summary>
        /// l == 108
        /// </summary>
        internal const byte LowerL = 108;
 
        /// <summary>
        /// m == 109
        /// </summary>
        internal const byte LowerM = 109;
        
        /// <summary>
        /// n == 110
        /// </summary>
        internal const byte LowerN = 110;

        /// <summary>
        /// p == 112
        /// </summary>
        internal const byte LowerP = 112;

        /// <summary>
        /// q == 113
        /// </summary>
        internal const byte LowerQ = 113;

        /// <summary>
        /// r == 114
        /// </summary>
        internal const byte LowerR = 114;

        /// <summary>
        /// s == 115
        /// </summary>
        internal const byte LowerS = 115;

        /// <summary>
        /// t == 116
        /// </summary>
        internal const byte LowerT = 116;

        /// <summary>
        /// u == 117
        /// </summary>
        internal const byte LowerU = 117;

        /// <summary>
        /// x == 120
        /// </summary>
        internal const byte LowerX = 120;

        /// <summary>
        /// y == 121
        /// </summary>
        internal const byte LowerY = 121;

        /// <summary>
        /// z == 122
        /// </summary>
        internal const byte LowerZ = 122;


        /// <summary>
        ///  { == 123
        /// </summary>
        internal const byte LeftOpeningBrace = 123;

        /// <summary>
        ///  } == 125
        /// </summary>
        internal const byte RightClosingBrace = 125;


        internal const byte Unknown = 239;
    }
}
