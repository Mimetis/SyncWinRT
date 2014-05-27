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
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.ServiceModel.Channels;
using System.Xml;
using Microsoft.Synchronization.Services.Formatters;
using System.Collections.Specialized;

namespace Microsoft.Synchronization.Services
{
    internal abstract class SyncRequestProcessorBase
    {
        protected string _serverConnectionString = null;
        protected string _scopeName = null;
        protected ConflictResolutionPolicy _conflictResolutionPolicy = ConflictResolutionPolicy.ClientWins;
        protected SyncServiceConfiguration _configuration;
        protected HttpContextServiceHost _serviceHost;
        protected SyncOperations _syncOperation;
        protected SyncOperationContext _operationContext;

        protected SyncRequestProcessorBase(SyncServiceConfiguration configuration, HttpContextServiceHost serviceHost)
        {
            WebUtil.CheckArgumentNull(configuration, "configuration");

            Debug.Assert(0 != configuration.ScopeNames.Count);

            _configuration = configuration;
            _scopeName = configuration.ScopeNames[0];
            _serviceHost = serviceHost;

            _serverConnectionString = _configuration.ServerConnectionString;
            _conflictResolutionPolicy = _configuration.ConflictResolutionPolicy;
        }

        /// <summary>
        /// Virtual function that will be overriden by request processors that has Interceptors and needs
        /// a context object to encapsulate all request info.
        /// </summary>
        protected virtual void InitRequestOperationContext()
        {
            // no-op
        }

        /// <summary>
        /// Virtual function that will be overriden by request processors that has Interceptors and needs
        /// a context object to encapsulate all response info.
        /// </summary>
        protected virtual void InitResponseOperationContext()
        {
            // no-op
        }

        protected Message CreateResponseMessage(SyncSerializationFormat serializationFormat, SyncWriter oDataWriter)
        {
            var bodyWriter = new DelegateBodyWriter(WriteResponse, oDataWriter);

            Message message = Message.CreateMessage(MessageVersion.None, String.Empty, bodyWriter);

            

            switch (serializationFormat)
            {
                case SyncSerializationFormat.ODataAtom:
                    message.Properties.Add(WebBodyFormatMessageProperty.Name, new WebBodyFormatMessageProperty(WebContentFormat.Xml));
                    break;
                case SyncSerializationFormat.ODataJson:
                    message.Properties.Add(WebBodyFormatMessageProperty.Name, new WebBodyFormatMessageProperty(WebContentFormat.Json));
                    break;
            }

            

            var property = new HttpResponseMessageProperty { StatusCode = HttpStatusCode.OK };
            property.Headers[HttpResponseHeader.ContentType] = WebUtil.GetContentType(serializationFormat);
            
            // Copy the SyncOperationContext's ResponseHeaders if present
            if (this._operationContext != null)
            {
                property.Headers.Add(this._operationContext.ResponseHeaders);
            }

            message.Properties.Add(HttpResponseMessageProperty.Name, property);

            return message;
        }

        protected void ProcessRequestInterceptors()
        {
            this._configuration.InvokeOperationInterceptors(this._operationContext, null/*entityType*/, true/*isRequest*/);
        }

        protected void ProcessResponseInterceptors()
        {
            this._configuration.InvokeOperationInterceptors(this._operationContext, null/*entityType*/, false/*isRequest*/);
        }

        protected void ProcessTypedRequestInterceptors(Type entityType)
        {
            this._configuration.InvokeOperationInterceptors(this._operationContext, entityType, true/*isRequest*/);
        }

        protected void ProcessTypedResponseInterceptors(Type entityType)
        {
            this._configuration.InvokeOperationInterceptors(this._operationContext, entityType, false/*isRequest*/);
        }

        /// <summary>
        /// Delegate passed into the custom body writer to form the outgoing response.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="syncWriter"></param>
        private static void WriteResponse(XmlDictionaryWriter writer, SyncWriter syncWriter)
        {
            try
            {
                
                syncWriter.WriteFeed(writer);
            }
            catch (Exception exception)
            {
                // An exception at this point seems to be unrecoverable but ideally we should not hit exceptions since we are only
                // writing to the XmlDictionaryWriter. 
                SyncServiceTracer.TraceError("Exception in WriteResponse method. Details: {0}", WebUtil.GetExceptionMessage(exception));
            }
        }
    }
}