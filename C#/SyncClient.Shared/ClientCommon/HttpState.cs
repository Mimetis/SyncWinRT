using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Synchronization.ClientServices
{
    public enum HttpState
    {
        Start,
        WriteRequest,
        ReadResponse,
        End
    }
}
