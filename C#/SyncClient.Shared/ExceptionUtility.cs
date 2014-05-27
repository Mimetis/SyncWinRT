using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Reflection;


namespace Microsoft.Synchronization.ClientServices
{
    
    static class ExceptionUtility
    {

        internal static bool IsFatal(Exception exception)
        {
            while (exception != null)
            {
                if (exception is OutOfMemoryException || exception is SEHException)
                {
                    return true;
                }
                exception = exception.InnerException;
            }
            return false;
        }
    }
}
