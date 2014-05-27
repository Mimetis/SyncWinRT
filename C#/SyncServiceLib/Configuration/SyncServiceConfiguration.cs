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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Services.SqlProvider;
using System.ServiceModel;
using System.Globalization;
using System.Diagnostics;

namespace Microsoft.Synchronization.Services
{
    /// <summary>
    /// This class is used to configure service wide policies for the SyncService implementation.
    /// </summary>
    internal sealed class SyncServiceConfiguration : ISyncServiceConfiguration
    {
        #region Members

        /// <summary>
        /// Contains mapping of table global name name to entity type.
        /// </summary>
        internal Dictionary<string, Type> TableGlobalNameToTypeMapping { get; private set; }

        /// <summary>
        /// For faster lookup, this cache stores the reverse mapping between a type and the corresponding table global name.
        /// The global name is used when manipulating Datasets.
        /// </summary>
        internal Dictionary<Type, string> TypeToTableGlobalNameMapping { get; private set; }

        /// <summary>
        /// For faster lookup, this cache stores the mapping between a type and the corresponding table local name.
        /// The local name is needed when making direct queries against the datastore.
        /// </summary>
        internal Dictionary<Type, string> TypeToTableLocalNameMapping { get; set; }

        /// <summary>
        /// List of scopes enabled.
        /// </summary>
        internal List<string> ScopeNames { get; private set; }

        /// <summary>
        /// Delegate to the InitializeService method in usercode.
        /// </summary>
        internal Func<Dictionary<string, string>, List<SqlSyncProviderFilterParameterInfo>> InitializeMethod { get; set; }

        /// <summary>
        /// Cache of the ScopeName - SyncInterceptors mapping
        /// </summary>
        Dictionary<string, SyncInterceptorsInfoWrapper> SyncInterceptors { get; set; }

        internal void ClearFilterParameters()
        {
            _filterParameters.Clear();
        }

        internal int? DownloadBatchSizeInKB;

        internal string BatchSpoolDirectory = Environment.GetEnvironmentVariable("TEMP");

        internal string ServiceTypeNamespace { get; private set; }

        /// <summary>
        /// Contains the SQL Schema that was used to provision the sync objects in the database.
        /// </summary>
        internal string SyncObjectSchema { get; private set; }

        private static readonly object _lockObject = new object();

        // default policies
        internal ConflictResolutionPolicy ConflictResolutionPolicy = ConflictResolutionPolicy.ClientWins;

        internal SyncSerializationFormat SerializationFormat = SyncSerializationFormat.ODataAtom;

        private List<SqlSyncProviderFilterParameterInfo> _filterParameters = new List<SqlSyncProviderFilterParameterInfo>();

        #endregion

        #region Constructors

