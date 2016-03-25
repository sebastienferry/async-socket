// ----------------------------------------------------------------------------
// "THE BEER-WARE LICENSE" (Revision 42):
// <phk@FreeBSD.ORG> wrote this file.  As long as you retain this notice you
// can do whatever you want with this stuff. If we meet some day, and you think
// this stuff is worth it, you can buy me a beer in return.   Poul-Henning Kamp
// ----------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using ZuperSocket.Core.Patterns;

namespace ZuperSocket.Core.IoCompletionPort
{
    public class AsyncSocket
    {
        private readonly Socket _socket;

        private readonly IProducerConsumerCollection<AsyncEvent> _events;

        private Pool<SocketAsyncEventArgs> _sendRecEventArgsPool;

        private Pool<SocketAsyncEventArgs> _acceptEventArgsPool;

        private AsyncSocket(Socket socket, Pool<SocketAsyncEventArgs> acceptEventArgsPool,
            Pool<SocketAsyncEventArgs> sendReceiveEventArgsPool, IProducerConsumerCollection<AsyncEvent> events)
        {
            _socket = socket;

            _acceptEventArgsPool = acceptEventArgsPool;

            _sendRecEventArgsPool = sendReceiveEventArgsPool;

            _events = events;
        }

        public static AsyncSocket Create(Socket socket, IProducerConsumerCollection<AsyncEvent> events)
        {
            // Create a pool of SocketAcceptEventArgs for the send / receive operations.
            // This is maximum number of message we can handle at the same time.
            Pool<SocketAsyncEventArgs> sendRecEventArgsPool = new Pool<SocketAsyncEventArgs>();

            // Do we need a pool for accept operation ?
            Pool<SocketAsyncEventArgs> acceptEventArgsPool = new Pool<SocketAsyncEventArgs>();

            // Create the socket to initialize
            AsyncSocket serverSocket = new AsyncSocket(socket, acceptEventArgsPool, sendRecEventArgsPool, events);
            
            // Let's start with 20 simultaneous accept operation.
            foreach (int index in Enumerable.Range(0, 20))
            {
                // Create a new Saea
                SocketAsyncEventArgs acceptEventArgs = new SocketAsyncEventArgs();

                // Attach our completion event handler
                acceptEventArgs.Completed += serverSocket.OperationCompleted;

                // Push it to the pool
                acceptEventArgsPool.Push(acceptEventArgs);
            }

            // Create a large buffer that we will devide into small part of 4kb to handle async operation.
            // This buffer is access by unmanaged code and allocating it for every operation might result
            // in heap memory fragmentation. That's why we are using a contiguous bunch of data.
            SaeaSharedBuffer sharedHeapBuffer = new SaeaSharedBuffer(4096, 100);

            // Same size as the number of buffer chunks we have.
            foreach (int index in Enumerable.Range(0, sharedHeapBuffer.PoolSize))
            {
                SocketAsyncEventArgs sendReceiveAsyncEventArgs = new SocketAsyncEventArgs();

                sendReceiveAsyncEventArgs.Completed += serverSocket.OperationCompleted;

                sharedHeapBuffer.AssignBuffer(sendReceiveAsyncEventArgs);

                sendRecEventArgsPool.Push(sendReceiveAsyncEventArgs);
            }

            return serverSocket;
        }
        
        private void OperationCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.LastOperation == SocketAsyncOperation.Accept)
            {
                HandleAccept(args);
            }

            if (args.LastOperation == SocketAsyncOperation.Receive)
            {
                Trace.WriteLine("Receive SocketAsyncOperation");

                HandleReceive(args);
            }

