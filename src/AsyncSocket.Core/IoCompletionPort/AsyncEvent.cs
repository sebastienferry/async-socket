// ----------------------------------------------------------------------------
// "THE BEER-WARE LICENSE" (Revision 42):
// <phk@FreeBSD.ORG> wrote this file.  As long as you retain this notice you
// can do whatever you want with this stuff. If we meet some day, and you think
// this stuff is worth it, you can buy me a beer in return.   Poul-Henning Kamp
// ----------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncSocket.Core.IoCompletionPort
{
    /// <summary>
    /// Type of operation.
    /// </summary>
    public enum AsyncOperation
    {
        /// <summary>
        /// New connection event.
        /// </summary>
        NewClient,

        /// <summary>
        /// New connection to listeing socket accepted.
        /// </summary>
        ConnectionAccepted,

        /// <summary>
        /// New data received.
        /// </summary>
        DataReceived,

        /// <summary>
        /// Data sent.
        /// </summary>
        DataSent
    }
    
    /// <summary>
    /// An event associated to an asynchronous socket operation.
    /// </summary>
    public class AsyncEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncEvent"/> class.
        /// </summary>
        /// <param name="operation">Operation type</param>
        /// <param name="socket">Associated socket wrapper</param>
        /// <param name="bufferSize">Size of the buffer.</param>
        public AsyncEvent(AsyncOperation operation, IAsyncSocket socket, int bufferSize = 0)
        {
            Operation = operation;

            Peer = socket;

            if (bufferSize > 0)
            {
                Buffer = new byte[bufferSize];
            }
        }
        
        /// <summary>
        /// Gets the operation type of the event.
        /// </summary>
        public AsyncOperation Operation { get; private set; }

        /// <summary>
        /// Gets the peer associated to the event.
        /// </summary>
        public IAsyncSocket Peer { get; private set; }

        /// <summary>
        /// Gets the buffer of the event if it is related to incoming data.
        /// </summary>
        public byte[] Buffer { get; private set; }
    }
}