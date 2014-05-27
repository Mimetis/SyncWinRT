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

using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Channels;
using System.IO;

namespace Microsoft.Synchronization.Services
{
    /// <summary>
    /// Contains the service contract that is implemented by the SyncService.
    /// </summary>
    [ServiceContract]
    internal interface IRequestHandler
    {
        /// <summary>
        /// This is a global handler for all incoming requests.
        /// 
        /// Usages: 
        /// 1. .svc code behind files need to inherit from this class to enable sync methods.
        /// 2. No endpoints need to be added explicitly to the to the web.config
        /// 
        /// Note: For more details on how service can accept arbitary data. See 
        /// http://msdn.microsoft.com/en-us/library/cc656724.aspx 
        /// </summary>
        /// <param name="messageBody">Message body</param>
        /// <returns>The response <see cref="Message"/>.</returns>
        [WebInvoke(UriTemplate = "*", Method = "*"), OperationContract]
        Message ProcessRequestForMessage(Stream messageBody);

        /// <summary>
        /// This method is invoked when a GET request is made to the service root.
        /// We will redirect this request to the /$syncscopes URL which will return a list of scopes.
        /// Without this, the service returns a missing operation error.
        /// </summary>
        [WebInvoke(UriTemplate = "/", Method = "GET"), OperationContract]
        void ProcessRequestToServiceRoot();

        /// <summary>Processes the diagnostics page request.</summary>
        /// <returns>The response <see cref="Message"/>.</returns>
        [WebInvoke(UriTemplate = "$diag", Method = "GET"), OperationContract]
        Message ProcessRequestForDiagnostics();
    }
}
