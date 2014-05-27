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
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Resources;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Data.SqlClient;
using Microsoft.Synchronization.Data.SqlServer;

namespace Microsoft.Synchronization.Services
{
    /// <summary>This class contains helper functions for service diagnostics.</summary>
    internal class DiagHelper
    {
        /// <summary>Cached <see cref="XslCompiledTransform" /> instance.</summary>
        private static XslCompiledTransform _compiledTransform;

        private static readonly object _lockObject = new object();

        /// <summary>Gets a value indicating if the request originated from the local machine.</summary>
        private static bool IsLocalRequest
        {
            get
            {
                return (null != HttpContext.Current && HttpContext.Current.Request.IsLocal);
            }
        }

        /// <summary>
        /// Perform diagnostic checks and return an instance of the <see cref="Message" /> class.
        /// </summary>
        /// <param name="configuration">Service configuration</param>
        /// <param name="serviceHost">HttpContext for the service</param>
        /// <returns>Result of the diagnostic checks</returns>
        internal static Message CreateDiagResponseMessage(SyncServiceConfiguration configuration, HttpContextServiceHost serviceHost)
        {
            // Check core msi presence by creating SyncKnowledge instance.
            DiagTestResult syncFxCoreCheck = CheckForSyncFxCore();

            // Establish connnection to SQL Server.
            DiagTestResult sqlConnectionCheck = CheckSqlConnection(configuration);

            // Check database provisioning.
            DiagTestResult dbProvisionedCheck = CheckDbProvisioning(configuration);

            // Check for clientaccesspolicy.xml or crossdomain.xml file.
            //DiagTestResult clientAccessPolicyCheck = CheckForClientAccessPolicy(serviceHost);

            // Check presence of batching folder.
            DiagTestResult batchingFolderExistsResult = CheckBatchingFolderExists(configuration);

            // Check write access to batching folder.
            DiagTestResult writeAccessToBatchingFolder = CheckWriteAccessToBatchingFolder(configuration);

            // Add configuration related information
            var configElement = new XElement("Configuration",
                                                new XElement("ScopeName", HttpUtility.HtmlEncode(configuration.ScopeNames[0])),

                                                new XElement("ConflictResolution", configuration.ConflictResolutionPolicy),

                                                new XElement("SerializationFormat", configuration.SerializationFormat),

                                                new XElement("VerboseEnabled", configuration.UseVerboseErrors),

                                                new XElement("BatchingDirectory", configuration.DownloadBatchSizeInKB == null 
                                                                                ? DiagConstants.BATCHING_NOT_ENABLED
                                                                                : configuration.BatchSpoolDirectory),

                                                new XElement("BatchSize", configuration.DownloadBatchSizeInKB == null 
                                                                                ? DiagConstants.BATCHING_NOT_ENABLED 
                                                                                : configuration.DownloadBatchSizeInKB.Value.ToString()));

            var diagXDocument = new XDocument();
            
            var rootNode = new XElement("root");
            
            diagXDocument.AddFirst(rootNode);
            
            // Add the results to the xml document.
            rootNode.Add(
                        new XElement("SyncFxCore",
                            new XElement("Result") { Value = syncFxCoreCheck.TestResult },
                            new XElement("ErrorInfo") { Value = syncFxCoreCheck.ExceptionDetails ?? String.Empty }),

                        new XElement("SqlConnection",
                            new XElement("Result") { Value = sqlConnectionCheck.TestResult },
                            new XElement("ErrorInfo") { Value = sqlConnectionCheck.ExceptionDetails ?? String.Empty }),

                        new XElement("DbProvisioned", 
                            new XElement("Result") { Value = dbProvisionedCheck.TestResult },
                            new XElement("ErrorInfo") { Value = dbProvisionedCheck.ExceptionDetails ?? String.Empty }),

                        new XElement("BatchingFolderPresent",
                            new XElement("Result") { Value = batchingFolderExistsResult.TestResult },
                            new XElement("ErrorInfo") { Value = batchingFolderExistsResult.ExceptionDetails ?? String.Empty }),

                        new XElement("WriteAccessToBatchingFolder",
                            new XElement("Result") { Value = writeAccessToBatchingFolder.TestResult },
                            new XElement("ErrorInfo") { Value = writeAccessToBatchingFolder.ExceptionDetails ?? String.Empty }),

                        //new XElement("PolicyFiles",
                        //    new XElement("Result") { Value = clientAccessPolicyCheck.TestResult },
                        //    new XElement("ErrorInfo") { Value = clientAccessPolicyCheck.ExceptionDetails ?? String.Empty }),

                        // Add the configuration node.
                        new XElement(configElement));

            // Create and cache the XslCompiledTransform if it is already not in the cache.
            ConfigureXslCompiledTransform();

            Message message;

            using (XmlReader diagXmlReader = diagXDocument.CreateReader())
            {
                var document = new XPathDocument(diagXmlReader);

                using (var writer = new StringWriter())
                {
                    // Transform the xml document into HTML.
                    _compiledTransform.Transform(document, null, writer);

                    // Create an return an instance of the Message class.
                    message = Message.CreateMessage(MessageVersion.None, String.Empty, XDocument.Parse(writer.ToString()).CreateReader());

                    message.Properties.Add(WebBodyFormatMessageProperty.Name, new WebBodyFormatMessageProperty(WebContentFormat.Xml));

                    var property = new HttpResponseMessageProperty { StatusCode = HttpStatusCode.OK };

                    property.Headers.Add(HttpResponseHeader.ContentType, SyncServiceConstants.CONTENT_TYPE_HTML);

                    message.Properties.Add(HttpResponseMessageProperty.Name, property);
                }
            }

            return message;
        }

