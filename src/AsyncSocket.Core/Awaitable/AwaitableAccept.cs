// ----------------------------------------------------------------------------
// "THE BEER-WARE LICENSE" (Revision 42):
// <phk@FreeBSD.ORG> wrote this file.  As long as you retain this notice you
// can do whatever you want with this stuff. If we meet some day, and you think
// this stuff is worth it, you can buy me a beer in return.   Poul-Henning Kamp
// ----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace ZuperSocket.Core.Awaitable
{
    internal class AwaitableAccept : AwaitableSaea
    {
        public AwaitableAccept(SocketAsyncEventArgs eventArgs) : base(eventArgs) { }

        public Socket GetResult()
        {
            if (EventArgs.SocketError != SocketError.Success)
                throw new SocketException((int)EventArgs.SocketError);

            return EventArgs.AcceptSocket;
        }
    }
}
