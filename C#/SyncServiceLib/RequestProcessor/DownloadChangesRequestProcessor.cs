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
using System.ServiceModel.Channels;
using Microsoft.Synchronization.Services.Formatters;
using Microsoft.Synchronization.Services.SqlProvider;
using System.Collections.Specialized;
using System.Net;
using System.Linq;

namespace Microsoft.Synchronization.Services
{
    /// <summary>
    /// Handler for the download changes request command.
    /// </summary>
    internal class DownloadChangesRequestProcessor : SyncRequestProcessorBase, IRequestProcessor
    {
        #region Private Members

        private GetChangesResponse _getChangesResponse;
        private Uri _baseUri;
        private SyncSerializationFormat _responseSerializationFormat;
        private readonly WebHeaderCollection _interceptorsResponseHeaders = new WebHeaderCollection();

        #endregion

        #region Constructor

        public DownloadChangesRequestProcessor(SyncServiceConfiguration configuration, HttpContextServiceHost serviceHost)
            : base(configuration, serviceHost)
        {
            base._syncOperation = SyncOperations.Download;
        }

        #endregion

        protected override void InitRequestOperationContext()
        {
            base._operationContext = new SyncOperationContext()
            {
                ScopeName = this._scopeName,
                Operation = this._syncOperation,
                QueryString = new NameValueCollection(_serviceHost.QueryStringCollection.Count, _serviceHost.QueryStringCollection),
                RequestHeaders = _serviceHost.RequestHeaders,
                ResponseHeaders = _interceptorsResponseHeaders
            };
        }

        protected override void InitResponseOperationContext()
        {
            base._operationContext = new SyncDownloadResponseOperationContext()
            {
                ScopeName = this._scopeName,
                Operation = this._syncOperation,
                QueryString = new NameValueCollection(_serviceHost.QueryStringCollection.Count, _serviceHost.QueryStringCollection),
                RequestHeaders = _serviceHost.RequestHeaders,
                ResponseHeaders = _interceptorsResponseHeaders,
            };
        }

        #region IRequestProcessor Members

        /// <summary>
        /// Process the incoming request and forms a formatted outgoing response.
        /// </summary>
        /// <param name="incomingRequest">Incoming request</param>
        /// <returns>Message instance containing the outgoing response</returns>
        public Message ProcessRequest(Request incomingRequest)
        {
            _baseUri = _serviceHost.ServiceBaseUri;

            _responseSerializationFormat = incomingRequest.ResponseSerializationFormat;

            // Check and fire request interceptor.
            if (_configuration.HasRequestInterceptors(this._scopeName, SyncOperations.Download))
            {
                // Init the SyncOperationContext
                this.InitRequestOperationContext();

                // Fire the request Interceptors if any
                base.ProcessRequestInterceptors();
            }

            SqlSyncProviderService providerService =
                new SqlSyncProviderService(_configuration,
                                           Convert.ToString(incomingRequest.CommandParams[CommandParamType.ScopeName]),
                                           incomingRequest.ProviderParams, 
                                           base._operationContext);

            _getChangesResponse = providerService.GetChanges(incomingRequest.SyncBlob);

            // Check and fire response interceptor.
            this.PrepareAndProcessResponseInterceptors();

            var oDataWriter = GetSyncWriterWithContents();

            return base.CreateResponseMessage(incomingRequest.ResponseSerializationFormat, oDataWriter);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Function that is used to check and fire the DownloadResponse interceptors code.
        /// It check to see if it has non typed interceptors and if yes then invokes it. Else it
        /// checks it any typed filter exists for the current entites being sent back and invokes them.
        /// </summary>
        private void PrepareAndProcessResponseInterceptors()
        {
            if (_configuration.HasResponseInterceptors(this._scopeName, SyncOperations.Download) ||
                _configuration.HasTypedResponseInterceptors(this._scopeName, SyncOperations.Download))
            {
                // Init the response SyncOperationContext
                this.InitResponseOperationContext();

                SyncDownloadResponseOperationContext context = base._operationContext as SyncDownloadResponseOperationContext;

                // Set context properties.
                context.IsLastBatch = _getChangesResponse.IsLastBatch;

                if (_configuration.HasResponseInterceptors(this._scopeName, SyncOperations.Download))
                {
                    context.OutgoingChanges = _getChangesResponse.EntityList.AsReadOnly();

                    // Fire the response Interceptors if any
                    base.ProcessResponseInterceptors();
                }
                else
                {
                    // Group the entities by its Type. This gives us a grouping with a Key parameter
                    // that has the actual type.
                    foreach (IGrouping<Type, IOfflineEntity> grouping in _getChangesResponse.EntityList.GroupBy(e => e.GetType()))
                    {
                        if (_configuration.HasTypedResponseInterceptor(this._scopeName, SyncOperations.Download, grouping.Key))
                        {
                            // Select the items in current group
                            context.OutgoingChanges = grouping.ToList().AsReadOnly();

                            // Fire the response Interceptors if any
                            base.ProcessTypedResponseInterceptors(grouping.Key);
                        }
                    }
                }
            }
        }

        // This method ensures that we have a valid feed through the formatters. The BodyWriter delegate in WCF
        // seems to not recover from an unhandled exception caused by the formatters and sends out an empty response to the caller. 
        // This is a workaround for this issue until we find a better solution.
        private SyncWriter GetSyncWriterWithContents()
        {
            // Get the appropriate SyncWriter instance based on the serialization format.
            SyncWriter oDataWriter = WebUtil.GetSyncWriter(_responseSerializationFormat, _baseUri);

            oDataWriter.StartFeed(_getChangesResponse.IsLastBatch, _getChangesResponse.ServerBlob);

            // Write entities for response.
            foreach (var entity in _getChangesResponse.EntityList)
            {
                // Set the Id and add item with a null tempId.

                entity.ServiceMetadata.Id = WebUtil.GenerateOfflineEntityId(entity);

                oDataWriter.AddItem(entity, null /*tempId*/);
            }

            return oDataWriter;
        }

        #endregion
    }
}
