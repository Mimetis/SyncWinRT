using System;
using System.Threading;


namespace Microsoft.Synchronization.ClientServices.IsolatedStorage
{
    /// <summary>
    /// This class implements a lock using an auto-reset event
    /// 
    /// Note: Be careful using this class.  It can cause a deadlock if you
    /// attempt to lock twice from the same thread since it doesn't keep track
    /// of thread ids.
    /// </summary>
    internal class AutoResetLock : IDisposable
    {
        public AutoResetLock()
        {
            _isDisposed = false;
            _event = new AutoResetEvent(true);
        }

        /// <summary>
        /// This method is used to preserve the scoping semantics of lock(...) { ... }. Instead
        /// it should be used as: using(lock.LockObject()) { .... }
        /// </summary>
        /// <returns>Object that holds the lock</returns>
        public IDisposable LockObject()
        {
            return new Locker(this);
        }

        /// <summary>
        /// This method will just wait until the lock can be entered
        /// </summary>
        public void Lock()
        {
            _event.WaitOne();
        }

        /// <summary>
        /// This sets the event so other threads can proceed
        /// </summary>
        public void Unlock()
        {
            _event.Set();
        }

        /// <summary>
        /// Disposes the event, necessary because of the AutoResetEvent
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (_event != null)
                    {
                        _event.Dispose();
                        _isDisposed = true;
                    }
                }
            }
        }

        /// <summary>
        /// This class is what is returned in the LockObject method which allows us to use
        /// the using statement to give us nice scoping
        /// </summary>
        internal class Locker : IDisposable
        {
            /// <summary>
            /// This method will take the lock passed in
            /// </summary>
            /// <param name="l"></param>
            public Locker(AutoResetLock l)
            {
                _lock = l;
                _lock.Lock();
            }

            /// <summary>
            /// Dispose will release the lock
            /// </summary>
            public void Dispose()
            {
                _lock.Unlock();
                _lock = null;
                GC.SuppressFinalize(this);
            }

            AutoResetLock _lock;
        }

        private bool _isDisposed;
        private AutoResetEvent _event;
    }
}
