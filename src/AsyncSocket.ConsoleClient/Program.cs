// ----------------------------------------------------------------------------
// "THE BEER-WARE LICENSE" (Revision 42):
// <phk@FreeBSD.ORG> wrote this file.  As long as you retain this notice you
// can do whatever you want with this stuff. If we meet some day, and you think
// this stuff is worth it, you can buy me a beer in return.   Poul-Henning Kamp
// ----------------------------------------------------------------------------

namespace ZuperSocket.ConsoleClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using ZuperSocket.Core;
    using ZuperSocket.Core.Messaging.Patterns;

    /// <summary>
    /// Client in a console.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Client console entry point.
        /// </summary>
        static void Main()
        {
            Requester requester = new Requester();

            requester.Do();

            Console.ReadLine();
        }
    }
}
