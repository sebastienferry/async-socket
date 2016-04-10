// ----------------------------------------------------------------------------
// "THE BEER-WARE LICENSE" (Revision 42):
// <phk@FreeBSD.ORG> wrote this file.  As long as you retain this notice you
// can do whatever you want with this stuff. If we meet some day, and you think
// this stuff is worth it, you can buy me a beer in return.   Poul-Henning Kamp
// ----------------------------------------------------------------------------

using System.Diagnostics;

namespace AsyncSocket.Core.IoCompletionPort
{
    /// <summary>
    /// Interface used to mask methods of AsyncSocket
    /// </summary>
    public interface IAsyncSocket
    {
        /// <summary>
        /// Sends a bunch of bytes.
        /// </summary>
        /// <param name="bytes">Array of bytes to send.</param>
        void Send(byte[] bytes);

        /// <summary>
        /// Receive data.
        /// </summary>
        void Receive();
    }
}