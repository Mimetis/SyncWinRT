// Copyright 2010 Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License"); 
// You may not use this file except in compliance with the License. 
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 

// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, 
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR 
// CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, 
// MERCHANTABLITY OR NON-INFRINGEMENT. 

// See the Apache 2 License for the specific language governing 
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Globalization;

#if SERVER
using Microsoft.Synchronization.Services;
using System.Data;
#elif CLIENT
using Microsoft.Synchronization.ClientServices;
using System.ComponentModel.DataAnnotations;
#endif

namespace Microsoft.Synchronization.Services.Formatters
{
    /// <summary>
    /// Class that will use .NET Reflection to serialize and deserialize an Entity to Atom/JSON
    /// </summary>
    class ReflectionUtility
    {
        static object _lockObject = new object();
        static Dictionary<string, PropertyInfo[]> _stringToPropInfoMapping = new Dictionary<string, PropertyInfo[]>();
        static Dictionary<string, PropertyInfo[]> _stringToPKPropInfoMapping = new Dictionary<string, PropertyInfo[]>();
        static Dictionary<string, ConstructorInfo> _stringToCtorInfoMapping = new Dictionary<string, ConstructorInfo>();

        public static PropertyInfo[] GetPropertyInfoMapping(Type type)
        {
            PropertyInfo[] props = null;
            if (!_stringToPropInfoMapping.TryGetValue(type.FullName, out props))
            {
                lock (_lockObject)
                {
                    if (!_stringToPropInfoMapping.TryGetValue(type.FullName, out props))
                    {
                        props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        props = props.Where((e) => 
                            (!e.Name.Equals("ServiceMetadata", StringComparison.Ordinal) && 
                            e.GetGetMethod() != null && 
                            e.GetSetMethod() != null && 
                            (Attribute.IsDefined(e, typeof(SyncEntityPropertyMappingAttribute)) |
                            e.DeclaringType.Equals(type)))).ToArray();

                        _stringToPropInfoMapping[type.FullName] = props;

#if CLIENT
                        // Look for the fields marked with [Key()] Attribute
                        PropertyInfo[] keyFields = props.Where((e) => e.GetCustomAttributes(typeof(KeyAttribute), true).Count() > 0).ToArray();

                        if (keyFields.Length == 0)
                        {
                            throw new InvalidOperationException(string.Format("Entity {0} does not have the any property marked with the [KeyAttribute].", type.Name));
                        }
                        _stringToPKPropInfoMapping[type.FullName] = keyFields;
#elif SERVER
                        // Look for the Primary key fields
                        SyncEntityTypeAttribute attr = (SyncEntityTypeAttribute)type.GetCustomAttributes(typeof(SyncEntityTypeAttribute), true).FirstOrDefault();
                        if (attr == null)
                        {
                            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Entity {0} does not have the mandatory SyncEntityTypeAttribute defined.", type.Name));
                        }

                        if (null == attr.KeyFields)
                        {
                            throw new InvalidOperationException(
                                string.Format("Entity {0} does not have the KeyFields defined for the SyncEntityTypeAttribute.", type.Name));
                        }

                        List<string> keyFields = attr.KeyFields.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                        if (keyFields.Count == 0)
                        {
                            throw new InvalidOperationException(string.Format("Entity {0} does not have the KeyFields defined for the SyncEntityTypeAttribute.", type.Name));
                        }
                        _stringToPKPropInfoMapping[type.FullName] = props.Where((e) => keyFields.Contains(e.Name)).ToArray();
#endif

                        // Look for the constructor info
                        ConstructorInfo ctorInfo = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Where((e) => e.GetParameters().Count() == 0).FirstOrDefault();
                        if (ctorInfo == null)
                        {
                            throw new InvalidOperationException(string.Format("Type {0} does not have a public parameterless constructor.", type.FullName));
                        }
                        _stringToCtorInfoMapping[type.FullName] = ctorInfo;
                    }
                }
            }
            return props;
        }

