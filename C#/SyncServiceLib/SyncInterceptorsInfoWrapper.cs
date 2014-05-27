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
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace Microsoft.Synchronization.Services
{
    /// <summary>
    /// Utility cache class that holds information on the list of configured SyncOperationInterceptor for a scope.
    /// </summary>
    internal class SyncInterceptorsInfoWrapper
    {
        string _scopeName;

        MethodInfo _downloadRequestInterceptor;
        MethodInfo _downloadResponseInterceptor;
        MethodInfo _uploadRequestInterceptor;
        MethodInfo _uploadResponseInterceptor;
        MethodInfo _conflictInterceptor;

        Dictionary<Type, MethodInfo> _downloadTypedResponseInterceptors;
        Dictionary<Type, MethodInfo> _uploadTypedRequestInterceptors;
        Dictionary<Type, MethodInfo> _uploadTypedResponseInterceptors;
        Dictionary<Type, MethodInfo> _conflictTypedInterceptors;

        internal SyncInterceptorsInfoWrapper(string scopeName)
        {
            this._scopeName = scopeName;
            _downloadTypedResponseInterceptors = new Dictionary<Type, MethodInfo>();
            _uploadTypedRequestInterceptors = new Dictionary<Type, MethodInfo>();
            _uploadTypedResponseInterceptors = new Dictionary<Type, MethodInfo>();
            _conflictTypedInterceptors = new Dictionary<Type, MethodInfo>();

            _downloadRequestInterceptor = _downloadResponseInterceptor =
            _uploadRequestInterceptor = _uploadResponseInterceptor = _conflictInterceptor = null;
        }

        internal String ScopeName
        {
            get { return this._scopeName; }
        }

        internal MethodInfo DownloadRequestInterceptor
        {
            get { return this._downloadRequestInterceptor; }
        }

        internal MethodInfo DownloadResponseInterceptor
        {
            get { return this._downloadResponseInterceptor; }
        }

        internal MethodInfo UploadRequestInterceptor
        {
            get { return this._uploadRequestInterceptor; }
        }

        internal MethodInfo UploadResponseInterceptor
        {
            get { return this._uploadResponseInterceptor; }
        }

        internal MethodInfo ConflictInterceptor
        {
            get { return this._conflictInterceptor; }
        }

        /// <summary>
        /// Adds the MethodInfo signature for the specified interceptor type and operation. This
        /// method also ensures that the interceptor is valid and is not a duplicate entry before 
        /// adding it.
        /// </summary>
        /// <param name="attr">Interceptor Attribute</param>
        /// <param name="info">MethodInfo signature</param>
        /// <param name="className">ClassName for error messages</param>
        internal void AddInterceptor(SyncInterceptorAttribute attr, MethodInfo info, string className)
        {
            if (attr is SyncRequestInterceptorAttribute)
            {
                // Its a Request interceptor. Check for Operation
                if ((attr.Operation & SyncOperations.Download) == SyncOperations.Download)
                {
                    // Configured for Download. Ensure no duplicates
                    if (this._downloadRequestInterceptor != null)
                    {
                        throw WebUtil.ThrowDuplicateInterceptorException(className, this.ScopeName, attr);
                    }
                    this._downloadRequestInterceptor = info;
                 }
                if ((attr.Operation & SyncOperations.Upload) == SyncOperations.Upload)
                {
                    // Configured for Upload. Ensure no duplicates
                    CheckForDuplicateAndAddInterceptors(attr, info, className, ref _uploadRequestInterceptor, _uploadTypedRequestInterceptors);
                }
            }
            else if (attr is SyncResponseInterceptorAttribute)
            {
                // Its a Response interceptor. Check for Operation
                if ((attr.Operation & SyncOperations.Download) == SyncOperations.Download)
                {
                    // Configured for Download.
                    CheckForDuplicateAndAddInterceptors(attr, info, className, ref _downloadResponseInterceptor, _downloadTypedResponseInterceptors);
                }
                if ((attr.Operation & SyncOperations.Upload) == SyncOperations.Upload)
                {
                    // Configured for Upload.
                    CheckForDuplicateAndAddInterceptors(attr, info, className, ref _uploadResponseInterceptor, _uploadTypedResponseInterceptors);
                }
            }
            else
            {
                // Its a conflict interceptor. Check we dont have another conflict interceptor for the same scope
                CheckForDuplicateAndAddInterceptors(attr, info, className, ref _conflictInterceptor, _conflictTypedInterceptors);
            }
        }

        /// <summary>
        /// Utility function to check if a request interceptor is configured
        /// </summary>
        /// <param name="operation">SyncOperations</param>
        /// <returns>bool</returns>
        internal bool HasRequestInterceptor(SyncOperations operation)
        {
            switch (operation)
            {
                case SyncOperations.Download:
                    return this._downloadRequestInterceptor != null;
                case SyncOperations.Upload:
                    return this._uploadRequestInterceptor != null;
            }
            return false;
        }

        /// <summary>
        /// Utility function to check if a response interceptor is configured
        /// </summary>
        /// <param name="operation">SyncOperations</param>
        /// <returns>bool</returns>
        internal bool HasResponseInterceptor(SyncOperations operation)
        {
            switch (operation)
            {
                case SyncOperations.Download:
                    return this._downloadResponseInterceptor != null;
                case SyncOperations.Upload:
                    return this._uploadResponseInterceptor != null;
            }
            return false;
        }

        /// <summary>
        /// Utility function to check if a conflict interceptor is configured
        /// </summary>
        /// <returns>bool</returns>
        internal bool HasConflictInterceptor()
        {
            return this._conflictInterceptor != null;
        }

        /// <summary>
        /// Utility function to check if a request interceptor for the specific type is configured
        /// </summary>
        /// <returns>bool</returns>
        internal bool HasRequestInterceptor(Type type)
        {
            return this._uploadTypedRequestInterceptors.ContainsKey(type);
        }

        /// <summary>
        /// Utility function to check if a response interceptor for the specific type is configured
        /// </summary>
        /// <param name="operation">SyncOperations</param>
        /// <param name="type">Type requested</param>
        /// <returns>bool</returns>
        internal bool HasResponseInterceptor(SyncOperations operation, Type type) 
        {
            switch (operation)
            {
                case SyncOperations.Download:
                    return this._downloadTypedResponseInterceptors.ContainsKey(type);
                case SyncOperations.Upload:
                    return this._uploadTypedResponseInterceptors.ContainsKey(type);
            }
            return false;
        }

        /// <summary>
        /// Utility function to check if a conflict interceptor for the specific type is configured
        /// </summary>
        /// <returns>bool</returns>
        internal bool HasConflictInterceptor(Type type) 
        {
            return this._conflictTypedInterceptors.ContainsKey(type);
        }

        /// <summary>
        /// Utility function to check if any typed conflict interceptor is configured
        /// </summary>
        /// <returns>bool</returns>
        internal bool HasTypedConflictInterceptors()
        {
            return this._conflictTypedInterceptors.Count > 0;
        }

        /// <summary>
        /// Returns the request interceptor MethodInfo for specific Type
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>MethodInfo</returns>
        internal MethodInfo GetRequestInterceptor(Type type)
        {
            Debug.Assert(HasRequestInterceptor(type), "No Typed request interceptor found.");
            if (!HasRequestInterceptor(type))
            {
                return null;
            }

            return this._uploadTypedRequestInterceptors[type];
        }

        /// <summary>
        /// Returns the response interceptor MethodInfo for specific Type
        /// </summary>
        /// <param name="operation">SyncOperations</param>
        /// <param name="type">Type</param>
        /// <returns>MethodInfo</returns>
        internal MethodInfo GetResponseInterceptor(SyncOperations operation, Type type) 
        {
            Debug.Assert(HasResponseInterceptor(operation, type), "No Typed response interceptors found for operation " + operation);
            if (!HasResponseInterceptor(operation, type))
            {
                return null;
            }

            switch (operation)
            {
                case SyncOperations.Download:
                    return this._downloadTypedResponseInterceptors[type];
                case SyncOperations.Upload:
                    return this._uploadTypedResponseInterceptors[type];
            }
            return null;
        }

        /// <summary>
        /// Returns the conflict interceptor MethodInfo for specific Type
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>MethodInfo</returns>
        internal MethodInfo GetConflictInterceptor(Type type)
        {
            Debug.Assert(HasConflictInterceptor(type), "No Typed conflict interceptor found.");
            if (!HasConflictInterceptor(type))
            {
                return null;
            }

            return this._conflictTypedInterceptors[type];
        }

        /// <summary>
        /// Function that returns if any filtered request interceptors are configured
        /// </summary>
        /// <returns>bool</returns>
        internal bool HasTypedRequestInterceptors()
        {
            return _uploadTypedRequestInterceptors.Count > 0;
        }

        /// <summary>
        /// Function that returns if any filtered response interceptors are configured
        /// </summary>
        /// <param name="operation">SyncOperations</param>
        /// <returns>bool</returns>
        internal bool HasTypedResponseInterceptors(SyncOperations operation)
        {
            switch (operation)
            {
                case SyncOperations.Download:
                    return _downloadTypedResponseInterceptors.Count > 0;
                case SyncOperations.Upload:
                    return _uploadTypedResponseInterceptors.Count > 0;
            }
            return false;
        }


        private void CheckForDuplicateAndAddInterceptors(SyncInterceptorAttribute attr, MethodInfo info, string className,
        ref MethodInfo nonFilteredInfo, Dictionary<Type, MethodInfo> filteredInfo)
        {
            // Check we dont have two non filtered interceptors for same operation
            if (nonFilteredInfo != null)
            {
                throw WebUtil.ThrowDuplicateInterceptorException(className, this.ScopeName, attr);
            }

            // Check we dont have a filtered and non filtered interceptors for same operation
            if ((attr.EntityType != null && nonFilteredInfo != null) ||
                (attr.EntityType == null && filteredInfo.Count != 0))
            {
                throw WebUtil.ThrowFilteredAndNonFilteredInterceptorException(className, this.ScopeName, attr);
            }

            if (attr.EntityType != null)
            {
                // If filtered, ensure we dont have duplicates in type
                if (filteredInfo.ContainsKey(attr.EntityType))
                {
                    throw WebUtil.ThrowDuplicateInterceptorForArgumentException(className, this.ScopeName,
                        attr, attr.EntityType.FullName);
                }

                // Check that argument is of type IOfflineEntity
                if (!SyncServiceConstants.IOFFLINEENTITY_TYPE.IsAssignableFrom(attr.EntityType))
                {
                    throw WebUtil.ThrowInterceptorArgumentNotIOEException(className, this.ScopeName,
                        attr, attr.EntityType.FullName);
                }

                // If not then its valid. So add it to the list
                filteredInfo.Add(attr.EntityType, info);
            }
            else
            {
                // Its a valid non filtered interceptor.
                nonFilteredInfo = info;
            }
        }
    }
}
