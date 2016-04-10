using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncSocket.Core.IoCompletionPort;

namespace AsyncSocket.Core.Messaging
{
    public static class PeerHelper
    {
        public static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }
    }

    public interface IMessageContext
    {
        
    }

    public interface ITaskMessageHandler
    {
        Task ProcessRequest(IMessageContext context);
    }

    internal class ActionMessageHandler : ITaskMessageHandler
    {
        private readonly Func<IMessageContext, Task> _func;

        public ActionMessageHandler(Func<IMessageContext, Task> func)
        {
            _func = func;
        }

        public async Task ProcessRequest(IMessageContext context)
        {
            await _func(context);
        }
    }

    internal class MessageContext : IMessageContext
    {
        
    }

    public class Peer
    {
        private IPAddress _bindAddress;

        private Socket _socket;

        private IList<Peer> _connectedPeers;

        private readonly IProducerConsumerCollection<AsyncEvent> _events;

        private readonly AsyncSocketWrapper _asyncSocketWrapper;

        private IDictionary<string, ITaskMessageHandler> _handlers; 

        public Peer()
        {
            // Resolve the local IP address to bind to.
            _bindAddress = PeerHelper.GetLocalIPAddress();

            // Create the peer socket.
            _socket = new Socket(
                _bindAddress.AddressFamily,
                SocketType.Stream,
                _bindAddress.AddressFamily == AddressFamily.InterNetworkV6 ? ProtocolType.IPv6 : ProtocolType.IP);

            // Create end-point
            IPEndPoint endPoint = new IPEndPoint(_bindAddress, 5555);

            // Bind to the local IP.
            _socket.Bind(endPoint);

            // Create our event bus.
            _events = new ConcurrentQueue<AsyncEvent>();

            // Init our handlers list
            _handlers = new Dictionary<string, ITaskMessageHandler>();

            // Create our async wrapper around the .NET socket.
            _asyncSocketWrapper = AsyncSocketWrapper.Create(_socket, _events);
        }

        public void Map(string tag, ITaskMessageHandler handler)
        {
            throw new NotImplementedException();
        }

        public void Map(string tag, Func<IMessageContext, Task> handlingFunc)
        {
            _handlers[tag] = new ActionMessageHandler(handlingFunc);
        }

        public Task Start(CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                _socket.Listen(100);

                while (!cancellationToken.IsCancellationRequested)
                {
                    _asyncSocketWrapper.Accept();

                    AsyncEvent asyncEvent = null;

                    if (_asyncSocketWrapper.GetEvent(out asyncEvent))
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
                            string tag = Encoding.UTF8.GetString(asyncEvent.Buffer);

                            // Send data back to the customer.
                            //asyncEvent.Peer.Send(Encoding.UTF8.GetBytes("pong"));

                            ITaskMessageHandler handler;
                            if(_handlers.TryGetValue(tag, out handler))
                            {
                                handler.ProcessRequest(new MessageContext());
                            }
                        }
                    }
                }

            }, cancellationToken);
        }
    }
}
