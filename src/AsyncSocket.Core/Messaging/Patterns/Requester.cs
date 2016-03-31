// ----------------------------------------------------------------------------
// "THE BEER-WARE LICENSE" (Revision 42):
// <phk@FreeBSD.ORG> wrote this file.  As long as you retain this notice you
// can do whatever you want with this stuff. If we meet some day, and you think
// this stuff is worth it, you can buy me a beer in return.   Poul-Henning Kamp
// ----------------------------------------------------------------------------

using System.Threading;

namespace AsyncSocket.Core.Messaging.Patterns
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;
    using AsyncSocket.Core.IoCompletionPort;
    
    /// <summary>
    /// Allows to handle request-response messaging pattern over TCP.
    /// A <see cref="Requester"/> must be used on the request side and it communicates with a <seealso cref="Replier"/>.
    /// </summary>
    public class Requester : IDisposable
    {
        /// <summary>
        /// .NET Socket used internally.
        /// </summary>
        private readonly Socket _socket;

        /// <summary>
        /// Reference to a message-queue like collection.
        /// </summary>
        private readonly IProducerConsumerCollection<AsyncEvent> _events;

        /// <summary>
        /// IP Address to connect to.
        /// </summary>
        private readonly IPAddress _ipaddress;

        /// <summary>
        /// TCP Port to connect to.
        /// </summary>
        private readonly int _port;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Requester"/> class.
        /// </summary>
        public Requester(IPAddress ipaddress, int port)
        {
            _ipaddress = ipaddress;

            _port = port;

            _events = new ConcurrentQueue<AsyncEvent>();

            _socket = new Socket(_ipaddress.AddressFamily, SocketType.Stream, ProtocolType.IP);
        }
        
        /// <summary>
        /// Start the client.
        /// </summary>
        /// <param name="func">A delegate to execute each time an event is received.</param>
        public async Task Start(Func<AsyncEvent, Task> callback, int pollingIntervalInMsecs, CancellationToken cancellationToken)
        {
            EndPoint endpoint = new IPEndPoint(_ipaddress, _port);

            AsyncSocketWrapper clientSocket = AsyncSocketWrapper.Create(_socket, _events);

            clientSocket.Connect(endpoint);

            await Task.Factory.StartNew(async () =>
            {
                bool loop = true;

                while (loop)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    AsyncEvent asyncEvent = default(AsyncEvent);

                    if (_events.TryTake(out asyncEvent))
                    {
                        await callback(asyncEvent);
                    }

                    // Try to receive the next data.
                    clientSocket.Receive();

                    Task.Delay(pollingIntervalInMsecs).Wait();
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Requester implements IDisposable since it uses a .NET socket.
        /// </summary>
        public void Dispose()
        {
            _socket.Dispose();
        }
    }
}