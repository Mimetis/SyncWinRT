using System;
using System.Xml;

namespace Microsoft.Synchronization.Services.Formatters
{
    static class JsonHelper
    {
        public static bool IsElement(XmlReader reader, string elementName)
        {
            return reader.Name.Equals(elementName, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
