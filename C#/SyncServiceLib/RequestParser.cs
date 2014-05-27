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
using System.Collections.Specialized;
using System.Linq;
using System.Xml;
using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Services.Formatters;

namespace Microsoft.Synchronization.Services
{
    /// <summary>
    /// This class encapsulates logic related to parsing the incoming HttpRequest and the message body.
    /// It identifies the type of request and collects all the necessary information to process the request.
    /// </summary>
    internal sealed class RequestParser
    {
        #region Private Members

        private readonly HttpContextServiceHost _serviceHost;
        private readonly SyncServiceConfiguration _configuration;

        private byte[] _syncBlob;
        private readonly List<IOfflineEntity> _entityList = new List<IOfflineEntity>();
        private readonly Dictionary<string, string> _idToTempIdMapping = new Dictionary<string, string>();

        #endregion

        #region Constructor

        internal RequestParser(HttpContextServiceHost serviceHost, SyncServiceConfiguration configuration)
        {
            _serviceHost = serviceHost;
            _configuration = configuration;
        }

        #endregion

        internal Request ParseIncomingRequest()
        {
            // Steps:
            // 1. Parse and validate request URI format (/syncscope/syncoperation)
            // 2. Validate QueryString using the HttpContextServiceHost.VerifyQueryParameters method.
            // 3. Parse and save query string parameters
            // 4. Identify and save request type, scopename, syncblob, request body.

            // throw BadRequest if duplicates are found.
            _serviceHost.VerifyQueryParameters();
            SyncSerializationFormat outputSerializationFormat = _serviceHost.GetOutputSerializationFormat(_configuration.SerializationFormat);
            SyncTracer.Verbose("Output Serialization format: {0}", outputSerializationFormat);

            RequestCommand requestCommand = GetRequestCommandType();
            SyncTracer.Verbose("RequestCommand type: {0}", requestCommand);

            List<IOfflineEntity> entities = null;
            Dictionary<CommandParamType, object> commandParameters = null;

            // Get command paramaters (filter params, scope name etc.) for all request types except $syncScopes
            if (requestCommand != RequestCommand.SyncScopes)
            {
                commandParameters = GetCommandParameters(_serviceHost.QueryStringCollection);
            }

            // Read the payload, headers etc for upload and download request types.
            if (requestCommand == RequestCommand.DownloadChanges || requestCommand == RequestCommand.UploadChanges)
            {
                ReadIncomingRequestDetails();
                entities = GetEntityListFromRequest(requestCommand);
            }

            var request = new Request(requestCommand, _serviceHost, commandParameters, _syncBlob, entities, outputSerializationFormat)
                              {
                                  IdToTempIdMapping = _idToTempIdMapping
                              };

            return request;
        }

        #region Private Methods

        private List<IOfflineEntity> GetEntityListFromRequest(RequestCommand requestCommand)
        {
            if (requestCommand != RequestCommand.UploadChanges)
            {
                return null;
            }

            return _entityList;
        }

        /// <summary>
        /// Gets the scope name from the query string.
        /// </summary>
        /// <returns>scope name</returns>
        private string GetScopeName()
        {
            // The item at index 0 of the RelativeUriSegments array contains the scope name.
            if (_serviceHost.RelativeUriSegments.Length < 1 || String.IsNullOrEmpty(_serviceHost.RelativeUriSegments[0].Trim()))
            {
                throw SyncServiceException.CreateBadRequestError(Strings.MissingScopeNameInRequest);
            }

            // Scope names are compared in a case insensitive manner and used as keys in lower case.
            return _serviceHost.RelativeUriSegments[0].ToLowerInvariant();
        }

        /// <summary>
        /// Gets the filter parameters from the query string. This excludes the system parameters such as 'syncscope', 'operation' etc.
        /// </summary>
        /// <param name="queryStringCollection">Query string</param>
        /// <returns>Dictionary of filter parameters</returns>
        private static Dictionary<string, string> GetFilterParamsFromIncomingRequest(NameValueCollection queryStringCollection)
        {
            var filterParams = new Dictionary<string, string>();

            foreach (string key in queryStringCollection.Keys)
            {
                filterParams.Add(key.ToLowerInvariant(), queryStringCollection[key].ToLowerInvariant());
            }

            return filterParams;
        }

