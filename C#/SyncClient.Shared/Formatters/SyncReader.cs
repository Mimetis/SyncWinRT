using System;
using System.Globalization;
using System.IO;
using System.Xml;
using Microsoft.Synchronization.ClientServices;
using Microsoft.Synchronization.ClientServices.Common;

namespace Microsoft.Synchronization.Services.Formatters
{
    /// <summary>
    /// Abstract class for SyncReader that individual format readers needs to extend
    /// </summary>    
    internal abstract class SyncReader : IDisposable
    {
        protected EntryInfoWrapper currentEntryWrapper;
        protected bool currentNodeRead = false;
        protected ReaderItemType currentType;
        protected Stream inputStream;
        protected Type[] knownTypes;
        protected IOfflineEntity liveEntity;
        protected XmlReader reader;

        protected SyncReader(Stream stream, Type[] types)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            inputStream = stream;
            knownTypes = types;
        }

        public abstract ReaderItemType ItemType { get; }

        #region IDisposable Members

        public void Dispose()
        {
            if (inputStream != null)
            {
                using (inputStream)
                {
                    //inputStream.Close();
                    inputStream.Dispose();
                }
            }
            inputStream = null;
            knownTypes = null;
        }

        #endregion

        public abstract void Start();

        public abstract IOfflineEntity GetItem();
        public abstract byte[] GetServerBlob();
        public abstract bool GetHasMoreChangesValue();
        public abstract bool Next();

        /// <summary>
        /// Check to see if the current object that was just parsed had a conflict element on it or not.
        /// </summary>
        /// <returns>bool</returns>
        public virtual bool HasConflict()
        {
            if (currentEntryWrapper != null)
            {
                return currentEntryWrapper.ConflictWrapper != null;
            }
            return false;
        }

        /// <summary>
        /// Check to see if the current conflict object that was just parsed has a tempId element on it or not.
        /// </summary>
        /// <returns>bool</returns>
        public virtual bool HasConflictTempId()
        {
            if (currentEntryWrapper != null && currentEntryWrapper.ConflictWrapper != null)
            {
                return currentEntryWrapper.ConflictWrapper.TempId != null;
            }
            return false;
        }

        /// <summary>
        /// Check to see if the current object that was just parsed has a tempId element on it or not.
        /// </summary>
        /// <returns>bool</returns>
        public virtual bool HasTempId()
        {
            if (currentEntryWrapper != null)
            {
                return currentEntryWrapper.TempId != null;
            }
            return false;
        }

        /// <summary>
        /// Returns the TempId parsed from the current object if present
        /// </summary>
        /// <returns>string</returns>
        public virtual string GetTempId()
        {
            if (!HasTempId())
            {
                return null;
            }

            return currentEntryWrapper.TempId;
        }

        /// <summary>
        /// Returns the TempId parsed from the current conflict object if present
        /// </summary>
        /// <returns>string</returns>
        public virtual string GetConflictTempId()
        {
            if (!HasConflictTempId())
            {
                return null;
            }

            return currentEntryWrapper.ConflictWrapper.TempId;
        }

        /// <summary>
        /// Get the conflict item
        /// </summary>
        /// <returns>Conflict item</returns>
        public virtual Conflict GetConflict()
        {
            if (!HasConflict())
            {
                return null;
            }

            Conflict conflict;

            if (currentEntryWrapper.IsConflict)
            {
                conflict = new SyncConflict
                {
                    LiveEntity = liveEntity,
                    LosingEntity = ReflectionUtility.GetObjectForType(currentEntryWrapper.ConflictWrapper, knownTypes),
                    Resolution =
                        (SyncConflictResolution)
                        Enum.Parse(FormatterConstants.SyncConflictResolutionType,
                                    currentEntryWrapper.ConflictDesc, true)
                };
            }
            else
            {
                conflict = new SyncError
                {
                    LiveEntity = liveEntity,
                    ErrorEntity =
                        ReflectionUtility.GetObjectForType(currentEntryWrapper.ConflictWrapper,
                                                            knownTypes),
                    Description = currentEntryWrapper.ConflictDesc
                };
            }

            return conflict;
        }

        protected void CheckItemType(ReaderItemType type)
        {
            if (currentType != type)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                                  "{0} is not a valid {1} element.", reader.Name, type));
            }

            currentNodeRead = true;
        }
    }
}