// ----------------------------------------------------------------------------
// "THE BEER-WARE LICENSE" (Revision 42):
// <phk@FreeBSD.ORG> wrote this file.  As long as you retain this notice you
// can do whatever you want with this stuff. If we meet some day, and you think
// this stuff is worth it, you can buy me a beer in return.   Poul-Henning Kamp
// ----------------------------------------------------------------------------

namespace ZuperSocket.ConsoleHost
{
    using System;
    using System.Diagnostics;
    using ZuperSocket.Core.Messaging.Patterns;

    /// <summary>
    /// Server console.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Server entry point.
        /// </summary>
        static void Main()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            Replier replyer = new Replier();

            replyer.Start();
            
            Console.ReadLine();
        }
    }
}
