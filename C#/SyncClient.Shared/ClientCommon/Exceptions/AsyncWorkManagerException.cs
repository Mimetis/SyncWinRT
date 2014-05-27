using System;

namespace Microsoft.Synchronization.ClientServices
{
    /// <summary>
    /// Exception thrown for invalid operations in the AsyncWorkerManager.
    /// </summary>
    class AsyncWorkManagerException : Exception
    {
        public AsyncWorkManagerException(string message)
            : base(message)
        { }

        public AsyncWorkManagerException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
