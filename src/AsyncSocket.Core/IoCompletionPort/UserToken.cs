// ----------------------------------------------------------------------------
// "THE BEER-WARE LICENSE" (Revision 42):
// <phk@FreeBSD.ORG> wrote this file.  As long as you retain this notice you
// can do whatever you want with this stuff. If we meet some day, and you think
// this stuff is worth it, you can buy me a beer in return.   Poul-Henning Kamp
// ----------------------------------------------------------------------------

using System;
using System.Linq;
using System.Security.Cryptography;

namespace AsyncSocket.Core.IoCompletionPort
{
    /// <summary>
    /// UserToken objects are used to hold application level data
    /// associated to SocketAsyncEventArgs objects. In our case,
    /// they hold the AsyncSocket object that initiated the SocketAsyncEventArgs event
    /// and the offset in the global heap buffer shared among all SocketAsyncEventArgs instances.
    /// <seealso cref="https://msdn.microsoft.com/fr-fr/library/system.net.sockets.socketasynceventargs.usertoken(v=vs.110).aspx"/>
    /// </summary>
    internal class UserToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserToken"/> class.
        /// This is done only one time when we create the SocketAsyncEventArgs pool. 
        /// </summary>
        /// <param name="heapBufferOffset">This is the offset in the shared heap buffer.</param>
        /// <param name="socket">The socket that host the completion delegate.</param>
        public UserToken(int heapBufferOffset, AsyncSocketWrapper socket)
        {
            this.HeapBufferOffset = heapBufferOffset;

            this.Socket = socket;
        }
        
        /// <summary>
        /// Gets the the offset in the global shared buffer shared among all SocketAsyncEventArgs instances.
        /// </summary>
        public int HeapBufferOffset { get; private set; }

        /// <summary>
        /// Gets or sets the AsyncSocket wrapper associated to the .NET Socket that initiated the SocketAsyncEventArgs.
        /// </summary>
        public AsyncSocketWrapper Socket { get; private set; }

        public IncomingMessage IncomingMessage;
    }

    internal class Frame
    {
        public byte[] FrameBytes;

        public int BytesToReceive { get; set; }

        public int BytesReceived { get; set; }

        public void Fill(byte[] buffer, int offset, int availableBytes)
        {
            Buffer.BlockCopy(
                buffer,
                offset,
                FrameBytes,
                BytesReceived,
                Math.Max(availableBytes, BytesToReceive));

            BytesReceived += availableBytes;
            BytesToReceive -= availableBytes;
        }

    }

    internal class Protocol
    {
        private const int MESSAGE_PREFIX_SIZE_IN_BYTES = sizeof(int);

        public const int MESSAGE_MAX_SIZE_IN_BYTES = 4096;

        public static IncomingMessage Create()
        {
            return  new IncomingMessage();
        }

        public static bool TryReadMessage(
            byte[] buffer,
            int offset,
            int availableBytes,
            ref IncomingMessage incomingMessage)
        {
            if (TryReadHeader(buffer, offset, availableBytes, ref incomingMessage))
            {
                return TryReadBody(buffer, offset, availableBytes, ref incomingMessage);
            }

            return false;
        }
        
        public static bool TryReadHeader(byte[] buffer, int offset, int availableBytes, ref IncomingMessage incomingMessage)
        {
            if (incomingMessage.Header.BytesReceived < MESSAGE_PREFIX_SIZE_IN_BYTES)
            {
                // Try to read header first
                incomingMessage.Header.Fill(buffer, offset, availableBytes);
            }

            if (incomingMessage.Header.BytesReceived < MESSAGE_PREFIX_SIZE_IN_BYTES)
            {
                return false;
            }

            incomingMessage.Body.BytesToReceive = BitConverter.ToInt32(incomingMessage.Header.FrameBytes, 0);
            
            incomingMessage.Body.BytesReceived = 0;

            return true;
        }

        public static bool TryReadBody(byte[] buffer, int offset, int availableBytes, ref IncomingMessage incomingMessage)
        {
            if (incomingMessage.Body.BytesReceived < incomingMessage.Body.BytesToReceive)
            {
                // Try to read header first
                incomingMessage.Body.Fill(buffer, offset, availableBytes);
            }

            if (incomingMessage.Body.BytesToReceive > 0)
            {
                return false;
            }

            return true;
        }
    }
    
    internal class IncomingMessage
    {
        public Frame Header;

        public Frame Body;
    }
}
