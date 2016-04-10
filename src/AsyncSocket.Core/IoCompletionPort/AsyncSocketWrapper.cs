using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using AsyncSocket.Core.Patterns;

namespace AsyncSocket.Core.IoCompletionPort
{
    /// <summary>
    /// This class is a wrapper for .NET Socket + SocketAsyncEventArgs.
    /// </summary>
    public class AsyncSocketWrapper : IAsyncSocket
    {
        /// <summary>
        /// .NET Socket instance used internally.
        /// </summary>
        private readonly Socket _socket;

        /// <summary>
        /// A collection that is used a a message queue.
        /// </summary>
        private readonly IProducerConsumerCollection<AsyncEvent> _events;

        /// <summary>
        /// SocketAsyncEventArgs use for accept.
        /// Only one new client accepted at a time.
        /// As we try to accept continuously, 
        /// having more that one object doesn't make sense because we would run
        /// out of available SAEA really fast.
        /// </summary>
        private SocketAsyncEventArgs _acceptSocketAsyncEventArgs;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Pool of SocketAsyncEventArgs to use for socket async operations.
        /// </summary>
        private readonly Pool<SocketAsyncEventArgs> _socketAsyncEventArgsPool;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncSocketWrapper"/> class.
        /// </summary>
        /// <param name="socket">.NET Socket to use.</param>
        /// <param name="socketAsyncEventArgsPool">Pool of SAEA for operations.</param>
        /// <param name="events">A message queue to publish to.</param>
        private AsyncSocketWrapper(
            Socket socket,
            Pool<SocketAsyncEventArgs> socketAsyncEventArgsPool,
            IProducerConsumerCollection<AsyncEvent> events)
        {
            _socket = socket;

            _socketAsyncEventArgsPool = socketAsyncEventArgsPool;

            _events = events;
        }

        /// <summary>
        /// A factory method used to initialize a new <see cref="AsyncSocketWrapper"/> wrapper.
        /// </summary>
        /// <param name="socket">.NET Socket to use</param>
        /// <param name="events">A message queue to publish to.</param>
        /// <returns>The newly created instance.</returns>
        public static AsyncSocketWrapper Create(
            Socket socket,
            IProducerConsumerCollection<AsyncEvent> events,
            int socketAsyncEventArgsPoolSize = 20,
            int individualSocketAsyncEventArgsBufferSizeInBytes = 4096)
        {
            // Create a pool of SocketAcceptEventArgs for the async operations.
            Pool<SocketAsyncEventArgs> socketAsyncEventArgsPool = new Pool<SocketAsyncEventArgs>();

            // Create a large buffer that we will devide into small part of 4kb to handle async operation.
            // This buffer is access by unmanaged code and allocating it for every operation might result
            // in heap memory fragmentation. That's why we are using a contiguous bunch of data.
            SaeaSharedBuffer sharedHeapBuffer = new SaeaSharedBuffer(individualSocketAsyncEventArgsBufferSizeInBytes, socketAsyncEventArgsPoolSize);
            
            // Create the socket to initialize
            AsyncSocketWrapper serverSocket = new AsyncSocketWrapper(socket, socketAsyncEventArgsPool, events);

            // Accept SocketAsyncEventArgs
            serverSocket._acceptSocketAsyncEventArgs = new SocketAsyncEventArgs();
            serverSocket._acceptSocketAsyncEventArgs.Completed += serverSocket.OperationCompleted;

            // Let's start with 20 simultaneous accept operation.
            foreach (int index in Enumerable.Range(0, socketAsyncEventArgsPoolSize))
            {
                // Create a new Saea
                SocketAsyncEventArgs eventArgs = new SocketAsyncEventArgs();

                // Attach our completion event handler
                eventArgs.Completed += serverSocket.OperationCompleted;

                sharedHeapBuffer.AssignBuffer(eventArgs);

                // Attach user token.
                eventArgs.UserToken = new UserToken(eventArgs.Offset, serverSocket);

                // Push it to the pool
                socketAsyncEventArgsPool.Push(eventArgs);
            }

            return serverSocket;
        }
        
