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
using System.Runtime.Serialization;

namespace Microsoft.Synchronization.Services
{
    /// <summary>
    /// The exception that is thrown when an error occurs while processing
    /// a web sync service request.
    /// </summary>
    /// <remarks>
    /// The SyncServiceException is thrown to indicate an error during
    /// request processing, specifying the appropriate response for
    /// the request.
    /// 
    /// RFC2616 (http://www.ietf.org/rfc/rfc2616.txt) about the status code values:
    ///     1xx: Informational  - Request received, continuing process
    ///     "100"  ; Section 10.1.1: Continue
    ///     "101"  ; Section 10.1.2: Switching Protocols
    ///     
    ///     2xx: Success        - The action was successfully received, understood, and accepted
    ///     "200"  ; Section 10.2.1: OK
    ///     "201"  ; Section 10.2.2: Created
    ///     "202"  ; Section 10.2.3: Accepted
    ///     "203"  ; Section 10.2.4: Non-Authoritative Information
    ///     "204"  ; Section 10.2.5: No Content
    ///     "205"  ; Section 10.2.6: Reset Content
    ///     "206"  ; Section 10.2.7: Partial Content
    ///     
    ///     3xx: Redirection    - Further action must be taken in order to complete the request
    ///     "300"  ; Section 10.3.1: Multiple Choices
    ///     "301"  ; Section 10.3.2: Moved Permanently
    ///     "302"  ; Section 10.3.3: Found
    ///     "303"  ; Section 10.3.4: See Other
    ///     "304"  ; Section 10.3.5: Not Modified
    ///     "305"  ; Section 10.3.6: Use Proxy
    ///     "307"  ; Section 10.3.8: Temporary Redirect
    ///     
    ///     4xx: Client Error   - The request contains bad syntax or cannot be fulfilled
    ///     "400"  ; Section 10.4.1: Bad Request
    ///     "401"  ; Section 10.4.2: Unauthorized
    ///     "402"  ; Section 10.4.3: Payment Required
    ///     "403"  ; Section 10.4.4: Forbidden
    ///     "404"  ; Section 10.4.5: Not Found
    ///     "405"  ; Section 10.4.6: Method Not Allowed
    ///     "406"  ; Section 10.4.7: Not Acceptable
    ///     "407"  ; Section 10.4.8: Proxy Authentication Required
    ///     "408"  ; Section 10.4.9: Request Time-out
    ///     "409"  ; Section 10.4.10: Conflict
    ///     "410"  ; Section 10.4.11: Gone
    ///     "411"  ; Section 10.4.12: Length Required
    ///     "412"  ; Section 10.4.13: Precondition Failed
    ///     "413"  ; Section 10.4.14: Request Entity Too Large
    ///     "414"  ; Section 10.4.15: Request-URI Too Large
    ///     "415"  ; Section 10.4.16: Unsupported Media Type
    ///     "416"  ; Section 10.4.17: Requested range not satisfiable
    ///     "417"  ; Section 10.4.18: Expectation Failed
    ///     
    ///     5xx: Server Error   - The server failed to fulfill an apparently valid request
    ///     "500"  ; Section 10.5.1: Internal Server Error
    ///     "501"  ; Section 10.5.2: Not Implemented
    ///     "502"  ; Section 10.5.3: Bad Gateway
    ///     "503"  ; Section 10.5.4: Service Unavailable
    ///     "504"  ; Section 10.5.5: Gateway Time-out
    ///     "505"  ; Section 10.5.6: HTTP Version not supported
    /// </remarks>
    [Serializable]
    [DebuggerDisplay("{StatusCode}: {Message}")]
    public sealed class SyncServiceException : InvalidOperationException
    {
        #region Constants

        private const string INTERNAL_SERVER_ERROR = "Internal Server Error";
        private const string ERROR_CODE = "Forbidden";
        private const string RESOURCE_NOT_FOUND = "ResourceNotFound";
        private const string SYNTAX_ERROR = "Syntax Error";

        #endregion

        #region Private Members