        internal SyncServiceConfiguration(Type entityContainerType)
        {
            SyncInterceptors = new Dictionary<string, SyncInterceptorsInfoWrapper>();

            DiscoverTypes(entityContainerType);

            ServiceTypeNamespace = entityContainerType.Namespace;

            ScopeNames = new List<string>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Indicates if the configuration is initialized. 
        /// We ideally don't want to allow rediscovery of types (mainly for performance),
        /// so this flag is checked before the type discovery is attempted.
        /// </summary>
        internal bool IsInitialized { get; private set; }

        /// <summary>
        /// Readonly list that contains the filter parameters that the service is configured to operate on.
        /// </summary>
        internal ReadOnlyCollection<SqlSyncProviderFilterParameterInfo> FilterParameters
        {
            get
            {
                return _filterParameters.AsReadOnly();
            }
        }

        #endregion

        #region ISyncServiceConfiguration Members

        /// <summary>
        /// Change the default conflict resolution policy. The default value is ClientWins.
        /// </summary>
        /// <param name="policy">The new conflict resolution policy</param>
        public void SetConflictResolutionPolicy(ConflictResolutionPolicy policy)
        {
            ConflictResolutionPolicy = policy;
        }

        /// <summary>
        /// Change the default serialization format. The default value is ODataAtom.
        /// </summary>
        /// <param name="serializationFormat">serialization format</param>
        public void SetDefaultSyncSerializationFormat(SyncSerializationFormat serializationFormat)
        {
            SerializationFormat = serializationFormat;
        }

        /// <summary>
        /// Enable scopes.
        /// </summary>
        /// <param name="scopeName">Scope name to enable for sync.</param>
        /// <exception cref="ArgumentNullException">Throws when scopeName is null</exception>
        public void SetEnableScope(string scopeName)
        {
            WebUtil.CheckArgumentNull(scopeName, "scopeName");

            if (ScopeNames.Count != 0)
            {
                throw new InvalidOperationException("Sync Service already has a SyncScope registered. A Sync Service cannot be configured for multiple scopes.");
            }

            string lowerCaseScopeName = scopeName.ToLower();

            if (!ScopeNames.Contains(lowerCaseScopeName))
            {
                ScopeNames.Add(lowerCaseScopeName);
            }
        }

        /// <summary>
        /// Add a new filter parameter configuration.
        /// </summary>
        /// <param name="queryStringParam">Name of the querystring parameter</param>
        /// <param name="tableName">SQL table name</param>
        /// <param name="sqlParameterName">SQL parameter name (has to be exact since its used in query formation)</param>
        /// <param name="typeOfParam">Indicates the Type of the parameter</param>
        public void AddFilterParameterConfiguration(string queryStringParam, string tableName, string sqlParameterName, Type typeOfParam)
        {
            // Check if there is a filter parameter which has the same querystring, tablename and sqlparameter name.
            if (0 == _filterParameters.Where(p =>
                    0 == String.Compare(p.QueryStringKey, queryStringParam, StringComparison.InvariantCultureIgnoreCase) &&
                    0 == String.Compare(p.TableName, tableName, StringComparison.InvariantCultureIgnoreCase) &&
                    0 == String.Compare(p.SqlParameterName, sqlParameterName, StringComparison.InvariantCultureIgnoreCase)
                                            ).Count())
            {
                _filterParameters.Add(new SqlSyncProviderFilterParameterInfo
                                          {
                                              QueryStringKey = queryStringParam.ToLowerInvariant(),
                                              SqlParameterName = sqlParameterName,
                                              TableName = tableName,
                                              ValueType = typeOfParam
                                          });
            }
            else
            {
                throw SyncServiceException.CreateInternalServerError(Strings.DuplicateFilterParameter);
            }
        }

        /// <summary>
        /// Set the path where batches will be spooled. The directory must already exist. Default directory is %TEMP%.
        /// </summary>
        /// <param name="directoryPath">Path to the batch spooling directory.</param>
        public void SetBatchSpoolDirectory(string directoryPath)
        {
            WebUtil.CheckArgumentNull(directoryPath, "directoryPath");

            if (!Directory.Exists(directoryPath))
            {
                // Throw a generic exception here so that we don't return the string value for InvalidBatchSpoolDirectory
                // when UseVerboseErrors = false. For Verbose Errors the exception message and stack trace are returned in the response.
                throw new InvalidOperationException(Strings.InvalidBatchSpoolDirectory);
            }

            BatchSpoolDirectory = directoryPath;
        }

        /// <summary>
        /// Set a download batch size. Batching is disabled by default.
        /// </summary>
        /// <param name="batchSizeInKB">Download batch size in KB</param>
        public void SetDownloadBatchSize(uint batchSizeInKB)
        {
            // Sébastien PERTUS : fix a bug on size ...
            DownloadBatchSizeInKB = (int?)(batchSizeInKB / 10);
        }

        /// <summary>
        /// Set the schema name under which sync related objects were generated in the SQL database when the database was provisioned.
        /// </summary>
        /// <param name="schemaName">Name of the schema under which sync related objects are created.</param>
        public void SetSyncObjectSchema(string schemaName)
        {
            SyncObjectSchema = schemaName;
        }

        /// <summary>
        /// Gets/Sets the server connection string.
        /// </summary>
        public string ServerConnectionString { get; set; }

        /// <summary>
        /// Gets/Sets the log level for sync operations. Default value is None.
        /// </summary>
        public bool UseVerboseErrors { get; set; }

        /// <summary>
        /// Indicates if batching is enabled on the provider service.
        /// </summary>
        public bool IsBatchingEnabled
        {
            get
            {
                return (null != DownloadBatchSizeInKB);
            }
        }

        /// <summary>Enable or disable the diagnostic page served by the $diag URL.</summary>
        public bool EnableDiagnosticPage { get; set; }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Invokes the static InitializeService method that allows one-time service wide policy configuration.
        /// </summary>
        /// <param name="syncServiceType">service type (used for reflection).</param>
        internal void Initialize(Type syncServiceType)
        {
            if (!IsInitialized)
            {
                lock (_lockObject)
                {
                    if (!IsInitialized)
                    {
                        // Initialize the filter parameter list, to remove data from previously 
                        // failed initialization attempt.
                        _filterParameters = new List<SqlSyncProviderFilterParameterInfo>();

                        // Invoke the static InitializeService method.
                        InvokeStaticInitialization(syncServiceType);

                        // Build Sync Operation Inspectors list
                        ReadSyncInterceptors(syncServiceType);

                        // Check for empty connection string.
                        if (String.IsNullOrEmpty(ServerConnectionString))
                        {
                            throw SyncServiceException.CreateInternalServerError(Strings.ConnectionStringNotSet);
                        }

                        // There are no dynamic scope changes allowed anyway, so why should the service start.
                        if (0 == ScopeNames.Count)
                        {
                            throw SyncServiceException.CreateInternalServerError(Strings.NoScopesVisible);
                        }

                        // We are initialized now.
                        IsInitialized = true;
                    }
                }
            }
        }

        /// <summary>
        /// Inspects and builds a list of SyncOperationInterceptor info from the user code.
        /// </summary>
        internal void ReadSyncInterceptors(Type syncServiceType)
        {
            // Look for all public instance methods that has custom attributes
            foreach (MethodInfo info in syncServiceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                // Check for request interceptors
                foreach (object attr in info.GetCustomAttributes(SyncServiceConstants.SYNC_REQUEST_INTERCEPTOR_TYPE, false))
                {
                    ProcessSyncInterceptor((SyncInterceptorAttribute)attr, syncServiceType, info);
                }

                // Check for response interceptors
                foreach (object attr in info.GetCustomAttributes(SyncServiceConstants.SYNC_RESPONSE_INTERCEPTOR_TYPE, false))
                {
                    ProcessSyncInterceptor((SyncInterceptorAttribute)attr, syncServiceType, info);
                }

                // Check for conflict interceptors
                foreach (object attr in info.GetCustomAttributes(SyncServiceConstants.SYNC_CONFLICT_INTERCEPTOR_TYPE, false))
                {
                    ProcessSyncInterceptor((SyncInterceptorAttribute)attr, syncServiceType, info);
                }
            }
        }

        /// <summary>
        /// Invokes the user method that is supposed to initialize a new client. 
        /// If this method is not defined, then just returns the scopeName passed in to the method. The caller
        /// has to ensure that this is unique for the client that is making the request.
        /// </summary>
        /// <param name="filterParams">Filter parameters obtained from the incoming request</param>
        /// <returns>Property bag containing provider specific filter params.</returns>
        internal List<SqlSyncProviderFilterParameterInfo> InvokeInitializeUserMethod(Dictionary<string, string> filterParams)
        {
            if (null != InitializeMethod)
            {
                return InitializeMethod(filterParams);
            }

            return null;
        }

        /// <summary>
        /// Utility function that invokes the actual user interceptor extension method. If entityType is null
        /// it looks for the generic interceptor. If its not null then it looks for a typed interceptor for the
        /// specific type being passed.
        /// </summary>
        /// <param name="context">The context to pass as parameter to user code</param>
        /// <param name="entityType">Type of the entity being processed</param>
        /// <param name="isRequest">True if intercepting a request operation else false.</param>
        internal void InvokeOperationInterceptors(SyncOperationContext context, Type entityType, bool isRequest)
        {
            SyncInterceptorsInfoWrapper wrapper = null;
            if (this.SyncInterceptors.TryGetValue(context.ScopeName, out wrapper))
            {
                MethodInfo methodInfo = null;
                switch (context.Operation)
                {
                    case SyncOperations.Download:
                        if (entityType == null)
                        {
                            methodInfo = (isRequest) ? wrapper.DownloadRequestInterceptor : wrapper.DownloadResponseInterceptor;
                        }
                        else
                        {
                            Debug.Assert(!isRequest, "Cannot fire typed interceptor for DownloadRequest");
                            methodInfo = wrapper.GetResponseInterceptor(SyncOperations.Download, entityType);
                        }
                        break;
                    case SyncOperations.Upload:
                        if (entityType == null)
                        {
                            methodInfo = (isRequest) ? wrapper.UploadRequestInterceptor : wrapper.UploadResponseInterceptor;
                        }
                        else
                        {
                            methodInfo = (isRequest) ? 
                                wrapper.GetRequestInterceptor(entityType) : 
                                wrapper.GetResponseInterceptor(SyncOperations.Upload, entityType);
                        }
                        break;
                }

                if (methodInfo != null)
                {
                    InvokeUserInterceptorMethod(methodInfo, OperationContext.Current.InstanceContext.GetServiceInstance(), new object[] { context });
                }
            }
        }

        /// <summary>
        /// Utility for invoking user code for conflict interceptors
        /// </summary>
        /// <param name="context">The context to pass as parameter to user code</param>
        /// <param name="mergedVersion">The merged version for Merge resolution</param>
        /// <param name="entityType">Entity type of the conflict being raised</param>
        /// <returns>Actual resolution picked by user</returns>
        internal SyncConflictResolution? InvokeConflictInterceptor(SyncConflictContext context, Type entityType, out IOfflineEntity mergedVersion)
        {
            SyncInterceptorsInfoWrapper wrapper = null;
            if (this.SyncInterceptors.TryGetValue(context.ScopeName, out wrapper))
            {
                // Look for unfiltered Conflict and if that is null then look for filtered one.
                // Its an error to have both unfiltered and filtered ConflictInterceptor so both cannot be set.
                MethodInfo methodInfo = wrapper.ConflictInterceptor ?? wrapper.GetConflictInterceptor(entityType);
                if (methodInfo != null)
                {
                    object[] inputParams = new object[] { context, null };
                    SyncConflictResolution resolution = (SyncConflictResolution)InvokeUserInterceptorMethod(methodInfo, OperationContext.Current.InstanceContext.GetServiceInstance(), inputParams);
                    // Merged version is in the second parameter which is passed by reference. Look it up
                    mergedVersion = (IOfflineEntity)inputParams[1];
                    return resolution;
                }
            }
            mergedVersion = null;
            return null;
        }

        /// <summary>
        /// Checks to see if a SyncConflictInterceptor has been configured by the user
        /// </summary>
        /// <returns>Boolean on whether or not a interceptor was configured</returns>
        internal bool HasConflictInterceptors(string scopeName)
        {
            SyncInterceptorsInfoWrapper wrapper = null;
            if (this.SyncInterceptors.TryGetValue(scopeName.ToLowerInvariant(), out wrapper))
            {
                return wrapper.HasConflictInterceptor();
            }
            return false;
        }

        /// <summary>
        /// Checks to see if a SyncRequestInterceptor for a specific operation has been configured by the user
        /// </summary>
        /// <returns>Boolean on whether or not a interceptor was configured</returns>
        internal bool HasRequestInterceptors(string scopeName, SyncOperations operation)
        {
            SyncInterceptorsInfoWrapper wrapper = null;
            if (this.SyncInterceptors.TryGetValue(scopeName.ToLowerInvariant(), out wrapper))
            {
                return wrapper.HasRequestInterceptor(operation);
            }
            return false;
        }

        /// <summary>
        /// Checks to see if a SyncResponseInterceptor for a specific operation has been configured by the user
        /// </summary>
        /// <returns>Boolean on whether or not a interceptor was configured</returns>
        internal bool HasResponseInterceptors(string scopeName, SyncOperations operation)
        {
            SyncInterceptorsInfoWrapper wrapper = null;
            if (this.SyncInterceptors.TryGetValue(scopeName.ToLowerInvariant(), out wrapper))
            {
                return wrapper.HasResponseInterceptor(operation);
            }
            return false;
        }

        /// <summary>
        /// Checks to see if a typed SyncRequestInterceptor for a specific operation has been configured by the user
        /// </summary>
        /// <returns>Boolean on whether or not a interceptor was configured</returns>
        internal bool HasTypedRequestInterceptors(string scopeName)
        {
            SyncInterceptorsInfoWrapper wrapper = null;
            if (this.SyncInterceptors.TryGetValue(scopeName.ToLowerInvariant(), out wrapper))
            {
                return wrapper.HasTypedRequestInterceptors();
            }
            return false;
        }

        /// <summary>
        /// Checks to see if a typed SyncResponseInterceptor for a specific operation has been configured by the user
        /// </summary>
        /// <returns>Boolean on whether or not a interceptor was configured</returns>
        internal bool HasTypedResponseInterceptors(string scopeName, SyncOperations operation)
        {
            SyncInterceptorsInfoWrapper wrapper = null;
            if (this.SyncInterceptors.TryGetValue(scopeName.ToLowerInvariant(), out wrapper))
            {
                return wrapper.HasTypedResponseInterceptors(operation);
            }
            return false;
        }

        /// <summary>
        /// Checks to see if any typed SyncConflictInterceptor has been configured by the user
        /// </summary>
        /// <returns>bool</returns>
        internal bool HasTypedConflictInterceptors(string scopeName)
        {
            SyncInterceptorsInfoWrapper wrapper = null;
            if (this.SyncInterceptors.TryGetValue(scopeName.ToLowerInvariant(), out wrapper))
            {
                return wrapper.HasTypedConflictInterceptors();
            }
            return false;
        }

        /// <summary>
        /// Checks to see if a typed SyncConflictInterceptor has been configured by the user
        /// </summary>
        /// <returns>bool</returns>
        internal bool HasTypedConflictInterceptor(string scopeName, Type type) 
        {
            SyncInterceptorsInfoWrapper wrapper = null;
            if (this.SyncInterceptors.TryGetValue(scopeName.ToLowerInvariant(), out wrapper))
            {
                return wrapper.HasConflictInterceptor(type);
            }
            return false;
        }

        /// <summary>
        /// Checks to see if a typed SyncRequestInterceptor for a specific operation has been configured by the user
        /// </summary>
        /// <returns>bool</returns>
        internal bool HasTypedRequestInterceptor(string scopeName, Type type)
        {
            SyncInterceptorsInfoWrapper wrapper = null;
            if (this.SyncInterceptors.TryGetValue(scopeName.ToLowerInvariant(), out wrapper))
            {
                return wrapper.HasRequestInterceptor(type);
            }
            return false;
        }

        /// <summary>
        /// Checks to see if a typed SyncResponseInterceptor for a specific operation has been configured by the user
        /// </summary>
        /// <returns>bool</returns>
        internal bool HasTypedResponseInterceptor(string scopeName, SyncOperations operation, Type type)
        {
            SyncInterceptorsInfoWrapper wrapper = null;
            if (this.SyncInterceptors.TryGetValue(scopeName.ToLowerInvariant(), out wrapper))
            {
                return wrapper.HasResponseInterceptor(operation, type);
            }
            return false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Invokes the actual MethodInfo object with the passed in input param. Returns the value of the invocation
        /// back to the caller.
        /// </summary>
        /// <param name="info">MethodInfo to invoke</param>
        /// <param name="sourceObject">Instance of user Service on which the interceptor is invoked</param>
        /// <param name="inputParams">Input parameters passed to the method</param>
        /// <returns>Return value from the invocation.</returns>
        private object InvokeUserInterceptorMethod(MethodInfo info, object sourceObject, object[] inputParams)
        {
            // Invoke the actual request
            return info.Invoke(sourceObject, inputParams);
        }

        /// <summary>
        /// This method processes a single SyncInterceptorAttribute defined on a method. Processing involves the following
        /// 1. Ensure that the MethodInfo signature is the right one for the interceptor.
        /// 2. Retrieve the ScopeNames defined in the attribute and ensure they are valid scopes configures via the 
        /// ISyncScopeConfiguration.SetEnableScope() API.
        /// 3. Create a SyncInterceptorInfoWrapper object for the scope if none is present.
        /// 4. Add the interceptor to the wrapper object.
        /// </summary>
        /// <param name="attr">The SyncInterceptorAttribute to process.</param>
        /// <param name="syncServiceType">Actual SyncService type</param>
        /// <param name="methodInfo">User Method on which the attribute is applied</param>
        private void ProcessSyncInterceptor(SyncInterceptorAttribute attr, Type syncServiceType, MethodInfo methodInfo)
        {
            // Validate the method signature attribute
            WebUtil.ValidateInterceptorSignature(attr, methodInfo, syncServiceType.Name);

            // Read the list of scopeNames from the attribute
            string[] scopeNames = attr.ScopeName.Select(e => e.ToLowerInvariant()).ToArray();

            foreach (string scopeName in scopeNames)
            {
                // Check to ensure the scopeName is valid configured scope.
                if (!this.ScopeNames.Contains(scopeName))
                {
                    // ScopeName is not part of configured scopes. Throw.
                    throw new InvalidOperationException(
                        string.Format(CultureInfo.InvariantCulture, "ScopeName '{0}' defined in '{1}' on method '{2}' is not in the list of configured sync scopes.",
                        scopeName, attr.GetType().Name, methodInfo.Name));
                }
                SyncInterceptorsInfoWrapper wrapper = null;
                // Check and create the wrapper object for the current scope if none exists.
                if (!this.SyncInterceptors.TryGetValue(scopeName, out wrapper))
                {
                    wrapper = new SyncInterceptorsInfoWrapper(scopeName);
                    this.SyncInterceptors.Add(scopeName, wrapper);
                }

                // Add interceptor to the wrapper.
                wrapper.AddInterceptor(attr, methodInfo, syncServiceType.Name);
            }
        }

        /// <summary>
        /// Reflect the types from T and cache it in a list for reference.
        /// </summary>
        private void DiscoverTypes(Type t)
        {
            TableGlobalNameToTypeMapping = new Dictionary<string, Type>();
            TypeToTableGlobalNameMapping = new Dictionary<Type, string>();
            TypeToTableLocalNameMapping = new Dictionary<Type, string>();

            // We want to find the underlying type of every generic type which is a private instance member of the templated class
            // and check if it has the [SyncEntityType] attribute applied to it. If this check passes, then
            // extract the type and the attribute information and save it for future reference.
            FieldInfo[] privateTypes = t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var privateType in privateTypes)
            {
                Type fieldType = privateType.FieldType;

                if (fieldType.IsGenericType)
                {
                    Type[] genericArguments = fieldType.GetGenericArguments();

                    if (null != genericArguments && 1 == genericArguments.Length)
                    {
                        var argument = genericArguments[0];

                        var attributes = argument.GetCustomAttributes(false);

                        object syncAttribute = null;
                        foreach (var attribute in attributes)
                        {
                            if (attribute.GetType() == typeof(SyncEntityTypeAttribute))
                            {
                                syncAttribute = attribute;
                                break;
                            }
                        }

                        if (null != syncAttribute)
                        {
                            // Read the TableGlobalName property value
                            PropertyInfo globalTablenamePropertyInfo = syncAttribute.GetType().GetProperty(SyncServiceConstants.SYNC_ENTITY_TYPE_TABLE_GLOBAL_NAME);
                            
                            //Note: We cannot ToLower() this because the datatable name for sqlsyncprovider makes a case sensitive comparison.
                            string globalTableName = Convert.ToString(globalTablenamePropertyInfo.GetValue(syncAttribute, null));

                            if (TableGlobalNameToTypeMapping.ContainsKey(globalTableName))
                            {
                                throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, Strings.DuplicateGlobalTableName, globalTableName));
                            }

                            if (String.IsNullOrEmpty(globalTableName))
                            {
                                throw new InvalidOperationException(Strings.TableGlobalNameCannotByEmpty);
                            }

                            TableGlobalNameToTypeMapping.Add(globalTableName, argument.UnderlyingSystemType);
                            
                            TypeToTableGlobalNameMapping.Add(argument.UnderlyingSystemType, globalTableName);

                            // Read the TableLocalName property value
                            PropertyInfo localTablenamePropertyInfo = syncAttribute.GetType().GetProperty(SyncServiceConstants.SYNC_ENTITY_TYPE_TABLE_LOCAL_NAME);

                            string localTableName = Convert.ToString(localTablenamePropertyInfo.GetValue(syncAttribute, null));

                            if (String.IsNullOrEmpty(localTableName))
                            {
                                throw new InvalidOperationException(Strings.TableLocalNameCannotByEmpty);
                            }

                            TypeToTableLocalNameMapping.Add(argument.UnderlyingSystemType, localTableName);
                        }
                    }
                }
            }

