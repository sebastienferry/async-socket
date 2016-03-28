// ----------------------------------------------------------------------------
// "THE BEER-WARE LICENSE" (Revision 42):
// <phk@FreeBSD.ORG> wrote this file.  As long as you retain this notice you
// can do whatever you want with this stuff. If we meet some day, and you think
// this stuff is worth it, you can buy me a beer in return.   Poul-Henning Kamp
// ----------------------------------------------------------------------------

namespace ZuperSocket.Core.IoCompletionPort
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    public class AsyncEvent
    {
        public AsyncOperation Operation { get; private set; }

        public AsyncSocket Socket { get; private set; }

        public byte[] Buffer { get; set; }

        public AsyncEvent(AsyncOperation operation, AsyncSocket socket, int bufferSize=0)
        {
            Operation = operation;

            Socket = socket;

            if (bufferSize > 0)
            {
                Buffer = new byte[bufferSize];
            }
        }
    }
    
    public enum AsyncOperation
    {
        NewConnection,
        DataReceived,
        DataSent
    }
}