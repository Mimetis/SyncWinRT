using System.Runtime.Serialization;
namespace Microsoft.Synchronization.ClientServices.Common
{
    /// <summary>
    /// Represents a Conflict base type. 
    /// </summary>
    [DataContract()]
    public class Conflict
    {
        /// <summary>
        /// Represents the current live version that is stored on the server. The version when applied on
        /// the client will ensure data convergence between server and client for this particular
        /// entity.
        /// </summary>
        [DataMember]
        public IOfflineEntity LiveEntity { get; internal set; }
    }
}
