// ----------------------------------------------------------------------------
// "THE BEER-WARE LICENSE" (Revision 42):
// <phk@FreeBSD.ORG> wrote this file.  As long as you retain this notice you
// can do whatever you want with this stuff. If we meet some day, and you think
// this stuff is worth it, you can buy me a beer in return.   Poul-Henning Kamp
// ----------------------------------------------------------------------------

namespace AsyncSocket.Core.IoCompletionPort
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Net.Sockets;

    /// <summary>
    /// Create a large buffer on the heap for all SocketAsyncEventArgs
    /// of a pool, avoiding memory heap fragmentation.
    /// </summary>
    internal class SaeaSharedBuffer : IDisposable
    {
        /// <summary>
        /// Large heap buffer that would be reused among multiple SocketAsyncEventArgs.
        /// </summary>
        private readonly byte[] buffer;

        /// <summary>
        /// Pool management. We divide the buffer into same-size chunks identified by their ID (zero based).
        /// We use a concurrent stack because of the multi-threaded aspect of client / server communication.
        /// </summary>
        private readonly ConcurrentStack<int> chunkIds;

        /// <summary>
        /// Initializes a new instance of the <see cref="SaeaSharedBuffer"/> class.
        /// </summary>
        /// <param name="bufferChunkSizeInBytes">This is the size of each individual element.</param>
        /// <param name="poolSize">This is the number of elements in the pool.</param>
        public SaeaSharedBuffer(int bufferChunkSizeInBytes, int poolSize)
        {
            this.PoolSize = poolSize;

            this.ChunkSizeInBytes = bufferChunkSizeInBytes;

            this.buffer = new byte[this.ChunkSizeInBytes * this.PoolSize];

            this.chunkIds = new ConcurrentStack<int>(Enumerable.Range(0, this.PoolSize));
        }
        
        /// <summary>
        /// Gets the size of the pool.
        /// </summary>
        public int PoolSize { get; private set; }

        /// <summary>
        /// Gets the size of a chunk in bytes.
        /// </summary>
        public int ChunkSizeInBytes { get; private set; }
        
        /// <summary>
        /// Assign a portion of this large buffer to a SocketAsyncEventArgs.
        /// Each portion are equal in size.
        /// </summary>
        /// <param name="saea">SocketAsyncEventArgs to set the buffer to.</param>
        public void AssignBuffer(SocketAsyncEventArgs saea)
        {
            if (this.chunkIds.Count > 0)
            {
                int chunkId = 0;
                if (this.chunkIds.TryPop(out chunkId))
                {
                    int offset = this.ChunkSizeInBytes * chunkId;

                    saea.SetBuffer(
                        this.buffer,
                        this.ChunkSizeInBytes * chunkId,
                        this.ChunkSizeInBytes);

                    return;    
                }
            }            

            throw new InvalidOperationException("No more space available in the heap buffer");
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
