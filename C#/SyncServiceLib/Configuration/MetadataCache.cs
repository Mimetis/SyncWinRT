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
using System.Threading;

namespace Microsoft.Synchronization.Services
{
    /// <summary>
    /// This class represents a cache of metadata for a sync service.
    /// The cache key is denoted by the type MetadataCacheKey and an item in the cache is of type
    /// MetadataCacheItem.
    /// This will allow us to store a lot of metadata about the service and scope it to a single service head thus also
    /// allowing us to host multiple service heads in a single website on IIS or Azure.
    /// 
    /// Usage:
    /// Metadata for a new service can be added as follows:
    ///         MetadataCache.AddCacheItem(serviceType, item);
    /// Where serviceType is the type of the service class and item is of type MetadataCacheItem.
    /// 
    /// Similarly, you can lookup an item from the cache using the service type as follows:
    ///         MetadataCacheItem item = MetadataCache.TryLookup(serviceType);
    /// Where serviceType is the type of the service class.
    /// </summary>
    internal static class MetadataCache
    {
        /// <summary>
        /// Cache that contains the service metadata.
        /// </summary>
        private static readonly Dictionary<MetadataCacheKey, MetadataCacheItem> _cache =
            new Dictionary<MetadataCacheKey, MetadataCacheItem>(new MetadataCacheKey.Comparer());

        /// <summary>
        /// Lock used for concurrency scenarios when adding and looking up items from the cache.
        /// </summary>
        private static readonly object _lockObject = new object();

        /// <summary>
        /// Add an MetadataCacheItem object to the cache for a given service.
        /// </summary>
        /// <param name="serviceType">Type of the service class</param>
        /// <param name="item">Item to be added to the cache</param>
        /// <returns>Item that was added to the cache</returns>
        internal static MetadataCacheItem AddCacheItem(Type serviceType, MetadataCacheItem item)
        {
            MetadataCacheItem item2;
            
            var key = new MetadataCacheKey(serviceType);
            
            // Use a double check lock for concurrency.
            if (!_cache.TryGetValue(key, out item2))
            {
                lock (_lockObject)
                {
                    if (!_cache.TryGetValue(key, out item2))
                    {
                        _cache.Add(key, item);

                        item2 = item;
                    }
                }
            }

            return item2;
        }

        /// <summary>
        /// Lookup an item from the metadata cache for a given service type.
        /// </summary>
        /// <param name="serviceType">Service type for which we want to lookup metadata.</param>
        /// <returns>MetadataCacheItem for the service type. Null if there is no item in the cache for the given service type.</returns>
        internal static MetadataCacheItem TryLookup(Type serviceType)
        {
            MetadataCacheItem item;

            var key = new MetadataCacheKey(serviceType);
            
            _cache.TryGetValue(key, out item);

            return item;
        }
    }
}
