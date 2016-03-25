// ----------------------------------------------------------------------------
// "THE BEER-WARE LICENSE" (Revision 42):
// <phk@FreeBSD.ORG> wrote this file.  As long as you retain this notice you
// can do whatever you want with this stuff. If we meet some day, and you think
// this stuff is worth it, you can buy me a beer in return.   Poul-Henning Kamp
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ZuperSocket.Core.IoCompletionPort;

namespace ZuperSocket.Core.IoCompletionPort
{
    /// <summary>
    /// Create a large buffer on the heap for all SocketAsyncEventArgs
    /// of a pool, avoiding memory heap fragmentation.
    /// </summary>
    internal class SaeaSharedBuffer
    {
        // Buffer
        private readonly byte[] _buffer;

        // Pool managment
        private readonly Stack<int> _chunkIds;

        // Pool size
        public int PoolSize { get; private set; }

        // Chunk size
        public int ChunkSizeInBytes { get; private set; }
        
        /// <summary>
        /// Build a new buffer on the heap.
        /// </summary>
        /// <param name="bufferChunkSizeInBytes">This is the size of eahc individual element.</param>
        /// <param name="poolSize">This is the number of elements in the pool.</param>
        public SaeaSharedBuffer(int bufferChunkSizeInBytes, int poolSize)
        {
            PoolSize = poolSize;

            ChunkSizeInBytes = bufferChunkSizeInBytes;

            _buffer = new byte[ChunkSizeInBytes*PoolSize];

            _chunkIds = new Stack<int>(Enumerable.Range(0, PoolSize));
        }

        /// <summary>
        /// Assign a portion of this large buffer to a SocketAsyncEventArgs.
        /// Each portion are equals in size.
        /// </summary>
        /// <param name="saea">SocketAsyncEventArgs to set the buffer to.</param>
        public void AssignBuffer(SocketAsyncEventArgs saea)
        {
            if (_chunkIds.Count > 0)
            {
                int chunkId = _chunkIds.Pop();

                int offset = ChunkSizeInBytes*chunkId;

                saea.SetBuffer(_buffer, ChunkSizeInBytes * chunkId,
                    ChunkSizeInBytes);

                saea.UserToken = new UserToken(offset);

                return;
            }            

            throw new InvalidOperationException("No more space available in the heap buffer");
        }
    }
}