        /// <summary>
        /// Gets the next available async events from the message queue.
        /// </summary>
        /// <param name="asyncEvent">Async event to initialize.</param>
        /// <returns>False if no event to pop.</returns>
        public bool GetEvent(out AsyncEvent asyncEvent)
        {
            return _events.TryTake(out asyncEvent);
        }
        
        /// <summary>
        /// Accept a new client. This operation is non-blocking.
        /// </summary>
        public void Accept()
        {
            // Avoid to have more that one accepted connection at the same time.
            // This slow down the main loop if we are blocked :/
            // We might want the accept to run on its own task...
            if (_semaphore.CurrentCount <= 0 || !_semaphore.Wait(1))
            {
                return;
            }
            
            // Returns false if the I/O operation completed synchronously.
            // The SocketAsyncEventArgs.Completed event on the e parameter
            // will not be raised and the e object passed as a parameter
            // may be examined immediately after the method call returns
            // to retrieve the result of the operation.
            bool pendingOperation = _socket.AcceptAsync(_acceptSocketAsyncEventArgs);

            if (!pendingOperation)
            {
                HandleAccept(_acceptSocketAsyncEventArgs);
            }
        }

        /// <summary>
        /// Connect to the remote host.
        /// </summary>
        /// <param name="endPoint">End-point to connect to.</param>
        public void Connect(EndPoint endPoint)
        {
            SocketAsyncEventArgs eventArgs = _socketAsyncEventArgsPool.Pop();

            if (eventArgs == null)
            {
                return;
            }

            eventArgs.RemoteEndPoint = endPoint;

            bool pendingOperation = _socket.ConnectAsync(eventArgs);

            if (!pendingOperation)
            {
                HandleConnect(eventArgs);
            }
        }

        /// <summary>
        /// Sends a bunch of bytes.
        /// </summary>
        /// <param name="bytes">Array of bytes to send.</param>
        public void Send(byte[] bytes)
        {
            SocketAsyncEventArgs eventArgs = _socketAsyncEventArgsPool.Pop();

            if (eventArgs == null)
            {
                return;
            }

            UserToken userToken = eventArgs.UserToken as UserToken;

            // Copy the data to the buffer
            Buffer.BlockCopy(bytes, 0, eventArgs.Buffer, eventArgs.Offset, bytes.Length);

            // Just to set the Count value
            eventArgs.SetBuffer(eventArgs.Offset, bytes.Length);
            
            //Buffer.BlockCopy(bytes, 0, eventArgs.Buffer, eventArgs.Offset, bytes.Length);

            bool pendingOperation = _socket.SendAsync(eventArgs);

            if (!pendingOperation)
            {
                HandleSend(eventArgs);
            }
        }

        /// <summary>
        /// Receive data.
        /// </summary>
        public void Receive()
        {
            // Get a reusable Saea.
            SocketAsyncEventArgs eventArgs = _socketAsyncEventArgsPool.Pop();

            if (eventArgs == null)
            {
                return;
            }

            bool pendingOperation = _socket.ReceiveAsync(eventArgs);
            
            if (!pendingOperation)
            {
                HandleReceive(eventArgs);
            }
        }

        /// <summary>
        /// Clean-up a SocketAsyncEventArgs instance before pooling it back to the pool
        /// </summary>
        /// <param name="eventArgs">SocketAsyncEventArgs to pool back.</param>
        private void CleanUp(SocketAsyncEventArgs eventArgs)
        {
            eventArgs.RemoteEndPoint = null;
            eventArgs.AcceptSocket = null;
        }
        
