using System;
using System.Collections.Generic;
using System.Net;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Synchronization.ClientServices.Common;

namespace Microsoft.Synchronization.ClientServices
{
    /// <summary>
    /// CacheControllerBehavior Class
    /// </summary>
    public class CacheControllerBehavior
    {
        bool locked;
        readonly object lockObject = new object();
        List<Type> _knownTypes;
        ICredentials credentials;
        SerializationFormat serFormat;
        string scopeName;
        //Action<HttpWebRequest, Action<HttpWebRequest>> beforeSendingRequestHandler;
        //Action<HttpWebResponse> afterSendingResponse;
        readonly Dictionary<string, string> scopeParameters;
        readonly Dictionary<string, string> customHeaders;
        readonly bool automaticDecompression;

        internal CacheControllerBehavior()
        {
            this._knownTypes = new List<Type>();
            this.serFormat = SerializationFormat.ODataAtom;
            this.scopeParameters = new Dictionary<string, string>();
        }

        /// <summary>
        /// Represents the list of types that will be referenced when deserializing entities from the sync service.
        /// Default behavior is all public types deriving from IOfflineEntity is searched in all loaded assemblies.
        /// When this list is non empty the search is narrowed down to this list.
        /// </summary>
        public ReadOnlyCollection<Type> KnownTypes
        {
            get { return new ReadOnlyCollection<Type>(this._knownTypes); }
        }

        /// <summary>
        /// A enumerator that lets uses browse the list of specified scope level filter parameters.
        /// </summary>
        public Dictionary<string,string>.Enumerator ScopeParameters
        {
            get
            {
                return this.scopeParameters.GetEnumerator();
            }
        }

        /// <summary>
        /// Credentials that will be passed along with each outgoing WebRequest.
        /// </summary>
        public ICredentials Credentials
        {
            get
            {
                return this.credentials;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                lock (this.lockObject)
                {
                    CheckLockState();
                    this.credentials = value;
                }
            }
        }

        /// <summary>
        /// Represents the SerializatonFormat to be used for the network payload.
        /// </summary>
        public SerializationFormat SerializationFormat
        {
            get
            {
                return this.serFormat;
            }

            set
            {
                lock (this.lockObject)
                {
                    CheckLockState();
                    this.serFormat = value;
                }
            }
        }

        /// <summary>
        /// Returns the Sync scope name
        /// </summary>
        public string ScopeName
        {
            get
            {
                return this.scopeName;
            }

            internal set
            {
                this.scopeName = value;
            }
        }

        /// <summary>
        /// Represents if AutomaticDecompression should be used for outgoing WebRequest.
        /// </summary>
        public bool AutomaticDecompression { get; set; }

        /// <summary>
        /// Adds an Type to the collection of KnownTypes.
        /// </summary>
        /// <typeparam name="T">Type to include in search</typeparam>
        public void AddType<T>() where T : IOfflineEntity
        {
            lock (this.lockObject)
            {
                CheckLockState();
                this._knownTypes.Add(typeof(T));
            }
        }

        /// <summary>
        /// Function that users will use to add any custom scope level filter parameters and their values.
        /// </summary>
        /// <param name="key">parameter name as string</param>
        /// <param name="value">parameter value as string</param>
        public void AddScopeParameters(string key, string value)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("key cannot be empty", "key");

            if (value == null)
                throw new ArgumentNullException("value");

            lock (this.lockObject)
            {
                CheckLockState();
                this.scopeParameters.Add(key, value);
            }
        }

        /// <summary>
        /// Function that users will use to add any custom headers and their values.
        /// </summary>
        /// <param name="key">parameter name as string</param>
        /// <param name="value">parameter value as string</param>
        public void AddCustomHeaders(string key, string value)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("key cannot be empty", "key");

            if (value == null)
                throw new ArgumentNullException("value");

            lock (this.lockObject)
            {
                CheckLockState();
                this.customHeaders.Add(key, value);
            }
        }

        private void CheckLockState()
        {
            if (this.locked)
                throw new CacheControllerException("Cannot modify CacheControllerBehavior when sync is in progress.");
        }

        internal Dictionary<string, string> ScopeParametersInternal
        {
            get
            {
                return this.scopeParameters;
            }
        }

        internal Dictionary<string, string> CustomHeadersInternal
        {
            get
            {
                return this.customHeaders;
            }
        }

        internal bool Locked
        {
            set
            {
                lock (this.lockObject)
                {
                    this.locked = value;
                }
            }
        }
    }
}
