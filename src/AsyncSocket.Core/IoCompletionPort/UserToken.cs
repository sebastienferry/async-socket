// ----------------------------------------------------------------------------
// "THE BEER-WARE LICENSE" (Revision 42):
// <phk@FreeBSD.ORG> wrote this file.  As long as you retain this notice you
// can do whatever you want with this stuff. If we meet some day, and you think
// this stuff is worth it, you can buy me a beer in return.   Poul-Henning Kamp
// ----------------------------------------------------------------------------

namespace AsyncSocket.Core.IoCompletionPort
{
    using System.Net.Sockets;

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
    }
}
