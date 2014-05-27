using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Synchronization.ClientServices
{
    /// <summary>
    /// Represents the serialization format between the client and the server
    /// </summary>
    public enum SerializationFormat
    {
        /// <summary>
        /// ATOM Format
        /// </summary>
        ODataAtom,

        /// <summary>
        /// JSON Format
        /// </summary>
        ODataJSON
    }
}
