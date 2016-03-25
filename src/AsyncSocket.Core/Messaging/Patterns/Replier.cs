// ----------------------------------------------------------------------------
// "THE BEER-WARE LICENSE" (Revision 42):
// <phk@FreeBSD.ORG> wrote this file.  As long as you retain this notice you
// can do whatever you want with this stuff. If we meet some day, and you think
// this stuff is worth it, you can buy me a beer in return.   Poul-Henning Kamp
// ----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace ZuperSocket.Core.Messaging.Patterns
{
    using System.Threading.Tasks;
    using ZuperSocket.Core.IoCompletionPort;

    public class Replier
    {
        private static IPAddress ParseAddressIntoIPAddress(string address)
        {
            IPAddress ipAddress;

            if (!IPAddress.TryParse(address, out ipAddress))
            {
                throw new ArgumentException(address);
            }

            return ipAddress;
        }

        /// <summary>
        /// Start the reponder.
        /// </summary>
        public void Start()
        {
            // Prepare IP address to bind to.
            IPAddress ipAddress = ParseAddressIntoIPAddress("127.0.0.1");

            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 5555);

            ConcurrentQueue<AsyncEvent> events = new ConcurrentQueue<AsyncEvent>();

            // Initialize a new socket.
            using (Socket socket = new Socket(ipAddress.AddressFamily, SocketType.Stream,
                ipAddress.AddressFamily == AddressFamily.InterNetworkV6 ? ProtocolType.IPv6 : ProtocolType.IP))
            {
                socket.Bind(ipEndPoint);

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
    }

        //public void AddReactor(IReactor<T> reactor)
        //{
        //    _reactors.Add(reactor);
        //}
        
        //private void HandleReceive(T request)
        //{
        //    IReactor<T> last = null;
            
        //    foreach (IReactor<T> reactor in _reactors)
        //    {
        //        reactor.React(request);

        //        last = reactor;
        //    }

        //    T response = last.GetOutput();

        //    SendResponse(response);
        //}

        //public void SendResponse(T response)
        //{
            
        //}
}