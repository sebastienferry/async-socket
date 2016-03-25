// ----------------------------------------------------------------------------
// "THE BEER-WARE LICENSE" (Revision 42):
// <phk@FreeBSD.ORG> wrote this file.  As long as you retain this notice you
// can do whatever you want with this stuff. If we meet some day, and you think
// this stuff is worth it, you can buy me a beer in return.   Poul-Henning Kamp
// ----------------------------------------------------------------------------

using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ZuperSocket.Core.Awaitable
{
    /// Briefly:
    /// However, there are some scenarios where this is not sufficient,
    /// and not only the additional tasks, but the APM implementation itself.
    /// If you’re making thousands upon thousands of socket calls asynchronously per second,
    /// that’s thousands upon thousands of IAsyncResult objects getting created.
    /// That’s a lot of pressure on the garbage collector,
    /// and can result in unacceptable pauses in some networking-intensive apps.
    /// 
    /// To address that, Socket exposes another set of asynchronous methods.
    /// These methods look similar to the Event-based Async Pattern (EAP),
    /// but they’re subtly different.
    /// Basically, you create a SocketAsyncEventArgs instance,
    /// and you configure that instance with a buffer,
    /// with a Completion event handler, and so on.
    /// 
    /// While a bit more complicated, the benefit of this pattern is
    /// that these SocketAsyncEventArgs instances can be reused.

    /// <summary>
    /// Stephen Stoub awaitable socket.
    /// It doesn't allocate TaskCompletionSource per socket operation.
    /// http://blogs.msdn.com/b/pfxteam/archive/2011/12/15/10248293.aspx
    /// </summary>
    public class AwaitableSaea : INotifyCompletion
    {
        private readonly static Action SENTINEL = () => { };

        internal bool WasCompleted;

        internal Action Continuation;

        internal SocketAsyncEventArgs EventArgs;

        /// <summary>
        /// Build a new SocketAwaitable.
        /// It will wait for the SocketAsyncEventArgs complete event.
        /// </summary>
        /// <param name="eventArgs">SocketAsyncEventArgs to monitor</param>
        internal AwaitableSaea(SocketAsyncEventArgs eventArgs)
        {
            if (eventArgs == null)
            {
                throw new ArgumentNullException("eventArgs");
            }

            EventArgs = eventArgs;

            EventArgs.Completed += delegate
            {
                Action actionThatComesAfter = Continuation ?? Interlocked.CompareExchange(ref Continuation, SENTINEL, null);
                
                if (actionThatComesAfter != null)
                {
                    actionThatComesAfter();
                }
            };
        }

        internal void Reset()
        {
            WasCompleted = false;

            Continuation = null;
        }

        public AwaitableSaea GetAwaiter()
        {
            return this;
        }

        public bool IsCompleted
        {
            get
            {
                return WasCompleted;
            }
        }

        public void OnCompleted(Action continuation)
        {
            if (Continuation == SENTINEL ||
                Interlocked.CompareExchange(
                    ref Continuation, continuation, null) == SENTINEL)
            {
                Task.Run(continuation);
            }
        }

        public void GetResult()
        {
            if (EventArgs.SocketError != SocketError.Success)
                throw new SocketException((int)EventArgs.SocketError);
        }
    }
}