            if (0 == TableGlobalNameToTypeMapping.Count)
            {
                throw SyncServiceException.CreateInternalServerError(Strings.NoValidTypeFoundForSync);
            }
        }

        /// <summary>
        /// Invokes the InitializeService user method.
        /// </summary>
        /// <param name="type">service type (used for reflection)</param>
        private void InvokeStaticInitialization(Type type)
        {
            // Search for the InitializeService method going from most-specific to least-specific type.

            const BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

            while (type != null)
            {
                MethodInfo info = type.GetMethod("InitializeService", bindingAttr, null, new[] { typeof(ISyncServiceConfiguration) }, null);

                if ((info != null) && (info.ReturnType == typeof(void)))
                {
                    ParameterInfo[] parameters = info.GetParameters();

                    if ((parameters.Length == 1) && !parameters[0].IsOut)
                    {
                        var objArray = new object[] { this };
                        
                        try
                        {
                            info.Invoke(null, objArray);

                            return;
                        }
                        catch (TargetInvocationException exception)
                        {
                            SyncTracer.Warning("Exception invoking the static InitializeService method. Details {0}", WebUtil.GetExceptionMessage(exception));

                            ErrorHandler.HandleTargetInvocationException(exception);

                            throw;
                        }
                    }
                }

                type = type.BaseType;
            }

            // We should never exit from here when the InitializeService method is implemented.
            throw SyncServiceException.CreateInternalServerError(Strings.InitializeServiceMethodNotImplemented);
        }

        #region Test Hook

        //Note: This method changes the state of a static class. So results may not be as expected in multi-threaded scenarios.
        internal void InvokeTestHookInitializeMethod(Type type)
        {
            if (WebUtil.IsFriendClass(type))
            {

                const BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
                MethodInfo info = type.GetMethod("InitializeConfigurationTestHook", bindingAttr, null, new[] { typeof(ISyncServiceConfiguration) }, null);

                if ((info != null) && (info.ReturnType == typeof(void)))
                {
                    ParameterInfo[] parameters = info.GetParameters();
                    if ((parameters.Length == 1) && !parameters[0].IsOut)
                    {
                        var objArray = new object[] { this };
                        try
                        {
                            info.Invoke(null, objArray);
                        }
                        catch (TargetInvocationException exception)
                        {
                            SyncTracer.Warning("Exception invoking the TestHookInitialization method. Details {0}", WebUtil.GetExceptionMessage(exception));
                            ErrorHandler.HandleTargetInvocationException(exception);
                            throw;
                        }
                        return;
                    }
                }
            }
        }

        #endregion

        #endregion
    }
}