        private readonly string _errorCode;
        private string _responseAllowHeader;
        private readonly int _statusCode;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the SyncServiceException class.
        /// </summary>
        /// <remarks>
        /// The Message property is initialized to a system-supplied message 
        /// that describes the error. The StatusCode property is set to 500
        /// (Internal Server Error).
        /// </remarks>
        public SyncServiceException()
            : this(ResponseHttpStatusCode.InternalServerError, INTERNAL_SERVER_ERROR)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DataServiceException class.
        /// </summary>
        /// <param name="message">Plain text error message for this exception.</param>
        /// <remarks>
        /// The StatusCode property is set to 500 (Internal Server Error).
        /// </remarks>
        public SyncServiceException(string message) 
            : this(ResponseHttpStatusCode.InternalServerError, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SyncServiceException class.
        /// </summary>
        /// <param name="message">Plain text error message for this exception.</param>
        /// <param name="innerException">Exception that caused this exception to be thrown.</param>
        /// <remarks>
        /// The StatusCode property is set to 500 (Internal Server Error).
        /// </remarks>
        public SyncServiceException(string message, Exception innerException)
            : this(ResponseHttpStatusCode.InternalServerError, null /*errorCode*/, message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SyncServiceException class.
        /// </summary>
        /// <param name="statusCode">HTTP response status code for this exception.</param>
        /// <param name="message">Plain text error message for this exception.</param>
        public SyncServiceException(int statusCode, string message)
            : this(statusCode, null /*errorCode*/, message, null /*innerException*/)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SyncServiceException class.
        /// </summary>
        /// <param name="statusCode">HTTP response status code for this exception.</param>
        /// <param name="errorCode">Error code to be used in payloads.</param>
        /// <param name="message">Plain text error message for this exception.</param>
        /// <param name="innerException">Exception that caused this exception to be thrown.</param>
        internal SyncServiceException(int statusCode, string errorCode, string message, Exception innerException)
            : base(message, innerException)
        {
            _errorCode = errorCode ?? string.Empty;
            _statusCode = statusCode;
        }

        #endregion

        #region Properties

        /// <summary>Error code to be used in payloads.</summary>
        public string ErrorCode
        {
            get { return _errorCode;  }
        }

        /// <summary>'Allow' response for header.</summary>
        internal string ResponseAllowHeader
        {
            get { return _responseAllowHeader; }
        }

        /// <summary>Response status code for this exception.</summary>
        public int StatusCode
        {
            get { return _statusCode; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the <see cref="System.Runtime.Serialization.SerializationInfo" /> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("errorCode", _errorCode);
            info.AddValue("responseAllowHeader", _responseAllowHeader);
            info.AddValue("statusCode", _statusCode);

            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Creates a new SyncServiceException to indicate InternalServerError (HTTP 500) error.
        /// </summary>
        /// <param name="errorMessage">Plain text error message for this exception.</param>
        /// <returns>A new SyncServiceException to indicate an internal server error.</returns>
        public static SyncServiceException CreateInternalServerError(string errorMessage)
        {
            return new SyncServiceException(ResponseHttpStatusCode.InternalServerError, errorMessage);
        }

        /// <summary>
        /// Creates a new exception to indicate InternalServerError (HTTP 500) error.
        /// </summary>
        /// <param name="errorMessage">Plain text error message for this exception.</param>
        /// <param name="innerException">Inner Exception.</param>
        /// <returns>A new SyncServiceException to indicate an internal server error.</returns>
        public static SyncServiceException CreateInternalServerError(string errorMessage, Exception innerException)
        {
            return new SyncServiceException(ResponseHttpStatusCode.InternalServerError, null /*errorCode*/, errorMessage, innerException);
        }

        /// <summary>
        /// Creates a new SyncServiceException to indicate BadRequest (HTTP 400) error.
        /// </summary>
        /// <param name="errorMessage">Plain text error message for this exception.</param>
        /// <returns>A new SyncServiceException to indicate a bad request error.</returns>
        public static SyncServiceException CreateBadRequestError(string errorMessage)
        {
            return new SyncServiceException(ResponseHttpStatusCode.BadRequest, errorMessage);
        }

        /// <summary>
        /// Creates a new SyncServiceException to indicate BadRequest (HTTP 400) error.
        /// </summary>
        /// <param name="errorMessage">Plain text error message for this exception.</param>
        /// <param name="innerException">Inner Exception.</param>
        /// <returns>A new SyncServiceException to indicate a bad request error.</returns>
        public static SyncServiceException CreateBadRequestError(string errorMessage, Exception innerException)
        {
            return new SyncServiceException(ResponseHttpStatusCode.BadRequest, null, errorMessage, innerException);
        }

        /// <summary>Creates a new "Forbidden" (HTTP 403) exception.</summary>
        /// <returns>A new SyncServiceException to indicate that the request is forbidden.</returns>
        public static SyncServiceException CreateForbidden()
        {
            return new SyncServiceException(ResponseHttpStatusCode.Forbidden, ERROR_CODE);
        }

        /// <summary>Creates a new "Method Not Allowed" exception.</summary>
        /// <param name="errorMessage">Plain text error message for this exception.</param>
        /// <param name="allow">String value for 'Allow' header in response.</param>
        /// <returns>A new SyncServiceException to indicate the requested method is not allowed on the response.</returns>
        public static SyncServiceException CreateMethodNotAllowed(string errorMessage, string allow)
        {
            var exception = new SyncServiceException(ResponseHttpStatusCode.MethodNotAllowed, errorMessage) { _responseAllowHeader = allow };
            return exception;
        }

        /// <summary>
        /// Creates a new exception to indicate MethodNotImplemented error.
        /// </summary>
        /// <param name="errorMessage">Plain text error message for this exception.</param>
        /// <returns>A new SyncServiceException to indicate a MethodNotImplemented error.</returns>
        public static SyncServiceException CreateMethodNotImplemented(string errorMessage)
        {
            return new SyncServiceException(ResponseHttpStatusCode.MethodNotImplemented, errorMessage);
        }

        /// <summary>Creates a new "Resource Not Found" (HTTP 404) exception.</summary>
        /// <returns>A new SyncServiceException to indicate the requested resource cannot be found.</returns>
        public static SyncServiceException CreateResourceNotFound()
        {
            return new SyncServiceException(ResponseHttpStatusCode.ResourceNotFound, RESOURCE_NOT_FOUND);
        }

        /// <summary>Creates a new "Resource Not Found" (HTTP 404) exception.</summary>
        /// <param name="errorMessage">Plain text error message for this exception.</param>
        /// <returns>A new SyncServiceException to indicate the requested resource cannot be found.</returns>
        public static SyncServiceException CreateResourceNotFound(string errorMessage)
        {
            return new SyncServiceException(ResponseHttpStatusCode.ResourceNotFound, errorMessage);
        }

        /// <summary>Creates a new "Not Acceptable" (HTTP 406) exception.</summary>
        /// <param name="errorMessage">Plain text error message for this exception.</param>
        /// <returns>A new SyncServiceException to indicate a NotAcceptable error.</returns>
        public static SyncServiceException CreateNotAcceptable(string errorMessage)
        {
            return new SyncServiceException(ResponseHttpStatusCode.NotAcceptable, errorMessage);
        }

        /// <summary>Creates a new "Unsupported Media Type" (HTTP 415) exception.</summary>
        /// <param name="errorMessage">Plain text error message for this exception.</param>
        /// <returns>A new SyncServiceException to indicate a UnsupportedMediaType error.</returns>
        public static SyncServiceException CreateUnsupportedMediaType(string errorMessage)
        {
            return new SyncServiceException(ResponseHttpStatusCode.UnsupportedMediaType, errorMessage);
        }

        /// <summary>
        /// Creates a new SyncServiceException to indicate Precondition error.
        /// </summary>
        /// <param name="errorMessage">Plain text error message for this exception.</param>
        /// <returns>A new SyncServiceException to indicate a Precondition failed error.</returns>
        public static SyncServiceException CreatePreConditionFailed(string errorMessage)
        {
            return new SyncServiceException(ResponseHttpStatusCode.PreConditionFailed, errorMessage);
        }

        /// <summary>Creates a new SyncServiceException to indicate a syntax error.</summary>
        /// <returns>A new SyncServiceException to indicate a syntax error.</returns>
        public static SyncServiceException CreateSyntaxError()
        {
            return CreateSyntaxError(SYNTAX_ERROR);
        }

        /// <summary>Creates a new SyncServiceException to indicate a syntax error.</summary>
        /// <param name="errorMessage">Plain text error message for this exception.</param>
        /// <returns>A new SyncServiceException to indicate a syntax error.</returns>
        public static SyncServiceException CreateSyntaxError(string errorMessage)
        {
            return CreateBadRequestError(errorMessage);
        }

        #endregion
    }
}
