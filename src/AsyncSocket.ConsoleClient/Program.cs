// ----------------------------------------------------------------------------
// "THE BEER-WARE LICENSE" (Revision 42):
// <phk@FreeBSD.ORG> wrote this file.  As long as you retain this notice you
// can do whatever you want with this stuff. If we meet some day, and you think
// this stuff is worth it, you can buy me a beer in return.   Poul-Henning Kamp
// ----------------------------------------------------------------------------

using System.Collections.Concurrent;
using AsyncSocket.Core.IoCompletionPort;

namespace AsyncSocket.ConsoleClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AsyncSocket.Core;
    using AsyncSocket.Core.Messaging.Patterns;

    /// <summary>
    /// Client in a console.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Client console entry point.
        /// </summary>
        public static void Main()
        {
            IPAddress ipaddress = IPAddress.Parse("192.168.225.134");
            
            Requester requester = new Requester(ipaddress, 5555);

            CancellationTokenSource cts = new CancellationTokenSource();

            int loopSize = 0;

            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
            {
                socket.Connect(new IPEndPoint(ipaddress, 5555));

                var t = new ConcurrentQueue<AsyncEvent>();

                AsyncSocketWrapper asyncSocket = AsyncSocketWrapper.Create(socket, t);

                asyncSocket.Send(Encoding.UTF8.GetBytes("ping"));

                //socket.Send(Encoding.UTF8.GetBytes("ping"));
            }

            Console.ReadLine();
            

            //AsyncSocket socket = AsyncSocket.Create()

            //socket.Connect();


            //requester.Start(async (evt) =>
            //{
            //    Console.WriteLine(evt.Operation.ToString());

            //    if (evt.Operation == AsyncOperation.ConnectionAccepted ||
            //        evt.Operation == AsyncOperation.DataReceived)
            //    {
            //        // Send some data to the connected peed.
            //        evt.Peer.Send(Encoding.UTF8.GetBytes("ping"));

            //        loopSize++;

            //        if (loopSize >= 1 - 1)
            //        {
            //            cts.Cancel();
            //        }
            //    }

            //    await Task.Yield();

            //}, 1, cts.Token).Wait(cts.Token);

            Console.ReadLine();
        }
    }
}
