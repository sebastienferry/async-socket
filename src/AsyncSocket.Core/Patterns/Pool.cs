// ----------------------------------------------------------------------------
// "THE BEER-WARE LICENSE" (Revision 42):
// <phk@FreeBSD.ORG> wrote this file.  As long as you retain this notice you
// can do whatever you want with this stuff. If we meet some day, and you think
// this stuff is worth it, you can buy me a beer in return.   Poul-Henning Kamp
// ----------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace ZuperSocket.Core.Patterns
{
    /// <summary>
    /// Pool to manage reusable instances.
    /// </summary>
    internal class Pool<T>
    {
        private readonly ConcurrentStack<T> _pool;

        /// <summary>
        /// Create a new pool.
        /// </summary>
        public Pool()
        {
            _pool = new ConcurrentStack<T>();
        }

        /// <summary>
        /// Current number of available item in the pool.
        /// </summary>
        internal int Count
        {
            get { return _pool.Count; }
        }

        /// <summary>
        /// Pop one available instance from the pool making it unavailable.
        /// </summary>
        /// <returns>One availabe instance</returns>
        internal T Pop()
        {
            T item = default(T);

            _pool.TryPop(out item);

            return item;
        }

        /// <summary>
        /// Append an instance to the pool.
        /// The instance would be available for reuse. 
        /// </summary>
        internal void Push(T item)
        {
            _pool.Push(item);
        }
    }
}