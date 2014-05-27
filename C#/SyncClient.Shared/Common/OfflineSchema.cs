using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Synchronization.ClientServices.SQLite;

namespace Microsoft.Synchronization.ClientServices.Common
{
    /// <summary>
    /// This class is used to specify the schema used by the IsolatedStorageOfflineContext
    /// </summary>
    public class OfflineSchema
    {
        private readonly Dictionary<string, Type> collections;

        /// <summary>
        /// Default constructor
        /// </summary>
        public OfflineSchema()
        {
            collections = new Dictionary<string, Type>();
        }

        /// <summary>
        /// Returns the list of types used for collections.
        /// </summary>
        public ReadOnlyCollection<Type> Collections
        {
            get { return new ReadOnlyCollection<Type>(new List<Type>(collections.Values)); }
        }

        /// <summary>
        /// Adds a new collection for the type T, where T : WinEightStorageOfflineEntity
        /// </summary>
        /// <typeparam name="T">Type of entity for the new collection (where T : IOfflineEntity)</typeparam>
        public void AddCollection<T>() where T : IOfflineEntity
        {
            Type t = typeof (T);

            // TODO : A gérer le cas d'une OfflineEntity depuis WP8 (avec un define je pense)
            //if (t == typeof(OfflineEntity) && OfflineEntity.GetEntityKeyProperties(t).Length == 0)
            //    throw new ArgumentException("Type: " + t.FullName + " does not have a key specified");

            if (t == typeof(SQLiteOfflineEntity) && SQLiteOfflineEntity.GetEntityPrimaryKeyProperties(t).Length == 0)
                throw new ArgumentException("Type: " + t.FullName + " does not have a primary key specified");
  

            collections.Add(t.FullName, t);
        }
    }
}