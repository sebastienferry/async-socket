using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ZuperSocket.Core.Messaging.Patterns;
using ZuperSocket.Core; //for testing

namespace ZuperSocket.ConsoleHost
{
    class Program
    {
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            Replier replyer = new Replier();

            replyer.Start();
            
            Console.ReadLine();
        }
    }
}
