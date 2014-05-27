using System;

namespace Microsoft.Synchronization.ClientServices.IsolatedStorage
{
    /// <summary>
    /// Exception thrown when SaveChanges is attempted while sync is active.
    /// </summary>
    public class SyncActiveException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the Microsoft.Synchronization.ClientServices.IsolatedStorage.SyncActiveException
        /// class.
        /// </summary>
        public SyncActiveException()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Microsoft.Synchronization.ClientServices.IsolatedStorage.SyncActiveException
        /// class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SyncActiveException(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Microsoft.Synchronization.ClientServices.IsolatedStorage.SyncActiveException
        /// class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference
        /// (Nothing in Visual Basic) if no inner exception is specified.</param>
        public SyncActiveException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
