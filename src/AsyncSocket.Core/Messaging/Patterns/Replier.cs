// ----------------------------------------------------------------------------
// "THE BEER-WARE LICENSE" (Revision 42):
// <phk@FreeBSD.ORG> wrote this file.  As long as you retain this notice you
// can do whatever you want with this stuff. If we meet some day, and you think
// this stuff is worth it, you can buy me a beer in return.   Poul-Henning Kamp
// ----------------------------------------------------------------------------

namespace AsyncSocket.Core.Messaging.Patterns
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using AsyncSocket.Core.IoCompletionPort;
    using AsyncSocket.Core.Net;

    /// <summary>
    /// This class is used in the request response pattern. It handles the response side.
    /// </summary>
    public class Replier
    {
        /// <summary>
        /// Start the replier.
        /// </summary>
        public void Start(string address)
        {
            Address addr = new Address(address);

            ConcurrentQueue<AsyncEvent> events = new ConcurrentQueue<AsyncEvent>();

            // Initialize a new socket.
            using (Socket socket = new Socket(
                addr.IpAddress.AddressFamily,
                SocketType.Stream,
                addr.IpAddress.AddressFamily == AddressFamily.InterNetworkV6 ? ProtocolType.IPv6 : ProtocolType.IP))
            {
                socket.Bind(addr.EndPoint);

                socket.Listen(100);

                AsyncSocketWrapper eventSocket = AsyncSocketWrapper.Create(socket, events);
                
                while (true)
                {
                    eventSocket.Accept();

                    AsyncEvent asyncEvent = null;
                    
                    if (eventSocket.GetEvent(out asyncEvent))
                    {
                        AsyncSocketWrapper asyncSocket = asyncEvent.Peer as AsyncSocketWrapper;
                        
                        if (asyncEvent.Operation == AsyncOperation.NewClient)
                        {
                            // New client. Start to receive data.
                            asyncEvent.Peer.Receive();
                        }

                        if (asyncEvent.Operation == AsyncOperation.DataReceived)
                        {
                            // Data received.
                            string message = Encoding.UTF8.GetString(asyncEvent.Buffer);

                            Console.WriteLine(message);

                            // Send data back to the customer.
                            asyncEvent.Peer.Send(Encoding.UTF8.GetBytes("pong"));
                        }
                    }
                }
            }
        }
    }
}