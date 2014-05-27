// Copyright © Microsoft Corporation. All rights reserved.

// Microsoft Limited Permissive License (Ms-LPL)

// This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.

// 1. Definitions
// The terms “reproduce,” “reproduction,” “derivative works,” and “distribution” have the same meaning here as under U.S. copyright law.
// A “contribution” is the original software, or any additions or changes to the software.
// A “contributor” is any person that distributes its contribution under this license.
// “Licensed patents” are a contributor’s patent claims that read directly on its contribution.

// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any contributors’ name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
// (E) The software is licensed “as-is.” You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.
// (F) Platform Limitation- The licenses granted in sections 2(A) & 2(B) extend only to the software or derivative works that you create that run on a Microsoft Windows operating system product.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Microsoft.Synchronization.Services.Formatters;

namespace Microsoft.Synchronization.Services
{
    internal static class WebUtil
    {
        #region Members

        internal static bool? ServiceIsFriend = null;

        private static readonly object _lockObject = new object();

        #endregion

        internal static T CheckArgumentNull<T>(T value, string parameterName) where T : class
        {
            if (value == null)
            {
                throw Error.ArgumentNull(parameterName);
            }
            return value;
        }

        internal static void CheckArgumentEmpty(string value, string parameterName)
        {
            if (value.Equals(String.Empty))
            {
                throw Error.ArgumentEmpty(parameterName);
            }
        }