        /// <summary>
        /// Get a dictionary that contains command parameters.
        /// </summary>
        /// <param name="queryStringCollection">Querystring represented as a NameValueCollection</param>
        /// <returns>Dictionary of command parameter types and values.</returns>
        private Dictionary<CommandParamType, object> GetCommandParameters(NameValueCollection queryStringCollection)
        {
            var commandParams = new Dictionary<CommandParamType, object>();

            Dictionary<string, string> filterParams = GetFilterParamsFromIncomingRequest(queryStringCollection);
            commandParams.Add(CommandParamType.FilterParameters, filterParams);

            string scopeName = GetScopeName();

            // Check the scope requested against the list of enabled scopes.
            if (!_configuration.ScopeNames.Contains(scopeName.ToLowerInvariant()))
            {
                throw SyncServiceException.CreateBadRequestError(Strings.SyncScopeNotSupported);
            }

            commandParams.Add(CommandParamType.ScopeName, scopeName);

            return commandParams;
        }

        /// <summary>
        /// Gets the current request type.
        /// </summary>
        /// <returns>Request command type</returns>
        private RequestCommand GetRequestCommandType()
        {
            // Check if this is a request for $syncscopes
            if (_serviceHost.RelativeUriSegments.Length == 1 && 
                0 == String.Compare(_serviceHost.RelativeUriSegments[0], SyncServiceConstants.SYNC_SCOPES_URL_SEGMENT, StringComparison.InvariantCultureIgnoreCase))
            {
                // Only GET is allowed for $syncscopes, throw an error if the request http method is not GET.
                if (0 != String.Compare(_serviceHost.RequestHttpMethod, SyncServiceConstants.HTTP_VERB_GET, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw SyncServiceException.CreateMethodNotAllowed(Strings.InvalidHttpMethodForSyncScopesRequest, SyncServiceConstants.HTTP_VERB_GET);
                }

                return RequestCommand.SyncScopes;
            }

            // The item at index 1 of the RelativeUriSegments array contains the operation name.
            if (_serviceHost.RelativeUriSegments.Length < 2 || String.IsNullOrEmpty(_serviceHost.RelativeUriSegments[1].Trim()))
            {
                throw SyncServiceException.CreateBadRequestError(Strings.MissingOperationNameinRequest);
            }

            RequestCommand requestCommand;
            string operation = _serviceHost.RelativeUriSegments[1]; 

            switch (operation.ToLowerInvariant())
            {
                case SyncServiceConstants.SYNC_OPERATION_UPLOAD_CHANGES:
                    // Only POST is allowed for upload changes, throw an error if the request http method is not POST.
                    if (0 != String.Compare(_serviceHost.RequestHttpMethod, SyncServiceConstants.HTTP_VERB_POST, StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw SyncServiceException.CreateMethodNotAllowed(Strings.InvalidHttpMethodForUploadChangesRequest, SyncServiceConstants.HTTP_VERB_GET);
                    }

                    requestCommand = RequestCommand.UploadChanges;

                    break;

                case SyncServiceConstants.SYNC_OPERATION_DOWNLOAD_CHANGES:
                    requestCommand = RequestCommand.DownloadChanges;

                    break;
                case SyncServiceConstants.SYNC_OPERATION_SCOPE_METADATA:
                    // Only GET is allowed for $metadata, throw an error if the request http method is not GET.
                    if (0 != String.Compare(_serviceHost.RequestHttpMethod, SyncServiceConstants.HTTP_VERB_GET, StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw SyncServiceException.CreateMethodNotAllowed(Strings.InvalidHttpMethodForScopeMetadataRequest, SyncServiceConstants.HTTP_VERB_GET);
                    }

                    requestCommand = RequestCommand.ScopeMetadata;

                    break;

                default:
                    throw SyncServiceException.CreateBadRequestError(Strings.InvalidOperationName);
            }

            return requestCommand;
        }

        /// <summary>
        /// Read request details for Download/upload operations.
        /// </summary>
        private void ReadIncomingRequestDetails()
        {
            if (0 == String.Compare(_serviceHost.RequestHttpMethod, SyncServiceConstants.HTTP_VERB_GET, true))
            {
                SyncTracer.Info("Request HTTP method is GET");

                ReadIncomingRequestStreamForGet();
            }
            else
            {
                SyncTracer.Info("Request HTTP method is POST");

                // Parse request stream and populate members.
                ReadIncomingRequestStreamForPost();
            }
        }

        /// <summary>
        /// Read and parse the incoming request stream for a POST request.
        /// </summary>
        private void ReadIncomingRequestStreamForPost()
        {
            if (null == _serviceHost.RequestStream || !_serviceHost.RequestStream.CanRead)
            {
                SyncTracer.Info("Request stream for HTTP POST is empty, null or cannot be read.");
                return;
            }

            try
            {
                var reader = WebUtil.GetSyncReader(_serviceHost.GetRequestContentSerializationFormat(),
                                                   _serviceHost.RequestStream,
                                                   _configuration.TypeToTableGlobalNameMapping.Keys.ToArray());

                reader.Start();

                while (reader.Next())
                {
                    switch (reader.ItemType)
                    {
                        case ReaderItemType.Entry:
                            IOfflineEntity entity = reader.GetItem();

                            if (entity.ServiceMetadata.IsTombstone)
                            {
                                if (String.IsNullOrEmpty(entity.ServiceMetadata.Id))
                                {
                                    throw SyncServiceException.CreateBadRequestError(Strings.TombstoneEntityHasNoId);
                                }

                                WebUtil.ParseIdStringAndPopulateKeyFields(entity, _serviceHost.ServiceBaseUri);
                            }

                            _entityList.Add(entity);

                            bool hasTempId = false;
                            if (reader.HasTempId())
                            {
                                // Save the entity id to tempId mapping for use later when writing response.
                                _idToTempIdMapping.Add(WebUtil.GenerateOfflineEntityId(entity), reader.GetTempId());

                                hasTempId = true;
                            }

                            // Make sure, we have atleast one of Id or TempId
                            if (String.IsNullOrEmpty(entity.ServiceMetadata.Id) && !hasTempId)
                            {
                                throw SyncServiceException.CreateBadRequestError(Strings.BothIdAndTempIdAreMissing);
                            }

                            break;
                        case ReaderItemType.SyncBlob:
                            _syncBlob = reader.GetServerBlob();

                            break;
                    }
                }
            }
            catch (XmlException exception)
            {
                SyncTracer.Warning("XmlException: {0}", WebUtil.GetExceptionMessage(exception));

                throw SyncServiceException.CreateBadRequestError(Strings.BadRequestPayload);
            }
        }

        /// <summary>
        /// Read and parse the incoming request details for a DownloadChanges GET request.
        /// </summary>
        private void ReadIncomingRequestStreamForGet()
        {
            _syncBlob = null;

            // The syncblob is in the headers or querystring for a get request and there are no entities sent from the client.

            // Check the query string for the server blob since it overrides the header.
            string syncServerBlobQueryValue = _serviceHost.QueryStringCollection[SyncServiceConstants.SYNC_SERVERBLOB_QUERYKEY];

            if (!String.IsNullOrEmpty(syncServerBlobQueryValue) && syncServerBlobQueryValue.Trim().Length > 0)
            {
                SyncTracer.Verbose("Server blob read from query string for HTTP GET: {0}", syncServerBlobQueryValue);

                _syncBlob = Convert.FromBase64String(syncServerBlobQueryValue.Trim());
            }
            // Check the request headers for the blob.
            else if (!String.IsNullOrEmpty(_serviceHost.RequestHeaders.Get(SyncServiceConstants.SYNC_SERVERBLOB_HEADERKEY)) &&
                     _serviceHost.RequestHeaders[SyncServiceConstants.SYNC_SERVERBLOB_HEADERKEY].Trim().Length > 0)
            {
                SyncTracer.Verbose("Server blob read from request headers for HTTP GET: {0}", syncServerBlobQueryValue);

                _syncBlob = Convert.FromBase64String(_serviceHost.RequestHeaders[SyncServiceConstants.SYNC_SERVERBLOB_HEADERKEY].Trim());
            }
            else
            {
                SyncTracer.Info("No server blob in query string or header.");
            }
        }

        #endregion
    }
}
