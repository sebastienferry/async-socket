// ----------------------------------------------------------------------------
// "THE BEER-WARE LICENSE" (Revision 42):
// <phk@FreeBSD.ORG> wrote this file.  As long as you retain this notice you
// can do whatever you want with this stuff. If we meet some day, and you think
// this stuff is worth it, you can buy me a beer in return.   Poul-Henning Kamp
// ----------------------------------------------------------------------------

namespace ZuperSocket.Core.Messaging.Patterns
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;
    using ZuperSocket.Core.IoCompletionPort;
    
    /// <summary>
    /// Allows to handle request-response messaging pattern over TCP.
    /// A <see cref="Requester"/> must be used on the request side and it communicates with a <seealso cref="Replier"/>.
    /// </summary>
    public class Requester
    {
        /// <summary>
        /// Do the stuff.
        /// </summary>
        public void Do()
        {
            IPAddress ipaddress = IPAddress.Parse("127.0.0.1");

            EndPoint endpoint = new IPEndPoint(ipaddress, 5555);

            Socket socket = new Socket(ipaddress.AddressFamily, SocketType.Stream, ProtocolType.IP);

            ConcurrentQueue<AsyncEvent> asyncEvents = new ConcurrentQueue<AsyncEvent>();

            AsyncSocket clientSocket = AsyncSocket.Create(socket, asyncEvents);

            clientSocket.Connect(
                endpoint,
                wrapper =>
            {
                Console.WriteLine("Connection granted");

                byte[] message = Encoding.UTF8.GetBytes("ping");

                wrapper.Send(message);
            });

            Console.ReadLine();
        }
    }
}