        /// <summary>
        /// Get the PropertyInfo array for all Key fields
        /// </summary>
        /// <param name="type">Type to reflect on</param>
        /// <returns>PropertyInfo[]</returns>
        public static PropertyInfo[] GetPrimaryKeysPropertyInfoMapping(Type type)
        {
            PropertyInfo[] props = null;

            if (!_stringToPKPropInfoMapping.TryGetValue(type.FullName, out props))
            {
                GetPropertyInfoMapping(type);
                _stringToPKPropInfoMapping.TryGetValue(type.FullName, out props);
            }
            return props;
        }

        /// <summary>
        /// Build the OData Atom primary keystring representation
        /// </summary>
        /// <param name="live">Entity for which primary key is required</param>
        /// <returns>String representation of the primary key</returns>
        public static string GetPrimaryKeyString(IOfflineEntity live)
        {
            StringBuilder builder = new StringBuilder();

            string sep = string.Empty;
            foreach (PropertyInfo keyInfo in ReflectionUtility.GetPrimaryKeysPropertyInfoMapping(live.GetType()))
            {
                if (keyInfo.PropertyType == FormatterConstants.GuidType)
                {
                    builder.AppendFormat("{0}{1}=guid'{2}'", sep, keyInfo.Name, keyInfo.GetValue(live, null));
                }
                else if (keyInfo.PropertyType == FormatterConstants.StringType)
                {
                    builder.AppendFormat("{0}{1}='{2}'", sep, keyInfo.Name, keyInfo.GetValue(live, null));
                }
                else
                {
                    builder.AppendFormat("{0}{1}={2}", sep, keyInfo.Name, keyInfo.GetValue(live, null));
                }

                if(string.IsNullOrEmpty(sep))
                {
                    sep = ", ";
                }
            }
            return builder.ToString();
        }

        public static IOfflineEntity GetObjectForType(EntryInfoWrapper wrapper, Type[] knownTypes)
        {
            Type entityType = null;

            ConstructorInfo ctorInfo = null;

            // See if its cached first.
            if (!_stringToCtorInfoMapping.TryGetValue(wrapper.TypeName, out ctorInfo))
            {
                // Its not cached. Try to look for it then in list of known types.
                if (knownTypes != null)
                {
                    entityType = knownTypes.Where((e) => e.FullName.Equals(wrapper.TypeName, StringComparison.InvariantCulture)).FirstOrDefault();

                    if (entityType == null)
                    {
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Unable to find a matching type for entry '{0}' in list of KnownTypes.", wrapper.TypeName));
                    }
                }
                else
                {
                    // Try to look for the type in the list of all loaded assemblies.
#if SERVER
                    Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
#elif CLIENT
                    Assembly[] loadedAssemblies = new Assembly[] { Assembly.GetExecutingAssembly(), Assembly.GetCallingAssembly()};
#endif
                    foreach (Assembly assembly in loadedAssemblies)
                    {
#if SERVER
                        entityType = assembly.GetTypes().Where((e) => e.FullName.Equals(wrapper.TypeName, StringComparison.InvariantCulture) &&
                            e.GetCustomAttributes(typeof(SyncEntityTypeAttribute), true).Count() > 0).FirstOrDefault();
#elif CLIENT
                        entityType = assembly.GetTypes().Where((e) => e.FullName.Equals(wrapper.TypeName, StringComparison.InvariantCulture) &&
                            e.GetInterface("IOfflineEntity", false) != null).FirstOrDefault();
#endif 
                        if (entityType != null)
                        {
                            break;
                        }
                    }

                    if (entityType == null)
                    {
                        throw new InvalidOperationException(string.Format("Unable to find a matching type for entry '{0}' in the loaded assemblies. Specify the type name in the KnownTypes argument to the SyncReader instance.", wrapper.TypeName));
                    }
                }

                // Reflect this entity and get necessary info
                ReflectionUtility.GetPropertyInfoMapping(entityType);
                ctorInfo = _stringToCtorInfoMapping[wrapper.TypeName];
            }
            else
            {
                entityType = ctorInfo.ReflectedType;
            }

            // Invoke the ctor
            object obj = ctorInfo.Invoke(null);

            // Set the parameters only for non tombstone items
            if (!wrapper.IsTombstone)
            {
                PropertyInfo[] props = GetPropertyInfoMapping(entityType);
                foreach (PropertyInfo pinfo in props)
                {
                    string value = null;
                    if (wrapper.PropertyBag.TryGetValue(pinfo.Name, out value))
                    {
                        pinfo.SetValue(obj, GetValueFromType(pinfo.PropertyType, value), null);
                    }
                }
            }

            IOfflineEntity entity = (IOfflineEntity)obj;
            entity.ServiceMetadata = new OfflineEntityMetadata(wrapper.IsTombstone, wrapper.Id, wrapper.ETag, wrapper.EditUri);
            return entity;
        }

#if SERVER
        /// <summary>
        /// Reflects the Type to figure out which is the primary key for the DataTable
        /// </summary>
        /// <param name="table">Table to set primary keys on</param>
        /// <param name="type">Type to reflect</param>
        /// <param name="mappingInfo">Global to Local column name mappings</param>
        public static void SetDataTablePrimaryKeys(DataTable table, Type type, Dictionary<string,string> mappingInfo)
        {
            PropertyInfo[] pinfo = GetPrimaryKeysPropertyInfoMapping(type);
            DataColumn[] pKeys = new DataColumn[pinfo.Length];

            for (int i = 0; i < pinfo.Length; i++)
            {
                string colName = null;
                mappingInfo.TryGetValue(pinfo[i].Name, out colName);
                pKeys[i] = table.Columns[colName ?? pinfo[i].Name];
            }

            // Set the primary keys on the table
            table.PrimaryKey = pKeys;
        }
#endif

