using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZuperSocket.Core;
using ZuperSocket.Core.Messaging.Patterns;

namespace ZuperSocket.ConsoleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Requester requester = new Requester();

            requester.Do();

            Console.ReadLine();
        }
    }
}
