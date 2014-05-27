using System;
using System.Net;
using System.Windows;

namespace Microsoft.Synchronization.ClientServices.IsolatedStorage
{
    /// <summary>
    /// Event args passed to the LoadCompletedEventHandler when LoadAsync completes
    /// </summary>
    public class LoadCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor which takes the exception that occurred when loading.  The exception
        /// can be null if no exception occurred
        /// </summary>
        /// <param name="e">Exception which occurred during loading (can be null).</param>
        public LoadCompletedEventArgs(Exception e)
        {
            this._exception = e;
        }

        /// <summary>
        /// The exception that occurred during execution of LoadAsync.
        /// </summary>
        public Exception Exception
        {
            get
            {
                return _exception;
            }
        }

        Exception _exception;
    }
}
