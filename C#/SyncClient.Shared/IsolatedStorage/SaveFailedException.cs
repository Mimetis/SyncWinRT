using System;
using System.Collections.Generic;

namespace Microsoft.Synchronization.ClientServices.IsolatedStorage
{
    /// <summary>
    /// Exception thrown when the SaveChanges call fails
    /// </summary>
    public class SaveFailedException : Exception
    {
        private readonly IEnumerable<OfflineConflict> conflicts;

        /// <summary>
        /// Initializes a new instance of the SaveFailedException class.
        /// </summary>
        /// <param name="conflicts">Store conflicts which are preventing SaveChanges from succeeding</param>
        internal SaveFailedException(IEnumerable<OfflineConflict> conflicts)
        {
            this.conflicts = conflicts;
        }

        /// <summary>
        /// Initializes a new instance of the SaveFailedException class.
        /// </summary>
        /// <param name="conflicts">Store conflicts which are preventing SaveChanges from succeeding</param>
        /// <param name="message">Message that describes the error.</param>
        public SaveFailedException(IEnumerable<OfflineConflict> conflicts, string message)
            : base(message)
        {
            this.conflicts = conflicts;
        }

        /// <summary>
        /// Initializes a new instance of the SaveFailedException class.
        /// </summary>
        /// <param name="conflicts">Store conflicts which are preventing SaveChanges from succeeding</param>
        /// <param name="message">Message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference
        ///  (Nothing in Visual Basic) if no inner exception is specified. </param>
        public SaveFailedException(
            IEnumerable<OfflineConflict> conflicts,
            string message,
            Exception innerException)
            : base(message, innerException)
        {
            this.conflicts = conflicts;
        }

        /// <summary>
        /// The list of StoreConflicts that occurred when SaveChanges was attempted.
        /// </summary>
        public IEnumerable<OfflineConflict> Conflicts
        {
            get { return conflicts; }
        }
    }
}