using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Microsoft.Synchronization.Services.Formatters
{
 
    public static class ReflectionExtensionMethods
    {
        public static PropertyInfo GetProperty(this Type type, String propertyName)
        {
            return type.GetTypeInfo().GetDeclaredProperty(propertyName);
        }

        public static IEnumerable<PropertyInfo> GetProperties(this Type type)
        {
            return type.GetTypeInfo().DeclaredProperties;
        }

        public static MethodInfo GetMethod(this Type type, String methodName)
        {
            return type.GetTypeInfo().GetDeclaredMethod(methodName);
        }

        public static bool IsSubclassOf(this Type type, Type parentType)
        {
            return type.GetTypeInfo().IsSubclassOf(parentType);
        }

        public static bool IsAssignableFrom(this Type type, Type parentType)
        {
            return type.GetTypeInfo().IsAssignableFrom(parentType.GetTypeInfo());
        }

        public static bool IsEnum(this Type type)
        {
            return type.GetTypeInfo().IsEnum;
        }

        public static bool IsPrimitive(this Type type)
        {
            return type.GetTypeInfo().IsPrimitive;
        }

        public static Type GetBaseType(this Type type)
        {
            return type.GetTypeInfo().BaseType;
        }

        public static bool IsGenericType(this Type type)
        {
            return type.GetTypeInfo().IsGenericType;
        }

        public static Type[] GetGenericArguments(this Type type)
        {
            return type.GetTypeInfo().GenericTypeArguments;
        }

        public static object GetPropertyValue(this Object instance, string propertyValue)
        {
            return instance.GetType().GetTypeInfo().GetDeclaredProperty(propertyValue).GetValue(instance);
        }

        public static TypeInfo GetTypeInfo(this Type type)
        {
            IReflectableType reflectableType = (IReflectableType)type;
            return reflectableType.GetTypeInfo();
        }
    }


}
