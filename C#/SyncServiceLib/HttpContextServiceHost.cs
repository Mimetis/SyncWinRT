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
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Web;
using System.Web;
using Microsoft.Synchronization.Data;

namespace Microsoft.Synchronization.Services
{
    /// <summary>
    /// Exposes the http headers and other utility methods that act on the HttpRequest class.
    /// Also contains a reference to the message body of the incoming request.
    /// </summary>
    internal sealed class HttpContextServiceHost
    {
        private const string CONTENT_TYPE_APPLICATION_ATOM = "application/atom+xml";
        private const string CONTENT_TYPE_APPLICATION_JSON = "application/json";
        private const string CONTENT_TYPE_ANY = "*/*";
        private const string SYNC_FORMAT_QUERYKEY = "$format";
        private const string CONTENT_TYPE_APPLICATION_ANY = "application/*";

        static readonly string[] _allowedAcceptsHeader = new[] { CONTENT_TYPE_ANY, CONTENT_TYPE_APPLICATION_ATOM, CONTENT_TYPE_APPLICATION_JSON };

        #region Private Members

        private readonly Stream _incomingMessageBody;
        private readonly WebOperationContext _operationContext;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the incoming request stream (message body).
        /// </summary>
        internal Stream RequestStream
        {
            get
            {
                return _incomingMessageBody;
            }
        }

        /// <summary>
        /// Gets the host header (which contains the host name and port)
        /// </summary>
        internal string HostHeader
        {
            get
            {
                return _operationContext.IncomingRequest.Headers[HttpRequestHeader.Host];
            }
        }

        /// <summary>
        /// Gets the Http Accepts header.
        /// </summary>
        internal string RequestAccept
        {
            get
            {
                return _operationContext.IncomingRequest.Accept;
            }
        }

        /// <summary>
        /// Gets the http method of the incoming request.
        /// We dont handle the X-HTTP-Method header ("verb tunneling") since the only verbs supported are GET and POST.
        /// See http://msdn.microsoft.com/en-us/library/dd541471(PROT.10).aspx for more information about the X-HTTP-Method header.
        /// </summary>
        internal string RequestHttpMethod
        {
            get
            {
                return _operationContext.IncomingRequest.Method;
            }
        }

        /// <summary>
        /// The If-Match header, which specifies that the requested operation should be performed
        /// only if the client's cached copy of the indicated resource is current.
        /// </summary>
        internal string RequestIfMatch
        {
            get
            {
                return _operationContext.IncomingRequest.Headers[HttpRequestHeader.IfMatch];
            }
        }

        /// <summary>
        /// The If-None-Match header, which specifies that the requested operation should be
        /// performed only if none of client's cached copies of the indicated resources are current.
        /// </summary>
        internal string RequestIfNoneMatch
        {
            get
            {
                return _operationContext.IncomingRequest.Headers[HttpRequestHeader.IfNoneMatch];
            }
        }

        /// <summary>
        /// Gets or sets the HTTP Content-Type header value.
        /// </summary>
        internal string ResponseContentType
        {
            get
            {
                return _operationContext.OutgoingResponse.ContentType;
            }
            set
            {
                _operationContext.OutgoingResponse.ContentType = value;
            }
        }

        /// <summary>
        /// Gets or sets the HTTP status code of the response.
        /// </summary>
        internal int ResponseStatusCode
        {
            get
            {
                return (int)_operationContext.OutgoingResponse.StatusCode;
            }
            set
            {
                var statusCode = (HttpStatusCode)value;
                _operationContext.OutgoingResponse.StatusCode = statusCode;
                _operationContext.OutgoingResponse.SuppressEntityBody = MustNotReturnMessageBody(statusCode);
            }
        }

        /// <summary>
        /// Gets the protocol headers associated with the incoming request.
        /// </summary>
        internal WebHeaderCollection RequestHeaders
        {
            get
            {
                return _operationContext.IncomingRequest.Headers;
            }
        }

