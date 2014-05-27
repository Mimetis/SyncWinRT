using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Synchronization.ClientServices.Common;


namespace Microsoft.Synchronization.ClientServices
{
    /// <summary>
    /// OfflineSyncProvider
    /// </summary>
    public abstract class OfflineSyncProvider
    {
        /// <summary>
        /// Begin Session
        /// </summary>
        public abstract Task BeginSession();

        /// <summary>
        /// GetChangeSet, called on the Source to get the changes
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public abstract Task<ChangeSet> GetChangeSet(Guid state);

        /// <summary>
        /// OnChangeSetUploaded, fired when changeset is uploaded
        /// </summary>
        /// <param name="state"></param>
        /// <param name="response"></param>
        public abstract Task OnChangeSetUploaded(Guid state, ChangeSetResponse response);

        /// <summary>
        /// Gets the server blob
        /// </summary>
        /// <returns></returns>
        public abstract byte[] GetServerBlob();

        /// <summary>
        /// SaveChangeSet, called on the destination to save the changes on the local storage
        /// </summary>
        /// <param name="changeSet"></param>
        public abstract Task SaveChangeSet(ChangeSet changeSet);

        /// <summary>
        /// End Session
        /// </summary>
        public abstract void EndSession();

       

    }
}
