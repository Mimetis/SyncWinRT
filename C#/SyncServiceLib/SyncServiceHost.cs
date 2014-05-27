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
using System.ServiceModel.Web;

namespace Microsoft.Synchronization.Services
{
    /// <summary>
    /// WebServiceHost is a ServiceHost derived class that compliments the Windows Communication Foundation (WCF) Web programming model.
    /// 
    /// If WebServiceHost finds no endpoints in the service description, it automatically creates a default endpoint 
    /// at the service's base address for HTTP and HTTPS base addresses. 
    /// It does not create an endpoint automatically if the user has configured an endpoint explicitly at the base address. 
    /// WebServiceHost automatically configures the endpoint's binding to work with the associated 
    /// Internet Information Services (IIS) security settings when used in a secure virtual directory. 
    /// 
    /// When creating a default HTTP endpoint, the WebServiceHost also disables the HTTP Help page 
    /// and the Web Services Description Language (WSDL) GET functionality so the metadata endpoint 
    /// does not interfere with the default HTTP endpoint.
    /// 
    /// In addition, the WebServiceHost class adds the WebHttpBehavior to all endpoints that do not already 
    /// have the behavior and that have a WebMessageEncodingElement. 
    /// If all the operations on the service have either empty HTTP request bodies or deal with the 
    /// HTTP request body as a stream, then the WebServiceHost automatically 
    /// configures the appropriate content type mapper for the binding.
    /// 
    /// See http://msdn.microsoft.com/en-us/library/system.servicemodel.web.webservicehost.aspx for more information.
    /// </summary>
    public class SyncServiceHost : WebServiceHost
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="baseAddresses"></param>
        public SyncServiceHost(Type serviceType, Uri[] baseAddresses)
            : base(serviceType, baseAddresses)
        {
        }
    }
}