        /// <summary>
        /// Gets the protocol headers associated with the outgoing response.
        /// </summary>
        internal WebHeaderCollection ResponseHeaders
        {
            get
            {
                return _operationContext.OutgoingResponse.Headers;
            }
        }

        /// <summary>
        /// Gets the incoming request uri.
        /// </summary>
        internal Uri RequestUri
        {
            get
            {
                return _operationContext.IncomingRequest.UriTemplateMatch.RequestUri;
            }
        }

        /// <summary>
        /// Gets a NameValueCollection that contains query string information.
        /// </summary>
        internal NameValueCollection QueryStringCollection
        {
            get
            {
                return HttpUtility.ParseQueryString(RequestUri.Query);
            }
        }

        /// <summary>
        /// Gets the segments from the request URI.
        /// </summary>
        internal string[] RelativeUriSegments
        {
            get
            {
                return _operationContext.IncomingRequest.UriTemplateMatch.RelativePathSegments.ToArray();
            }
        }

        /// <summary>
        /// Get the service base uri.
        /// </summary>
        public Uri ServiceBaseUri
        {
            get
            {
                return _operationContext.IncomingRequest.UriTemplateMatch.BaseUri;
            }
        }

        /// <summary>
        /// Gets the outgoing response context. 
        /// </summary>
        public OutgoingWebResponseContext OutgoingResponse
        {
            get { return _operationContext.OutgoingResponse; }
        }

        #endregion

        #region Constructor

        /// <summary>Intialize a new instance of the class with a null RequestStream property value.</summary>
        internal HttpContextServiceHost() : this(null)
        {
        }

