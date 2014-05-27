using System;
using System.Collections.Generic;
using Microsoft.Synchronization.ClientServices.Common;


namespace Microsoft.Synchronization.ClientServices.IsolatedStorage
{
    /// <summary>
    /// This class manages object identities during archiving.  It guarantees that an object won't be
    /// written to an archive file more than once.  Because an object can have either a primary key,
    /// or an atom id, or both, it manages the relationships between these.  The contract is that
    /// the calling code must call ProcessedEntity after each entity it encounters, whether it is
    /// written to the archive file or not.
    /// </summary>
    class ArchiveIdManager
    {
        readonly Set<string> atomIdSet;
        readonly Set<OfflineEntityKey> pkeySet;

        public ArchiveIdManager()
        {
            atomIdSet = new Set<string>();
            pkeySet = new Set<OfflineEntityKey>();
        }

        public bool ContainsEntity(OfflineEntity entity)
        {
            // If the entity is a tombstone, use the atom id
            if (entity.IsTombstone)
            {
                if (String.IsNullOrEmpty(entity.ServiceMetadata.Id))
                {
                    // if it's a tombstone and the id is null, it means it is a delete of
                    // a local insert that can be skipped, so we report it as already written
                    return true;
                }
                return atomIdSet.Contains(entity.ServiceMetadata.Id);
            }

            if (!String.IsNullOrEmpty(entity.ServiceMetadata.Id))
                return atomIdSet.Contains(entity.ServiceMetadata.Id);

            OfflineEntityKey key = (OfflineEntityKey)entity.GetIdentity();
            key.TypeName = entity.GetType().FullName;

            return pkeySet.Contains(key);
        }

        public void ProcessedEntity(OfflineEntity entity)
        {
            string atomId = entity.ServiceMetadata.Id;
            OfflineEntityKey key = (OfflineEntityKey)entity.GetIdentity();
            key.TypeName = entity.GetType().FullName;
            
            if (entity.IsTombstone)
            {
                if (String.IsNullOrEmpty(atomId))
                    pkeySet.Add(key);
                else
                    atomIdSet.Add(atomId);
            }
            else
            {
                pkeySet.Add(key);

                if (!String.IsNullOrEmpty(atomId))
                    atomIdSet.Add(atomId);
            }
        }


        /// <summary>
        /// This class keeps track of a unique set of items of type T.
        /// </summary>
        /// <typeparam name="T">The type of item for which to store a unique set.</typeparam>
        class Set<T>
        {
            // bool is used here because it uses less space.
            Dictionary<T, bool> _dictionary;
 
            public Set()
            {
                _dictionary = new Dictionary<T, bool>();
            }

            public void Add(T t)
            {

                _dictionary[t] = false;
            }

            public bool Contains(T t)
            {
                return _dictionary.ContainsKey(t);
            }

        }
    }


}
