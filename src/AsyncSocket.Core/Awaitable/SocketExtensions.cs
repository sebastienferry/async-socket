// ----------------------------------------------------------------------------
// "THE BEER-WARE LICENSE" (Revision 42):
// <phk@FreeBSD.ORG> wrote this file.  As long as you retain this notice you
// can do whatever you want with this stuff. If we meet some day, and you think
// this stuff is worth it, you can buy me a beer in return.   Poul-Henning Kamp
// ----------------------------------------------------------------------------

using System.Net.Sockets;

namespace ZuperSocket.Core.Awaitable
{
    public static class SocketExtensions
    {
        public static AwaitableSaea AcceptAsync(this Socket socket, AwaitableSaea awaitable)
        {
            awaitable.Reset();

            if (!socket.AcceptAsync(awaitable.EventArgs))
            {
                awaitable.WasCompleted = true;
            }
                
            return awaitable;
        }

        public static AwaitableSaea ReceiveAsync(this Socket socket, AwaitableSaea awaitable)
        {
            awaitable.Reset();

            if (!socket.ReceiveAsync(awaitable.EventArgs))
            {
                awaitable.WasCompleted = true;
            }
                
            return awaitable;
        }

        public static AwaitableSaea SendAsync(this Socket socket, AwaitableSaea awaitable)
        {
            awaitable.Reset();

            if (!socket.SendAsync(awaitable.EventArgs))
            {
                awaitable.WasCompleted = true;
            }
                
            return awaitable;
        }
    }
}