        internal HttpContextServiceHost(Stream messageBody)
        {
            _incomingMessageBody = messageBody;
            _operationContext = WebOperationContext.Current;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get the value for a query string item.
        /// </summary>
        /// <param name="item">Item to search for in the incoming request uri.</param>
        /// <returns>Value of the item.</returns>
        internal string GetQueryStringItem(string item)
        {
            NameValueCollection values = _operationContext.IncomingRequest.UriTemplateMatch.QueryParameters;

            // Check if we have the item by using a direct function (good performance)
            // Note: the underlying data structure in a NameValueCollection is a hashtable.
            string[] strArray = values.GetValues(item);

            // If no match found, then loop through all keys and do a case insensitive search.
            if ((strArray == null) || (strArray.Length == 0))
            {
                string str = null;
                foreach (string str2 in values.Keys)
                {
                    if ((str2 != null) && StringComparer.OrdinalIgnoreCase.Equals(str2.Trim(), item))
                    {
                        if (str != null)
                        {
                            // Bad request
                            throw SyncServiceException.CreateBadRequestError(Strings.DuplicateParametersInRequestUri);
                        }
                        str = str2;
                        strArray = values.GetValues(str2);
                    }
                }
                if ((strArray == null) || (strArray.Length == 0))
                {
                    return null;
                }
            }

            if (strArray.Length != 1)
            {
                // syntax error, cannot have multiple querystring parameters with the same name.
                throw SyncServiceException.CreateBadRequestError(Strings.DuplicateParametersInRequestUri);
            }

            return strArray[0];
        }

        /// <summary>
        /// Verify query parameters for '$' etc.
        /// </summary>
        internal void VerifyQueryParameters()
        {
            NameValueCollection values = _operationContext.IncomingRequest.UriTemplateMatch.QueryParameters;

            var queryDictionary = new Dictionary<string, string>();

            foreach (string key in values.Keys)
            {
                if (!queryDictionary.ContainsKey(key))
                {
                    queryDictionary.Add(key, values[key]);
                }
                else
                {
                    throw SyncServiceException.CreateBadRequestError(Strings.DuplicateParametersInRequestUri);
                }
            }
        }

        /// <summary>
        /// Get the serialization format for the response based on the value of the HTTP Accept header.
        ///
        /// If $format is not specified then the format comes from the accept header.
        ///
        /// The order in which a response content-type is chosen is based on the
        /// incoming "Accept" header and the types that the service supports.
        /// According to the HTTP/1.1 Header Field Definitions RFC
        /// (http:///www.w3.org/Protocols/rfc2616/rfc2616-sec14.html), an absence of the
        /// Accept header means that the client accepts all response types.
        ///
        /// Media ranges can be overridden by more specific media ranges, for example:
        /// both application/json and application/atom+xml would override */*.
        ///
        /// Depending on the service configuration application/atom+xml would override application/json
        /// if application/atom+xml if the default serialization format, and application/json would
        /// override application/atom+xml if the default serialization format is application/json.
        ///
        /// A client can also send a media range of the following type: application/*, which can be
        /// substituted for application/atom+xml or application/json depending on the service configuration.
        ///
        /// A. If the default configured serialization format is "application/atom+xml"
        ///
        ///  The formats in order of priority are:
        ///     1. application/atom+xml
        ///     2. application/json
        ///     3. application/* or */* substituted with application/atom+xml
        ///
        ///  Examples (order of accept headers doesn't matter):
        ///     "application/*" -> ATOM+XML
        ///     "application/*,application/JSON" -> JSON
        ///     "application/*,application/ATOM+XML" -> ATOM+XML
        ///     "application/*,application/ATOM+XML,application/JSON" -> ATOM+XML
        ///     "application/JSON" -> JSON
        ///     "application/ATOM+XML" -> ATOM+XML
        ///     "application/JSON,application/ATOM+XML" -> ATOM+XML
        ///
        /// B. If the default configured serialization format is "application/json"
        ///
        ///  The formats in order of priority are:
        ///     1. application/json
        ///     2. application/atom+xml
        ///     3. application/* or */* substituted with application/json
        ///
        ///  Examples (order of accept headers doesn't matter):
        ///     "application/*" -> JSON
        ///     "application/*,application/JSON" -> JSON
        ///     "application/*,application/ATOM+XML" -> ATOM+XML
        ///     "application/*,application/ATOM+XML,application/JSON" -> JSON
        ///     "application/JSON" -> JSON
        ///     "application/ATOM+XML" -> ATOM+XML
        ///     "application/JSON,application/ATOM+XML" -> JSON
        ///
        /// Note: headers from firefox need to be trimmed before we make a comparison.In other words the media range
        /// parameter as specified in the above RFC are ignored.
        /// </summary>
        /// <returns>Response serialization format</returns>
        internal SyncSerializationFormat GetOutputSerializationFormat(SyncSerializationFormat defaultSerializationFormat)
        {
            // Read $format from querystring first
            string formatQueryString = QueryStringCollection[SYNC_FORMAT_QUERYKEY];
            if (!String.IsNullOrEmpty(formatQueryString))
            {
                if (0 == String.Compare(formatQueryString.ToLowerInvariant(), "atom", StringComparison.InvariantCultureIgnoreCase))
                {
                    return SyncSerializationFormat.ODataAtom;
                }

                if (0 == String.Compare(formatQueryString.ToLowerInvariant(), "json", StringComparison.InvariantCultureIgnoreCase))
                {
                    return SyncSerializationFormat.ODataJson;
                }
            }
            else if (!String.IsNullOrEmpty(RequestAccept))
            {
                var header =
                    RequestAccept.ToLowerInvariant().Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).Select(h => h.Trim());

                SyncSerializationFormat? outputSerializationFormat = null;

                foreach (string headerString in header)
                {
                    // Media range followed by optional semi-column and accept-params ,
                    string[] headerStringParts = headerString.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);

                    if (0 == headerStringParts.Length)
                    {
                        continue;
                    }

                    // Is this header application/atom+xml
                    if (headerStringParts[0].Equals(CONTENT_TYPE_APPLICATION_ATOM, StringComparison.OrdinalIgnoreCase))
                    {
                        outputSerializationFormat = SyncSerializationFormat.ODataAtom;
                    }

                    // Is this header application/json
                    if (headerStringParts[0].Equals(CONTENT_TYPE_APPLICATION_JSON, StringComparison.OrdinalIgnoreCase))
                    {
                        outputSerializationFormat = SyncSerializationFormat.ODataJson;
                    }

                    // If the default header has been set explicitly then no need to read other headers
                    if (outputSerializationFormat == defaultSerializationFormat)
                    {
                        break;
                    }

                    // Is this header application/* or */*
                    if ((null == outputSerializationFormat) &&
                        (headerStringParts[0].Equals(CONTENT_TYPE_APPLICATION_ANY) || headerStringParts[0].Equals(CONTENT_TYPE_ANY)))
                    {
                        // Do not exit the loop as this can be overwritten by an explicit json or atnm+xml header
                        outputSerializationFormat = defaultSerializationFormat;
                    }
                }

                if (null == outputSerializationFormat)
                {
                    throw SyncServiceException.CreateNotAcceptable(Strings.UnsupportedAcceptHeaderValue);
                }

                return outputSerializationFormat.Value;
            }