        private static object GetValueFromType(Type type, string value)
        {
            if (value == null)
            {
                if (type.IsGenericType)
                {
                    return null;
                }
                else if (!type.IsPrimitive)
                {
                    return null;
                }
                else
                {
                    // Error case. Value cannot be null for a non nullable primitive type
                    throw new InvalidOperationException("Error in deserializing type " + type.FullName);
                }
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == FormatterConstants.NullableType)
            {
                type = type.GetGenericArguments()[0];
            }

            if (FormatterConstants.StringType.IsAssignableFrom(type))
            {
                return value;
            }
            else if (FormatterConstants.ByteArrayType.IsAssignableFrom(type))
            {
                return Convert.FromBase64String(value);
            }
            else if (FormatterConstants.GuidType.IsAssignableFrom(type))
            {
                return new Guid(value);
            }
            else if (FormatterConstants.DateTimeType.IsAssignableFrom(type) ||
                FormatterConstants.DateTimeOffsetType.IsAssignableFrom(type) ||
                FormatterConstants.TimeSpanType.IsAssignableFrom(type))
            {
                return FormatterUtilities.ParseDateTimeFromString(value, type);
            }
            else if (FormatterConstants.DecimalType.IsAssignableFrom(type))
            {
                return Decimal.Parse(value, NumberStyles.Any, NumberFormatInfo.InvariantInfo);
                // return Convert.ChangeType(value + "M", type, null);
            }
            else if (FormatterConstants.FloatType.IsAssignableFrom(type))
            {
                return float.Parse(value, NumberStyles.Any, NumberFormatInfo.InvariantInfo);
            }
            else if (type.IsPrimitive)
            {
                return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
            }
            return value;
        }

#if ENABLE_TEST_HOOKS
        public static void ClearCache()
        {
            _stringToPropInfoMapping = new Dictionary<string, PropertyInfo[]>();
            _stringToPKPropInfoMapping = new Dictionary<string, PropertyInfo[]>();
            _stringToCtorInfoMapping = new Dictionary<string, ConstructorInfo>();
        }
#endif
    }
}
