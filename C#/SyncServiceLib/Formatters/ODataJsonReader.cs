// Copyright 2010 Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License"); 
// You may not use this file except in compliance with the License. 
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 

// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, 
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR 
// CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, 
// MERCHANTABLITY OR NON-INFRINGEMENT. 

// See the Apache 2 License for the specific language governing 
// permissions and limitations under the License.

using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.Serialization.Json;
#if SERVER
using Microsoft.Synchronization.Services;
#elif CLIENT
using Microsoft.Synchronization.ClientServices;
#endif

namespace Microsoft.Synchronization.Services.Formatters
{
    class ODataJsonReader : SyncReader
    {
        bool _traversingResultsNode = false;

        /// <summary>
        /// Constructor with no KnownTypes specified
        /// </summary>
        /// <param name="stream">Input reader stream</param>
        public ODataJsonReader(Stream stream)
            : this(stream, null)
        { }

        /// <summary>
        /// Constructor with KnownTypes specified
        /// </summary>
        /// <param name="stream">Input reader stream</param>
        /// <param name="knownTypes">List of types to reflect from</param>
        public ODataJsonReader(Stream stream, Type[] knownTypes)
            : base(stream, knownTypes)
        {
#if SERVER
            _reader = JsonReaderWriterFactory.CreateJsonReader(stream, new XmlDictionaryReaderQuotas());
#elif CLIENT
            _reader = JsonReaderWriterFactory.CreateJsonReader(stream, XmlDictionaryReaderQuotas.Max);
#endif
        }

        /// <summary>
        /// Validates that the stream contains a valid Json feed.
        /// </summary>
        public override void Start()
        {
            _reader.MoveToContent();

            if (_reader.Name != FormatterConstants.JsonDocumentElementName)
            {
                throw new InvalidOperationException("Not a valid Json Feed.");
            }
        }

        /// <summary>
        /// Returns the current Item type at which the reader is positioned.
        /// </summary>
        public override ReaderItemType ItemType
        {
            get
            {
                return _currentType;
            }
        }

        /// <summary>
        /// Returns the current Json object casted as an IOfflineEntity element
        /// </summary>
        /// <returns>Typed entry element</returns>
        public override IOfflineEntity GetItem()
        {
            CheckItemType(ReaderItemType.Entry);
            
            // Get the type name and the list of properties.
            _currentEntryWrapper = new JsonEntryInfoWrapper((XElement)XElement.ReadFrom(_reader));

            _liveEntity = ReflectionUtility.GetObjectForType(_currentEntryWrapper, this._knownTypes);
            return _liveEntity;
        }

        /// <summary>
        /// Returns the value of the hasMoreChanges key in __sync:{} Json object
        /// </summary>
        /// <returns>bool</returns>
        public override bool GetHasMoreChangesValue()
        {
            CheckItemType(ReaderItemType.HasMoreChanges);
            return (bool)_reader.ReadElementContentAs(FormatterConstants.BoolType, null);
        }

        /// <summary>
        /// Returns the serverBlob key contents from the __sync:{} Json object
        /// </summary>
        /// <returns>byte[]</returns>
        public override byte[] GetServerBlob()
        {
            CheckItemType(ReaderItemType.SyncBlob);
            string encodedBlob = (string)_reader.ReadElementContentAs(FormatterConstants.StringType, null);
            return Convert.FromBase64String(encodedBlob);
        }

        /// <summary>
        /// Traverses through the feed and returns when it arrives at the necessary object.
        /// </summary>
        /// <returns>bool detecting whether or not there is more elements to be read.</returns>
        public override bool Next()
        {
            // User did not read the node.
            if (_currentType != ReaderItemType.BOF && !_currentNodeRead)
            {
                _reader.Skip();
            }

            do
            {
                _currentEntryWrapper = null;
                _liveEntity = null;
                if (JsonHelper.IsElement(_reader, FormatterConstants.JsonSyncResultsElementName))
                {

                    if (_traversingResultsNode && _reader.IsStartElement())
                    {
                        throw new InvalidOperationException("Json feed has more than one results entry. Invalid stream.");
                    }
                    _traversingResultsNode = _reader.IsStartElement();
                }
                else if (JsonHelper.IsElement(_reader, "item") && _traversingResultsNode)
                {
                    _currentType = ReaderItemType.Entry;
                    _currentNodeRead = false;
                    return true;
                }
                else if (JsonHelper.IsElement(_reader, FormatterConstants.ServerBlobText))
                {
                    _currentType = ReaderItemType.SyncBlob;
                    _currentNodeRead = false;
                    return true;
                }
                else if (JsonHelper.IsElement(_reader, FormatterConstants.MoreChangesAvailableText))
                {
                    _currentType = ReaderItemType.HasMoreChanges;
                    _currentNodeRead = false;
                    return true;
                }
            } while (_reader.Read());

            this._currentType = ReaderItemType.EOF;
            return false;
        }
    }
}