            // return the default serialization format.
            return defaultSerializationFormat;
        }

        /// <summary>
        /// Gets the serialization format of the payload in the incoming request based on the content-type header.
        /// According to the HTTP header RFC http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html, the content type
        /// has the following format: 
        ///
        ///         "Content-Type" ":" media-type
        ///         media-type     = type "/" subtype *( ";" parameter )
        /// 
        /// We ignore the media-type parameter.
        /// </summary>
        internal SyncSerializationFormat GetRequestContentSerializationFormat()
        {
            if (String.IsNullOrEmpty(_operationContext.IncomingRequest.ContentType))
            {
                throw SyncServiceException.CreateBadRequestError(Strings.NoContentTypeInHeader);
            }

            // Get type /subtype without the media type parameter
            string[] contentTypeParts = _operationContext.IncomingRequest.ContentType.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);

            if (0 == contentTypeParts.Length) 
            {
                throw SyncServiceException.CreateBadRequestError(Strings.UnsupportedContentType);
            }

            if (contentTypeParts[0].Equals(CONTENT_TYPE_APPLICATION_ATOM, StringComparison.OrdinalIgnoreCase)) 
            {
                return SyncSerializationFormat.ODataAtom;
            }
            
            if (contentTypeParts[0].Equals(CONTENT_TYPE_APPLICATION_JSON, StringComparison.OrdinalIgnoreCase))
            {
                return SyncSerializationFormat.ODataJson;
            }

            throw SyncServiceException.CreateBadRequestError(Strings.UnsupportedContentType);
        }

        /// <summary>
        /// Validate the HTTP Verb for GET/POST and check if the URL matches the allowed format.
        /// </summary>
        internal void ValidateRequestHttpVerbAndSegments()
        {
            if (_operationContext == null)
            {
                throw SyncServiceException.CreateBadRequestError(Strings.NullWebOperationContext);
            }

            // Only allow GET and POST verbs
            if (0 != String.Compare(RequestHttpMethod, "GET", StringComparison.InvariantCultureIgnoreCase) &&
                0 != String.Compare(RequestHttpMethod, "POST", StringComparison.InvariantCultureIgnoreCase))
            {
                SyncTracer.Warning("Request HTTP method is not GET or POST. HTTP Method: {0}", RequestHttpMethod ?? String.Empty);
                throw SyncServiceException.CreateMethodNotAllowed(Strings.UnsupportedHttpMethod, "GET, POST");
            }

            // Check if we have less than 2 relative segments. The maximum segments we can have is 2
            // which is for /scope/operation.
            if (RelativeUriSegments.Length > 2)
            {
                throw SyncServiceException.CreateBadRequestError(Strings.InvalidUrlFormat);
            }
        }

        #endregion

        #region Private Methods

        private static bool MustNotReturnMessageBody(HttpStatusCode statusCode)
        {
            switch (statusCode)
            {
                case HttpStatusCode.NoContent:
                case HttpStatusCode.ResetContent:
                case HttpStatusCode.NotModified:
                    return true;
            }
            return false;
        }

        #endregion
    }
}
