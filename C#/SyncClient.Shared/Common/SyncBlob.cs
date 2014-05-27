using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Synchronization.ClientServices.Common
{
    [DataContract(Namespace = "Microsoft.Synchronization.ClientServices.Common")]
    internal sealed class SyncBlob
    {
        [DataMember]
        internal byte[] ClientKnowledge { get; set; }

        [DataMember]
        internal string ClientScopeName { get; set; }

        [DataMember]
        internal bool IsLastBatch { get; set; }

        [DataMember]
        internal Guid? BatchCode { get; set; }

        /// <summary>
        /// The next batch number.
        /// </summary>
        [DataMember]
        internal Guid? NextBatch { get; set; }

        internal byte[] Serialize()
        {

            using (var memoryStream = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(SyncBlob));
                serializer.WriteObject(memoryStream, this);
                //var binaryFormatter = new BinaryFormatter();
                //binaryFormatter.Serialize(memoryStream, this);
                return memoryStream.ToArray();
            }
        }

        internal static SyncBlob DeSerialize(byte[] syncBlob)
        {
            SyncBlob blob;

            using (var memoryStream = new MemoryStream(syncBlob))
            {

                memoryStream.Position = 0;
                var s = new StreamReader(memoryStream);

                Debug.WriteLine(s.ReadToEnd());
             
                memoryStream.Position = 0;
                
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(SyncBlob));
                
                blob = serializer.ReadObject(memoryStream) as SyncBlob;

                //var binaryFormatter = new BinaryFormatter();
                //blob = binaryFormatter.Deserialize(memoryStream) as SyncBlob;
            }

            return blob;
        }

        public override string ToString()
        {

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("ClientScopeName : " + ClientScopeName);
            sb.AppendLine("IsLastBatch : " + IsLastBatch);
            sb.AppendLine("BatchCode : " + this.BatchCode);
            sb.AppendLine("NextBatch : " + this.NextBatch);

            return sb.ToString();
        }
    }
}
