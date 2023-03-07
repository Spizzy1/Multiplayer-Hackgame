using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Program started");
            Int32 port = 13000;
            IPAddress localAddr = IPAddress.Parse("127.0.0.1");

            // TcpListener server = new TcpListener(port);
            TcpListener server = new TcpListener(localAddr, port);
            server.Start();
            Console.WriteLine("Waiting for connection...");
            TcpClient client = null;
            TcpClient client2 = null;
            while (true)
            {
                try
                {
                    Console.WriteLine("Waiting for connection...");

                    client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");
                    break;
                }
                catch
                {

                }
            }
            while (!client.Connected)
            {

            }
            if (!client.Connected)
            {
                server.Stop();
            }
            Thread checkThread = new Thread(checkConnection);
            checkThread.Start();
            byte i = 0;
            while (true)
            {
                Console.WriteLine(i);
                Thread.Sleep(1000);
            }
            void checkConnection()
            {
                while (true)
                {
                    if (!client.Connected)
                    {
                        server.Stop();
                        Environment.Exit(0);
                    }
                }
            }
        }
    }
}