        /// <summary>Check if the database is provisioned and has a template/scope that the service is configured for.</summary>
        /// <param name="configuration">Service configuration</param>
        /// <returns>Result of the diagnostic check</returns>
        private static DiagTestResult CheckDbProvisioning(SyncServiceConfiguration configuration)
        {
            Debug.Assert(configuration.ScopeNames.Count > 0, "configuration.ScopeNames.Count > 0");

            var result = new DiagTestResult();

            try
            {
                using (var connection = new SqlConnection(configuration.ServerConnectionString))
                {
                    var provisioning = new SqlSyncScopeProvisioning(connection);

                    // Set the ObjectSchema property. Without this, the TemplateExists and ScopeExists method
                    // always return false if the sync objects are provisioned in a non-dbo schema.
                    if (!String.IsNullOrEmpty(configuration.SyncObjectSchema))
                    {
                        provisioning.ObjectSchema = configuration.SyncObjectSchema;
                    }

                    // Current implementation only supports 1 scope per service head.
                    string scopeName = configuration.ScopeNames[0];
                    
                    if (provisioning.ScopeExists(scopeName) || provisioning.TemplateExists(scopeName))
                    {
                        result.TestResult = DiagConstants.SUCCESS;
                    }
                    else
                    {
                        result.TestResult = DiagConstants.TEMPLATE_OR_SCOPE_DOES_NOT_EXIST;
                    }
                }
            }
            catch (Exception e)
            {
                result.TestResult = DiagConstants.UNKNOWN_ERROR;

                AddExceptionInfo(e, result);
            }

            return result;
        }