        internal static bool CompareMimeType(string mimeType1, string mimeType2)
        {
            return string.Equals(mimeType1, mimeType2, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Generic ChangeType method. Extends the Convert.ChangeType functionality to cover more cases.
        /// </summary>
        /// <param name="value">Value to change</param>
        /// <param name="type">Type to convert to</param>
        /// <returns>item of type passed passed as a parameter</returns>
        internal static object ChangeType(object value, Type type)
        {
            if (value == null && type.IsGenericType) return Activator.CreateInstance(type);

            if (value == null) return null;

            if (type == value.GetType()) return value;

            if (type.IsEnum)
            {
                if (value is string)
                {
                    return Enum.Parse(type, value as string);
                }

                return Enum.ToObject(type, value);
            }

            if (!type.IsInterface && type.IsGenericType)
            {
                Type innerType = type.GetGenericArguments()[0];
                object innerValue = ChangeType(value, innerType);
                return Activator.CreateInstance(type, new[] { innerValue });
            }

            if (value is string && type == typeof(Guid)) return new Guid(value as string);

            if (value is string && type == typeof(Version)) return new Version(value as string);

            if (!(value is IConvertible)) return value;

            return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Checks if an exception can be handled by the service.
        /// </summary>
        /// <param name="exception">Exception to check</param>
        /// <returns>True - If exception cannot be handled (fatal), false - otherwise.</returns>
        internal static bool IsFatalException(Exception exception)
        {
            while (exception != null)
            {
                if (((exception is OutOfMemoryException) && !(exception is InsufficientMemoryException)) ||
                    (((exception is ThreadAbortException) || (exception is AccessViolationException)) || (exception is SEHException)))
                {
                    return true;
                }

                if (!(exception is TypeInitializationException) && !(exception is TargetInvocationException))
                {
                    break;
                }

                exception = exception.InnerException;
            }

            return false;
        }

        /// <summary>
        /// Checks if a type belongs to a friend assembly.
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>true if type belongs to a friend assembly, false otherwise.</returns>
        internal static bool IsFriendClass(Type type)
        {
            lock (_lockObject)
            {
                if (ServiceIsFriend == null)
                {
                    string assemblyName = string.Empty;
                    string publicKey = string.Empty;
                    bool hasStrongName = false;

                    ServiceIsFriend = false;

                    // Look for the type's Assembly name and private key in the assembly strong name
                    // if the assembly has no StrongName it is not a friend assembly                
                    System.Collections.IEnumerator e = type.Assembly.Evidence.GetHostEnumerator();
                    e.Reset();
                    while (e.MoveNext())
                    {
                        if (e.Current is System.Security.Policy.StrongName)
                        {
                            hasStrongName = true;
                            System.Security.Policy.StrongName sn = e.Current as System.Security.Policy.StrongName;
                            assemblyName = sn.Name;
                            publicKey = sn.PublicKey.ToString();
                            break;
                        }
                    }

                    if (hasStrongName)
                    {
                        // Compare the assemblyName and publicKey against the list of InternalsVisibleTo assemblies.  
                        foreach (object assembly in Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(System.Runtime.CompilerServices.InternalsVisibleToAttribute), false))
                        {
                            System.Runtime.CompilerServices.InternalsVisibleToAttribute friendClass = assembly as System.Runtime.CompilerServices.InternalsVisibleToAttribute;


                            string[] friendAssemblyName = friendClass.AssemblyName.Split(',', '=');

                            // friendClass.AssemblyName should match the following format: "Name, PublicKey=PublicKeyBlob"
                            // If the assembly has no PublicKey continue, the friend class has to be signed
                            if (friendAssemblyName.Length != 3) continue;

                            if (friendAssemblyName[0].Equals(assemblyName, StringComparison.InvariantCultureIgnoreCase) && friendAssemblyName[2].Equals(publicKey, StringComparison.InvariantCultureIgnoreCase))
                            {
                                ServiceIsFriend = true;
                                break;

                            }
                        }
                    }

                    // Result gets cached in ServiceIsFriend for future requests
                }

            }

            return (bool)ServiceIsFriend;
        }

        internal static SyncConflictResolution GetSyncConflictResolution(ConflictResolutionPolicy conflictResolutionPolicy)
        {
            switch (conflictResolutionPolicy)
            {
                case ConflictResolutionPolicy.ClientWins:
                    return SyncConflictResolution.ClientWins;
                case ConflictResolutionPolicy.ServerWins:
                    return SyncConflictResolution.ServerWins;
                default:
                    throw SyncServiceException.CreateInternalServerError(Strings.UnsupportedConflictResolutionPolicy);

            }
        }

        internal static SyncWriter GetSyncWriter(SyncSerializationFormat serializationFormat, Uri baseUri)
        {
            switch (serializationFormat)
            {
                case SyncSerializationFormat.ODataAtom:
                    return new ODataAtomWriter(baseUri);
                case SyncSerializationFormat.ODataJson:
                    return new ODataJsonWriter(baseUri);
                default:
                    throw new NotImplementedException();
            }
        }

        internal static SyncReader GetSyncReader(SyncSerializationFormat serializationFormat, Stream stream, Type[] knownTypes)
        {
            switch (serializationFormat)
            {
                case SyncSerializationFormat.ODataAtom:
                    return new ODataAtomReader(stream, knownTypes);
                case SyncSerializationFormat.ODataJson:
                    return new ODataJsonReader(stream, knownTypes);
                default:
                    throw new NotImplementedException();
            }
        }

        internal static string GetContentType(SyncSerializationFormat format)
        {
            switch (format)
            {
                case SyncSerializationFormat.ODataAtom:
                    return "application/atom+xml";
                case SyncSerializationFormat.ODataJson:
                    return "application/json";
                default:
                    throw SyncServiceException.CreateBadRequestError("Unsupported serialization format");
            }
        }

        internal static string GetExceptionMessage(Exception exception)
        {
            if (null == exception)
            {
                return String.Empty;
            }

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(exception.GetType().ToString());
            stringBuilder.AppendLine(exception.Message);
            stringBuilder.AppendLine(exception.StackTrace);
            stringBuilder.AppendLine();

            Exception innerException = exception.InnerException;
            while (null != innerException)
            {
                stringBuilder.AppendLine(innerException.GetType().ToString());
                stringBuilder.AppendLine(innerException.Message);
                stringBuilder.AppendLine(innerException.StackTrace);
                stringBuilder.AppendLine();

                innerException = innerException.InnerException;
            }

            stringBuilder.AppendLine();

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Create a WCF message object that is sent as a response. Write the contents of the XDocument to the response.
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        internal static Message CreateResponseMessage(XDocument document)
        {
            using (document.CreateReader())
            {
                Message message = Message.CreateMessage(MessageVersion.None, String.Empty, document.CreateReader());

                message.Properties.Add(WebBodyFormatMessageProperty.Name, new WebBodyFormatMessageProperty(WebContentFormat.Xml));

                var property = new HttpResponseMessageProperty { StatusCode = HttpStatusCode.OK };

                property.Headers[HttpResponseHeader.ContentType] = FormatterConstants.ApplicationXmlContentType;

                message.Properties.Add(HttpResponseMessageProperty.Name, property);

                return message;
            }
        }

        /// <summary>
        /// Generate the Id for an entity. The format currently is the OData Id.
        /// i.e. http://baseUri/tableName(primarykeylist)
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        internal static string GenerateOfflineEntityId(IOfflineEntity entity)
        {
            var primaryKeyString = ReflectionUtility.GetPrimaryKeyString(entity);

            if (String.IsNullOrEmpty(primaryKeyString))
            {
                throw SyncServiceException.CreateInternalServerError(
                    String.Format("GetPrimaryKeyString method returned an empty string for entity type {0}.", entity.GetType()));
            }

            return String.Format(@"{0}/{1}({2})", WebOperationContext.Current.IncomingRequest.UriTemplateMatch.BaseUri, entity.GetType().Name, primaryKeyString);
        }

        /// <summary>
        /// Parse the Id string and populate key fields of the object. This is called when the client sends tombstones
        /// and the Key fields are not present in the input payload.
        /// The approach used is to parse each key field individually and set it to a property with the same name.
        /// For example: For http://host/service.svc/Tag(ID=1), we parse out ID=1 and then populate the ID property of the targetObject with the
        /// value 1.
        /// </summary>
        /// <param name="entity">Entity for which we need to set the key fields.</param>
        /// <param name="serviceBaseUri">
        /// Base Uri of the service. The ServiceMetadata.Id property has the Uri which we want to strip off before attempting to 
        /// parse the keys and values.
        /// </param>
        internal static void ParseIdStringAndPopulateKeyFields(IOfflineEntity entity, Uri serviceBaseUri)
        {
            Debug.Assert(null != entity);
            Debug.Assert(!String.IsNullOrEmpty(entity.ServiceMetadata.Id));

            string idString = entity.ServiceMetadata.Id;
                       
            // Remove the ServiceUri and the entity type name from the EntityId
            // Note: Case sensitive comparisons are made since the client isn't supposed to change the Id for an
            // entity.
            string serviceUriWithTableName = serviceBaseUri + "/" + entity.GetType().Name;

            // If the Id does not have the correct format of serviceUri/TableName, then we should not continue further.
            if (!idString.StartsWith(serviceUriWithTableName, false, CultureInfo.InvariantCulture))
            {
                throw SyncServiceException.CreateBadRequestError(String.Format(Strings.EntityIdFormatIsIncorrect, idString));
            }

            // Remove the host and the table name from the Id.
            // Example: http://host/service.svc/table(id=123) will become (id=123)
            idString = idString.Remove(0, serviceUriWithTableName.Length);

            // Remove leading '/' if any. After this the id string is of the format (ID=1) or (ID=guid'<guidValue>')
            // If there are multiple Id values then, they are comma separated.));))
            if (idString.StartsWith("/"))
            {
                idString = idString.Substring(1);
            }

            // Make sure the ( and ) parenthesis exist.
            if (String.IsNullOrEmpty(idString) || idString[0] != '(' || idString[idString.Length - 1] != ')')
            {
                throw SyncServiceException.CreateBadRequestError(String.Format(Strings.EntityIdFormatIsIncorrect, entity.ServiceMetadata.Id));
            }

            // Remove the ( and ) characters.
            idString = idString.Substring(1, idString.Length - 2);

            // Get the key properties for the entity.
            var keyFieldPropertyInfoList = ReflectionUtility.GetPrimaryKeysPropertyInfoMapping(entity.GetType());

            // Split the string and get individual keyvalue pair strings. They key and value are still a single string separated by '='.
            // for types such as Guid, the value will be prefixed with 'guid'.
            string[] primaryKeyValuePair = idString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            // Throw if there is a mismatch between the key count of the entity and that passed in the URI.
            if (primaryKeyValuePair.Length != keyFieldPropertyInfoList.Length)
            {
                throw SyncServiceException.CreateBadRequestError(String.Format(Strings.BadRequestKeyCountMismatch, entity.ServiceMetadata.Id, entity.GetType()));
            }

            // At this point, we have key value pairs of the form "ID=1".
            foreach (var keyValuePair in primaryKeyValuePair)
            {
                // example: ID=1
                string[] keyValue = keyValuePair.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);

                // Every key should have only 2 components.
                Debug.Assert(2 == keyValue.Length);

                // Get the property from the key list.
                string key = keyValue[0].Trim();
                var propertyInfo = keyFieldPropertyInfoList.Where(p => p.Name == key).FirstOrDefault();

                if (null == propertyInfo)
                {
                    throw SyncServiceException.CreateBadRequestError(
                        String.Format(Strings.BadRequestKeyNotFoundInResource, key, entity.ServiceMetadata.Id, entity.GetType()));
                }

                // Get typed value of the value.
                object targetValue;
                // Parse the value based on the target type.
                if (!ODataIdParser.TryKeyStringToPrimitive(keyValue[1], propertyInfo.PropertyType, out targetValue))
                {
                    throw SyncServiceException.CreateBadRequestError(
                        String.Format(Strings.UnableToParseKeyValueForProperty, keyValuePair, entity.ServiceMetadata.Id));
                }

                // Set the property value.
                propertyInfo.SetValue(entity, targetValue, null);
            }
        }

        internal static void ValidateInterceptorSignature(SyncInterceptorAttribute attr, MethodInfo methodInfo, string parentTypeName)
        {
            // Retrieve the set of parameters
            ParameterInfo[] parameters = methodInfo.GetParameters();
            if (attr is SyncRequestInterceptorAttribute || attr is SyncResponseInterceptorAttribute)
            {
                // Ensure methodInfo is of format public void MethodName(SyncOperationContext context)
                if (methodInfo.ReturnType != SyncServiceConstants.VOID_TYPE
                    || parameters.Length != 1
                    || (parameters[0].ParameterType != SyncServiceConstants.SYNC_OPERATIONCONTEXT_TYPE))
                {
                    throw new InvalidOperationException(string.Format(SyncServiceConstants.SYNC_INCORRECT_INTERCEPTOR_SIGNATURE,
                        methodInfo.Name,
                        parentTypeName,
                        attr.GetType().Name,
                        SyncServiceConstants.SYNC_REQUEST_INTERCEPTOR_FORMAT));
                }
            }
            else
            {
                // Ensure methodInfo is of format public SyncUploadConflictResolution MethodName(SyncUploadConflictContext context)
                if (methodInfo.ReturnType != SyncServiceConstants.SYNC_CONFLICT_RESOLUTION_TYPE
                    || parameters.Length != 2
                    || (parameters[0].ParameterType != SyncServiceConstants.SYNC_CONFLICT_CONTEXT_TYPE)
                    || (parameters[1].ParameterType != SyncServiceConstants.IOFFLINEENTITY_BYREFTYPE))
                {
                    throw new InvalidOperationException(string.Format(SyncServiceConstants.SYNC_INCORRECT_INTERCEPTOR_SIGNATURE,
                        methodInfo.Name,
                        parentTypeName,
                        attr.GetType().Name,
                        SyncServiceConstants.SYNC_CONFLICT_INTERCEPTOR_FORMAT));
                }
            }
        }

        internal static Exception ThrowDuplicateInterceptorException(string className, string scopeName, SyncInterceptorAttribute attr)
        {
            return new InvalidOperationException(
                string.Format("SyncService '{0}' has mutiple '{1}' interceptors on SyncOperations '{2}' defined for scopename '{3}'",
                className,
                attr.GetType().Name,
                attr.Operation,
                scopeName));
        }

        internal static Exception ThrowDuplicateInterceptorForArgumentException(string className, string scopeName, 
            SyncInterceptorAttribute attr, string argumentName)
        {
            return new InvalidOperationException(
                string.Format("SyncService '{0}' has mutiple '{1}' interceptors on SyncOperations '{2}' defined for Type '{3}' and scopename '{4}'",
                className,
                attr.GetType().Name,
                attr.Operation,
                argumentName,
                scopeName));
        }

        internal static Exception ThrowFilteredAndNonFilteredInterceptorException(string className, string scopeName, SyncInterceptorAttribute attr)
        {
            return new InvalidOperationException(
                string.Format("SyncService '{0}' has both filtered and non-filtered '{1}' interceptor's defined for scope '{2}'.",
                className,
                attr.GetType().Name,
                scopeName));
        }

        internal static Exception ThrowInterceptorArgumentNotIOEException(string className, string scopeName, SyncInterceptorAttribute attr, string argumentName)
        {
            return new InvalidOperationException(
                string.Format("SyncService '{0}' filtered interceptor definition '{1}' for scope '{2}' declares a Type '{3}' that does not derive from IOfflineEntity.",
                className,
                attr.GetType().Name,
                scopeName, 
                argumentName));
        }
    }
}