            if (args.LastOperation == SocketAsyncOperation.Send)
            {
                HandleSend(args);
            }
        }

        public bool GetEvent(out AsyncEvent asyncEvent)
        {
            return _events.TryTake(out asyncEvent);
        }
        

        /// <summary>
        /// Accept a new client. This operation is non-blocking.
        /// </summary>
        public void Accept()
        {
            // Reuse an existing event args.
            SocketAsyncEventArgs acceptEventArgs = _acceptEventArgsPool.Pop();

            // No Saea available for the moment. Shall we create a new one ?
            if (acceptEventArgs == null)
                return;

            // Returns false if the I/O operation completed synchronously.
            // The SocketAsyncEventArgs.Completed event on the e parameter
            // will not be raised and the e object passed as a parameter
            // may be examined immediately after the method call returns
            // to retrieve the result of the operation.
            bool pendingOperation = _socket.AcceptAsync(acceptEventArgs);

            if (!pendingOperation)
            {
                HandleAccept(acceptEventArgs);
            }
        }

        private void HandleAccept(SocketAsyncEventArgs acceptEventArgs)
        {
            // Check for errors.
            if (acceptEventArgs.SocketError != SocketError.Success)
            {
                // Close the socket.
                acceptEventArgs.AcceptSocket.Close();

                // Release the event args instance
                _acceptEventArgsPool.Push(acceptEventArgs);

                return;
            }

            // Backup the client socket
            Socket clientSocket = acceptEventArgs.AcceptSocket;

            // Create a new event socket
            AsyncSocket client = new AsyncSocket(clientSocket, _acceptEventArgsPool, _sendRecEventArgsPool, _events);

            // Pool back the accept event args to the pool
            acceptEventArgs.AcceptSocket = null;
            _acceptEventArgsPool.Push(acceptEventArgs);

            // Enqueue a message for the server
            _events.TryAdd(new AsyncEvent(AsyncOperation.NewConnection, client));
        }

        
        /// <summary>
        /// Connecto to the remote host.
        /// </summary>
        /// <param name="connectCallback">A callback to execute in case of succesfull connection</param>
        public void Connect(EndPoint endPoint, Action<AsyncSocket> connectCallback)
        {
            // For client side socket, no need to use a pool for connect operation.
            SocketAsyncEventArgs socketAsyncEventArgs = new SocketAsyncEventArgs();

            socketAsyncEventArgs.RemoteEndPoint = endPoint;

            socketAsyncEventArgs.Completed += (sender, args) =>
            {
                // Check for errors.
                if (args.SocketError != SocketError.Success)
                {
                    // Close the socket.
                    _socket.Close();

                    return;
                }

                // Connection is ok, ready to send something.
                connectCallback(this);
            };

            bool pendingOperation = _socket.ConnectAsync(socketAsyncEventArgs);

            if (!pendingOperation)
            {
                connectCallback(this);
            }
        }

        /// <summary>
        /// Send.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="sendCallback"></param>
        public void Send(byte[] bytes)
        {
            SocketAsyncEventArgs eventArgs = _sendRecEventArgsPool.Pop();

            if (eventArgs == null)
                return;

            UserToken userToken = eventArgs.UserToken as UserToken;
            
            Buffer.BlockCopy(bytes, 0, eventArgs.Buffer, userToken.HeapBufferOffset, bytes.Length);

            eventArgs.SetBuffer(bytes, 0, bytes.Length);

            bool pendingOperation = _socket.SendAsync(eventArgs);

            if (!pendingOperation)
            {
                HandleSend(eventArgs);
            }
        }

        private void HandleSend(SocketAsyncEventArgs eventArgs)
        {
            // Check for errors.
            if (eventArgs.SocketError != SocketError.Success)
            {
                // Close the socket.
                eventArgs.AcceptSocket.Close();

                // Release the event args instance
                _sendRecEventArgsPool.Push(eventArgs);

                return;
            }

            UserToken userToken = eventArgs.UserToken as UserToken;

            _events.TryAdd(new AsyncEvent(AsyncOperation.DataSent, userToken.Socket));

            _sendRecEventArgsPool.Push(eventArgs);

        }

        public void Receive()
        {
            // Get a reusable Saea.
            SocketAsyncEventArgs eventArgs = _sendRecEventArgsPool.Pop();

            if (eventArgs == null)
                return;

            // We pass the socket
            UserToken userToken = eventArgs.UserToken as UserToken;

            userToken.Socket = this;

            bool pendingOperation = _socket.ReceiveAsync(eventArgs);
            
            if (!pendingOperation)
            {
                HandleReceive(eventArgs);
            }
        }

        private void HandleReceive(SocketAsyncEventArgs receiveEventArgs)
        {
            // Check for errors.
            if (receiveEventArgs.SocketError != SocketError.Success)
            {
                // Close the socket.
                receiveEventArgs.AcceptSocket.Close();

                // Release the event args instance
                _sendRecEventArgsPool.Push(receiveEventArgs);

                return;
            }

            UserToken userToken = receiveEventArgs.UserToken as UserToken;

            AsyncEvent receivedEvent = new AsyncEvent(AsyncOperation.DataReceived,
                userToken.Socket, receiveEventArgs.BytesTransferred);

            Buffer.BlockCopy(receiveEventArgs.Buffer, userToken.HeapBufferOffset,
                receivedEvent.Buffer, 0, receiveEventArgs.BytesTransferred);
            
            _events.TryAdd(receivedEvent);

            _sendRecEventArgsPool.Push(receiveEventArgs);
        }


        ///// <summary>
        ///// Task based send.
        ///// </summary>
        ///// <param name="bytes">TODO</param>
        ///// <returns>TODO</returns>
        //public async Task SendAsync(byte[] bytes)
        //{
        //    AwaitableSaea awaitableSaea;

        //    if (_sendRecEventArgsPool.Count > 0)
        //    {
        //        // Do we have an available accept SAEA ?
        //        awaitableSaea = _awaitableSendReceivePool.Pop();
        //    }
        //    else
        //    {
        //        // Create a new one.
        //        awaitableSaea = new AwaitableSaea(new SocketAsyncEventArgs());
        //    }

        //    // Here, we need to copy the data to send to the send buffer.
        //    awaitableSaea.EventArgs.SetBuffer(bytes, 0, bytes.Length);

        //    await _socket.SendAsync(awaitableSaea);

        //    _awaitableSendReceivePool.Push(awaitableSaea);
        //}

        #region TPM
        ///// <summary>
        ///// TPM based accept.
        ///// </summary>
        ///// <returns>The accepted socket</returns>
        //public async Task<AsyncSocketWrapper> AcceptAsync()
        //{
        //    AwaitableAccept awaitableAccept;

        //    if (_awaitableAcceptPool.Count > 0)
        //    {
        //        // Do we have an available socket ?
        //        awaitableAccept = _awaitableAcceptPool.Pop();
        //    }
        //    else
        //    {
        //        // Create a new one.
        //        awaitableAccept = new AwaitableAccept(new SocketAsyncEventArgs());
        //    }

        //    await _socket.AcceptAsync(awaitableAccept);

        //    AsyncSocketWrapper clientSocket = new AsyncSocketWrapper(awaitableAccept.GetResult(), _sharedHeapBuffer);

        //    _awaitableAcceptPool.Push(awaitableAccept);

        //    return clientSocket;
        //} 
        #endregion
    }
}