        /// <summary>
        /// Callback triggered when async operation complete.
        /// </summary>
        /// <param name="sender">The object that call the event.</param>
        /// <param name="eventArgs">The SAEA instance used to monitor async operation.</param>
        private void OperationCompleted(object sender, SocketAsyncEventArgs eventArgs)
        {
            // Check for errors.
            if (eventArgs.SocketError != SocketError.Success)
            {
                // Close the socket.
                if (eventArgs.LastOperation == SocketAsyncOperation.Accept)
                {
                    eventArgs.AcceptSocket.Close();    
                }

                // Release the event args instance
                CleanUp(eventArgs);
                _socketAsyncEventArgsPool.Push(eventArgs);

                return;
            }
            
            if (eventArgs.LastOperation == SocketAsyncOperation.Accept)
            {
                HandleAccept(eventArgs);
            }

            if (eventArgs.LastOperation == SocketAsyncOperation.Connect)
            {
                HandleConnect(eventArgs);
            }

            if (eventArgs.LastOperation == SocketAsyncOperation.Receive)
            {
                HandleReceive(eventArgs);
            }

            if (eventArgs.LastOperation == SocketAsyncOperation.Send)
            {
                HandleSend(eventArgs);
            }
        }

        /// <summary>
        /// Handle a SAEA event for accept operation.
        /// </summary>
        /// <param name="acceptEventArgs">SAEA event associated to the async accept.</param>
        private void HandleAccept(SocketAsyncEventArgs acceptEventArgs)
        {
            // Backup the client socket
            Socket clientSocket = acceptEventArgs.AcceptSocket;

            // Create a new event socket
            AsyncSocketWrapper client = new AsyncSocketWrapper(clientSocket, _socketAsyncEventArgsPool, _events);

            // Pool back the accept event args to the pool
            CleanUp(acceptEventArgs);            

            // Enqueue a message for the server
            _events.TryAdd(new AsyncEvent(AsyncOperation.NewClient, client));

            _semaphore.Release();
        }
        
        /// <summary>
        /// Callback that is call when a connect operation complete.
        /// </summary>
        /// <param name="eventArgs">SocketAsyncEventArgs associated with the event.</param>
        private void HandleConnect(SocketAsyncEventArgs eventArgs)
        {
            _events.TryAdd(new AsyncEvent(AsyncOperation.ConnectionAccepted, this));

            CleanUp(eventArgs);
            
            _socketAsyncEventArgsPool.Push(eventArgs);
        }
        
        /// <summary>
        /// Callback that is call when a connect operation complete.
        /// </summary>
        /// <param name="eventArgs">SocketAsyncEventArgs associated with the event.</param>
        private void HandleSend(SocketAsyncEventArgs eventArgs)
        {
            _events.TryAdd(new AsyncEvent(AsyncOperation.DataSent, this));

            _socketAsyncEventArgsPool.Push(eventArgs);
        }

        /// <summary>
        /// Callback that is call when a receive operation complete.
        /// </summary>
        /// <param name="eventArgs">SocketAsyncEventArgs associated with the event.</param>
        private void HandleReceive(SocketAsyncEventArgs eventArgs)
        {
            UserToken userToken = eventArgs.UserToken as UserToken;

            if (userToken != null)
            {
                if (Protocol.TryReadMessage(
                    eventArgs.Buffer,
                    eventArgs.Offset,
                    eventArgs.BytesTransferred,
                    ref userToken.IncomingMessage))
                {
                    AsyncEvent receivedEvent = new AsyncEvent(AsyncOperation.DataReceived, this, eventArgs.BytesTransferred);

                    Buffer.BlockCopy(
                        userToken.IncomingMessage.Body.FrameBytes, 0,
                        receivedEvent.Buffer, 0,
                        userToken.IncomingMessage.Body.BytesReceived);

                    CleanUp(eventArgs);

                    _socketAsyncEventArgsPool.Push(eventArgs);

                    _events.TryAdd(receivedEvent);
                }
            }
        }
    }
}