        /// <summary>Check whether a connection can be opened successfully to the database.</summary>
        /// <param name="configuration">Service configuration</param>
        /// <returns>Result of the diagnostic check</returns>
        private static DiagTestResult CheckSqlConnection(SyncServiceConfiguration configuration)
        {
            var result = new DiagTestResult();

            try
            {
                new SqlConnectionStringBuilder(configuration.ServerConnectionString);
            }
            catch (KeyNotFoundException keyNotFoundException)
            {
                result.TestResult = DiagConstants.INVALID_SQL_CONNECTION_STRING;

                if (IsLocalRequest)
                {
                    result.ExceptionDetails = WebUtil.GetExceptionMessage(keyNotFoundException);
                }
            }
            catch (FormatException formatException)
            {
                result.TestResult = DiagConstants.INVALID_SQL_CONNECTION_STRING;

                if (IsLocalRequest)
                {
                    result.ExceptionDetails = WebUtil.GetExceptionMessage(formatException);
                }
            }
            catch (ArgumentException argumentException)
            {
                result.TestResult = DiagConstants.INVALID_SQL_CONNECTION_STRING;

                if (IsLocalRequest)
                {
                    result.ExceptionDetails = WebUtil.GetExceptionMessage(argumentException);
                }
            }
            catch (Exception e)
            {
                result.TestResult = DiagConstants.UNKNOWN_ERROR;

                if (IsLocalRequest)
                {
                    result.ExceptionDetails = WebUtil.GetExceptionMessage(e);
                }
            }

            if (result.TestResult == DiagConstants.NOT_DETERMINED)
            {
                try
                {
                    using (var connection = new SqlConnection(configuration.ServerConnectionString))
                    {
                        connection.Open();
                    }

                    result.TestResult = DiagConstants.SUCCESS;
                }
                catch (InvalidOperationException invalidOperationException)
                {
                    result.TestResult = DiagConstants.ERROR_OPENING_SQL_CONNECTION;

                    if (IsLocalRequest)
                    {
                        result.ExceptionDetails = WebUtil.GetExceptionMessage(invalidOperationException);
                    }
                }
                catch (SqlException sqlException) 
                {
                    result.TestResult = DiagConstants.ERROR_OPENING_SQL_CONNECTION;

                    if (IsLocalRequest)
                    {
                        result.ExceptionDetails = WebUtil.GetExceptionMessage(sqlException);
                    }
                }
                catch (ArgumentException argumentException)
                {
                    result.TestResult = DiagConstants.ERROR_OPENING_SQL_CONNECTION;

                    if (IsLocalRequest)
                    {
                        result.ExceptionDetails = WebUtil.GetExceptionMessage(argumentException);
                    }
                }
                catch (Exception e)
                {
                    result.TestResult = DiagConstants.UNKNOWN_ERROR;

                    if (IsLocalRequest)
                    {
                        result.ExceptionDetails = WebUtil.GetExceptionMessage(e);
                    }
                }
            }

            return result;
        }

        /// <summary>Check whether the SyncFx core assemblies are available.</summary>
        /// <returns>Result of the diagnostic check</returns>
        private static DiagTestResult CheckForSyncFxCore()
        {
            var result = new DiagTestResult();

            try
            {
                // This overload of SyncKnowledge will fail if the native SyncFx dll's are not registered.
                new SyncKnowledge(new SyncIdFormatGroup(), new SyncId(Guid.NewGuid()), 0);

                result.TestResult = DiagConstants.SUCCESS;
            }
            catch (SyncException syncException)
            {
                result.TestResult = DiagConstants.SYNC_FX_CORE_ERROR;

                AddExceptionInfo(syncException, result);
            }
            catch (Exception exception)
            {
                result.TestResult = DiagConstants.UNKNOWN_ERROR;

                AddExceptionInfo(exception, result);
            }

            return result;
        }

        /// <summary>Check whether the website root has a ClientAccessPolicy.xml or CrossDomain.xml file.</summary>
        /// <returns>Result of the diagnostic check</returns>
        private static DiagTestResult CheckForClientAccessPolicy(HttpContextServiceHost serviceHost)
        {
            Debug.Assert(WebOperationContext.Current != null, "WebOperationContext.Current != null");

            var result = new DiagTestResult();

            try
            {
                Uri requestUri = WebOperationContext.Current.IncomingRequest.UriTemplateMatch.RequestUri;

                string requestHost = !String.IsNullOrEmpty(serviceHost.HostHeader) ? serviceHost.HostHeader : requestUri.Authority;
                
                // create the webroot string
                string webRoot = String.Format("{0}://{1}/", requestUri.Scheme, requestHost);

                var webRequest = (HttpWebRequest)WebRequest.Create(new Uri(webRoot + DiagConstants.CLIENT_ACCESS_POLICY_FILENAME));

                webRequest.CachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);

                // First, attempt to retrieve the clientaccesspolicy.xml file.

                HttpWebResponse response = null;

                try
                {
                    response = (HttpWebResponse)webRequest.GetResponse();

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        result.TestResult = DiagConstants.FOUND_CLIENT_ACCESS_POLICY;
                    }
                }
                catch (WebException)
                {
                    // Continue and ignore this error.
                }

