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
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using ZuperSocket.Core.IoCompletionPort;

    /// <summary>
    /// This class is used in the request response pattern. It handles the response side.
    /// </summary>
    public class Replier
    {
        /// <summary>
        /// Start the replier.
        /// </summary>
        public void Start()
        {
            // Prepare IP address to bind to.
            IPAddress ipaddress = ParseAddressIntoIPAddress("127.0.0.1");

            IPEndPoint ipendpoint = new IPEndPoint(ipaddress, 5555);

            ConcurrentQueue<AsyncEvent> events = new ConcurrentQueue<AsyncEvent>();

            // Initialize a new socket.
            using (Socket socket = new Socket(ipaddress.AddressFamily, SocketType.Stream, ipaddress.AddressFamily == AddressFamily.InterNetworkV6 ? ProtocolType.IPv6 : ProtocolType.IP))
            {
                socket.Bind(ipendpoint);

                socket.Listen(100);

                AsyncSocket eventSocket = AsyncSocket.Create(socket, events);
                
                while (true)
                {
                    eventSocket.Accept();

                    AsyncEvent asyncEvent = null;
                    
                    if (eventSocket.GetEvent(out asyncEvent))
                    {
                        if (asyncEvent.Operation == AsyncOperation.NewConnection)
                        {
                            Trace.WriteLine("NewConnection event");

                            // New client. Start to receive data.
                            asyncEvent.Socket.Receive();
                        }

                        if (asyncEvent.Operation == AsyncOperation.DataReceived)
                        {
                            Trace.WriteLine("DataReceived event");

                            // Data received.
                            string message = Encoding.UTF8.GetString(asyncEvent.Buffer);

                            Console.WriteLine(message);

                            // Send data back to the customer.
                            asyncEvent.Socket.Send(asyncEvent.Buffer);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Parse a string into an <see cref="IPAddress"/>.
        /// </summary>
        /// <param name="address">The address to parse.</param>
        /// <returns>The IPAddress parsed</returns>
        private static IPAddress ParseAddressIntoIPAddress(string address)
        {
            IPAddress ipaddress;

            if (!IPAddress.TryParse(address, out ipaddress))
            {
                throw new ArgumentException(address);
            }

            return ipaddress;
        }
    }
}