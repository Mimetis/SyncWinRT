using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Synchronization.ClientServices.Common;
using Microsoft;

namespace Microsoft.Synchronization.ClientServices.SQLite
{
    public class SQLiteOfflineEntity : IOfflineEntity, INotifyPropertyChanged
    {
        /// <summary>
        /// Stores the information that must be persisted for OData.  The most important attributes
        /// are tombstone and id.
        /// </summary>
        OfflineEntityMetadata entityMetadata;

        /// <summary>
        /// Event raised whenever a Microsoft.Synchronization.ClientServices.IsolatedStorage.IsolatedStorageOfflineEntity
        /// has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;


        /// <summary>
        /// Called from a property setter to notify the framework that a property
        /// has changed.  This method will raise the PropertyChanged event and change
        /// the state to Modified if its current state is Unmodified or Submitted.
        /// </summary>
        /// <param name="propertyName"></param>
        protected void OnPropertyChanged(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException("propertyName");
           
            RaisePropertyChanged(propertyName);
        }

        /// <summary>
        /// Called from a property setter to notify the framework that a property
        /// is about to be changed.  This method will perform change-tracking related
        /// operations.
        /// </summary>
        /// <param name="propertyName">The name of the property that is changing</param>
        protected void OnPropertyChanging(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException("propertyName");

            return;
        }

        public OfflineEntityMetadata GetServiceMetadata()
        {
            return ServiceMetadata;
        }
        public void SetServiceMetadata(OfflineEntityMetadata value)
        {
            ServiceMetadata = value;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        //[Display(AutoGenerateField = false)]
        [DataMember]
        [Ignore]
        internal OfflineEntityMetadata ServiceMetadata
        {
            get
            {
                return entityMetadata;
            }

            set
            {
                if (value != entityMetadata)
                {
                    entityMetadata = value;
                    RaisePropertyChanged("EntityMetadata");
                }
            }
        }

        /// <summary>
        /// Returns all properties of the specified type which are keys for the type
        /// </summary>
        /// <param name="t">Type from which to retrieve properties</param>
        /// <returns>Key propeties for the type.</returns>
        internal static PropertyInfo[] GetEntityPrimaryKeyProperties(Type t)
        {
            return (from p in GetEntityProperties(t)
                    where p.GetCustomAttributes(typeof(PrimaryKeyAttribute), false).Count() != 0
                    select p).ToArray();
        }

        /// <summary>
        /// Returns all properties of the specified type which are passed for sync (all properties which have 
        /// getters and setters).
        /// </summary>
        /// <param name="t">Type from which to retrieve properties</param>
        /// <returns>Properties for the type</returns>
        internal static PropertyInfo[] GetEntityProperties(Type t)
        {
            return (from p in t.GetTypeInfo().DeclaredProperties
                    where p.GetMethod != null && p.SetMethod != null && p.DeclaringType == t
                    select p).ToArray();
        }

  

        /// <summary>
        /// Notifies the PropertyChanged event if it is registered.
        /// </summary>
        /// <param name="propertyName">Name of the property for which the event is being raised.</param>
        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler == null) return;

            handler(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