                if (result.TestResult != DiagConstants.FOUND_CLIENT_ACCESS_POLICY)
                {
                    // Attempt to retrieve the crossdomain.xml file.
                    webRequest = (HttpWebRequest) WebRequest.Create(new Uri(webRoot + DiagConstants.CROSS_DOMAIN_FILENAME));

                    webRequest.CachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);

                    try
                    {
                        response = (HttpWebResponse)webRequest.GetResponse();

                        result.TestResult = response.StatusCode == HttpStatusCode.OK
                                     ? DiagConstants.FOUND_CROSSDOMAIN_POLICY_FILE
                                     : DiagConstants.CLIENT_ACCESS_POLICY_OR_CROSS_DOMAIN_NOT_FOUND;
                    }
                    catch (WebException exception)
                    {
                        // Could not find both clientaccesspolicy.xml and crossdomain.xml.
                        result.TestResult = DiagConstants.CLIENT_ACCESS_POLICY_OR_CROSS_DOMAIN_NOT_FOUND;

                        AddExceptionInfo(exception, result);
                    }
                }
            }
            catch (Exception exception)
            {
                result.TestResult = DiagConstants.UNKNOWN_ERROR;  
 
                AddExceptionInfo(exception, result);
            }

            return result;

        }

        /// <summary>Check whether the service has write permissions to the batch folder.</summary>
        /// <param name="configuration">Service configuration.</param>
        /// <returns>Result of the diagnostic check</returns>
        private static DiagTestResult CheckWriteAccessToBatchingFolder(SyncServiceConfiguration configuration)
        {
            var result = new DiagTestResult();

            try
            {
                if (configuration.IsBatchingEnabled)
                {
                    if (Directory.Exists(configuration.BatchSpoolDirectory))
                    {
                        try
                        {
                            string path = Path.Combine(configuration.BatchSpoolDirectory, Guid.NewGuid().ToString());

                            // attempt to create a file
                            using (File.Create(path)) { }

                            // delete the file.
                            File.Delete(path);

                            result.TestResult = DiagConstants.SUCCESS;
                        }
                        catch (UnauthorizedAccessException unauthorizedAccessException)
                        {
                            result.TestResult = DiagConstants.INSUFFICIENT_PERMISSIONS;

                            AddExceptionInfo(unauthorizedAccessException, result);
                        }
                        catch (PathTooLongException pathTooLongExceptionh)
                        {
                            result.TestResult = DiagConstants.PATH_TOO_LONG;

                            AddExceptionInfo(pathTooLongExceptionh, result);
                        }
                        catch (IOException ioException)
                        {
                            result.TestResult = DiagConstants.IO_ERROR;

                            AddExceptionInfo(ioException, result);
                        }
                    }
                    else
                    {
                        result.TestResult = DiagConstants.DIRECTORY_NOT_FOUND;
                    }
                }
                else
                {
                    result.TestResult = DiagConstants.BATCHING_NOT_ENABLED;
                }
            }
            catch (DirectoryNotFoundException directoryNotFoundException)
            {
                result.TestResult = DiagConstants.DIRECTORY_NOT_FOUND;

                AddExceptionInfo(directoryNotFoundException, result);
            }   
            catch (Exception exception)
            {
                result.TestResult = DiagConstants.UNKNOWN_ERROR;

                AddExceptionInfo(exception, result);
            }

            return result;
        }

        /// <summary>Check whether the batch folder exists.</summary>
        /// <param name="configuration">Service configuration.</param>
        /// <returns>Result of the diagnostic check</returns>
        private static DiagTestResult CheckBatchingFolderExists(SyncServiceConfiguration configuration)
        {
            var result = new DiagTestResult();

            try
            {
                if (configuration.IsBatchingEnabled)
                {
                    result.TestResult = Directory.Exists(configuration.BatchSpoolDirectory) ? DiagConstants.SUCCESS : DiagConstants.DIRECTORY_NOT_FOUND;
                }
                else
                {
                    result.TestResult = DiagConstants.BATCHING_NOT_ENABLED;
                }
            }
            catch (DirectoryNotFoundException directoryNotFoundException)
            {
                result.TestResult = DiagConstants.DIRECTORY_NOT_FOUND;

                AddExceptionInfo(directoryNotFoundException, result);
            }
            catch (Exception exception)
            {
                result.TestResult = DiagConstants.UNKNOWN_ERROR;

                AddExceptionInfo(exception, result);
            }

            return result;
        }

        /// <summary>Create and cache the compiled transform if was not already created earlier.</summary>
        private static void ConfigureXslCompiledTransform()
        {
            if (null == _compiledTransform)
            {
                lock (_lockObject)
                {
                    if (null == _compiledTransform)
                    {
                        _compiledTransform = new XslCompiledTransform();

                        XDocument document = XDocument.Parse(diagxslt.diag);
                        using (var readerStream = document.CreateReader())
                        {
                            _compiledTransform.Load(readerStream);
                        }
                    }
                }
            }
        }

        private static void AddExceptionInfo(Exception e, DiagTestResult result)
        {
            if (IsLocalRequest)
            {
                result.ExceptionDetails = WebUtil.GetExceptionMessage(e);
            }
        }

        /// <summary>Constants used in the diagnostic logic.</summary>
        private static class DiagConstants
        {
            internal const string CLIENT_ACCESS_POLICY_FILENAME = "clientaccesspolicy.xml";
            internal const string CROSS_DOMAIN_FILENAME = "crossdomain.xml";
            internal const string XSLT_RESOURCE_NAME = "Microsoft.Synchronization.Services.diagxslt";

            internal const string TEMPLATE_OR_SCOPE_DOES_NOT_EXIST = "TEMPLATE_OR_SCOPE_DOES_NOT_EXIST";
            internal const string DIRECTORY_NOT_FOUND = "DIRECTORY_NOT_FOUND";
            internal const string UNKNOWN_ERROR = "UNKNOWN_ERROR";
            internal const string INSUFFICIENT_PERMISSIONS = "INSUFFICIENT_PERMISSIONS";
            internal const string PATH_TOO_LONG = "PATH_TOO_LONG";
            internal const string IO_ERROR = "IO_ERROR";
            internal const string CLIENT_ACCESS_POLICY_OR_CROSS_DOMAIN_NOT_FOUND = "CLIENT_ACCESS_POLICY_OR_CROSS_DOMAIN_NOT_FOUND";
            internal const string FOUND_CLIENT_ACCESS_POLICY = "FOUND_CLIENT_ACCESS_POLICY";
            internal const string FOUND_CROSSDOMAIN_POLICY_FILE = "FOUND_CROSSDOMAIN_POLICY_FILE";
            internal const string SYNC_FX_CORE_ERROR = "SYNC_FX_CORE_ERROR";
            internal const string INVALID_SQL_CONNECTION_STRING = "INVALID_SQL_CONNECTION_STRING";
            internal const string ERROR_OPENING_SQL_CONNECTION = "ERROR_OPENING_SQL_CONNECTION";
            internal const string BATCHING_NOT_ENABLED = "BATCHING_NOT_ENABLED";
            internal const string SUCCESS = "SUCCESS";
            internal const string NOT_DETERMINED = "NOT_DETERMINED";
        }

        /// <summary>Contains the result of a single diagnostic test and any exception details if errors occur.</summary>
        private class DiagTestResult
        {
            public string TestResult { get; set; }
            public string ExceptionDetails { get; set; }

            public DiagTestResult()
            {
                TestResult = DiagConstants.NOT_DETERMINED;
                ExceptionDetails = String.Empty;
            }
        }
    }
}
