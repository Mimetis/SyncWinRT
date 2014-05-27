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

using System.Diagnostics;
using System.ServiceModel.Channels;
using System.Xml.Linq;
using Microsoft.Synchronization.Services.Formatters;

namespace Microsoft.Synchronization.Services
{
    /// <summary>
    /// Handler for processing the GetScopes request ($syncscopes).
    /// </summary>
    internal class SyncScopesRequestProcessor : IRequestProcessor
    {
        #region Private Members

        private readonly SyncServiceConfiguration _configuration;
        private Request _incomingRequest;
        private readonly XNamespace _w3AppNamespaceUri = XNamespace.Get("http://www.w3.org/2007/app");

        #endregion

        #region Constructor

        public SyncScopesRequestProcessor(SyncServiceConfiguration configuration)
        {
            _configuration = configuration;
        }

        #endregion

        #region IRequestProcessor Implementation

        /// <summary>
        /// Process the GetScopes ($syncscopes) request and return the xml description as per the sync protocol specification.
        /// </summary>
        /// <param name="incomingRequest">incoming request object.</param>
        /// <returns>WCF Message object that contains the output xml.</returns>
        public Message ProcessRequest(Request incomingRequest)
        {
            Debug.Assert(null != _configuration.ScopeNames);
            Debug.Assert(_configuration.ScopeNames.Count > 0);
            Debug.Assert(null != incomingRequest);

            _incomingRequest = incomingRequest;

            // We currently support only 1 scope, so read it.
            XDocument document = GetScopeListInfo();

            return WebUtil.CreateResponseMessage(document);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Get the XDocument that contains scope list information as described by the $syncscopes specification.
        /// </summary>
        /// <returns>XDocument object that has the scopes list as per the $syncscopes specification.</returns>
        private XDocument GetScopeListInfo()
        {
            // We only have 1 scope for now.
            //Note: Currently this is read from the service configuration and * is not allowed anymore for the SetEnableScope method.
            string scopeName = _configuration.ScopeNames[0];

            // The absolute uri includes $syncscopes since the UriTemplate match is set to *. We need to explicitly
            // remove $syncscopes to get the service url.
            string serviceBaseUrl = _incomingRequest.ServiceHost.RequestUri.AbsoluteUri.ToLower().Replace(SyncServiceConstants.SYNC_SCOPES_URL_SEGMENT, string.Empty);

            var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));

            // This will add the <service> node
            // example:
            // <service xml:base="http://localhost/service.svc/" xmlns:atom="http://www.w3.org/2005/Atom" xmlns="http://www.w3.org/2007/app">
            var root = new XElement("service",
                                    new XAttribute(XNamespace.Xml + "base", XNamespace.Get(serviceBaseUrl)),
                                    new XAttribute(XNamespace.Xmlns + "atom", FormatterConstants.AtomXmlNamespace),
                                    new XAttribute(FormatterConstants.AtomPubXmlNsPrefix, _w3AppNamespaceUri)
                                    );

            // Add <workspace> node
            var workspaceNode = new XElement(_w3AppNamespaceUri + "workspace");

            root.Add(workspaceNode);

            // Add atom:title element
            // example: <atom:title>SyncScopes</atom:title> 
            workspaceNode.Add(new XElement(FormatterConstants.AtomXmlNamespace + "title", "SyncScopes"));
              
            // Add collection node.
            // example: <collection href="ScopeName">
            var collectionNode = new XElement(_w3AppNamespaceUri + "collection", new XAttribute("href", scopeName));

            // add title for scope
            // example: <atom:title>ScopeName</atom:title>
            collectionNode.Add(new XElement(FormatterConstants.AtomXmlNamespace + "title", scopeName));

            // add the entry to the root.
            workspaceNode.Add(collectionNode);

            // add the root to the document.
            document.Add(root);

            return document;
        }

        #endregion
    }
}
