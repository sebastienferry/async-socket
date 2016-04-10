// ----------------------------------------------------------------------------
// "THE BEER-WARE LICENSE" (Revision 42):
// <phk@FreeBSD.ORG> wrote this file.  As long as you retain this notice you
// can do whatever you want with this stuff. If we meet some day, and you think
// this stuff is worth it, you can buy me a beer in return.   Poul-Henning Kamp
// ----------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace AsyncSocket.ConsoleHost
{
    using System;
    using System.Diagnostics;
    using AsyncSocket.Core.Messaging.Patterns;
    using AsyncSocket.Core.Messaging;

    /// <summary>
    /// Server console.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Server entry point.
        /// </summary>
        public static void Main()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            Peer peer = new Peer();

            peer.Map("ping", ( async context =>
            {
                Console.WriteLine("new ping message");
                await Task.Yield();
            }));

            CancellationToken ct = new CancellationToken();

            peer.Start(ct).Wait();

            Console.ReadLine();
        }
    }
}
