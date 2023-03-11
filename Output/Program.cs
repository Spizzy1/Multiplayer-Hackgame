using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Output
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Program started");
            Int32 port = 3856;
            IPAddress localAddr = IPAddress.Parse("127.0.0.1");
            TcpListener server = new TcpListener(localAddr, port);
            server.Start();
            TcpClient localClient = null;

            try
            {
                Console.WriteLine("Waiting for connection...");

                localClient = server.AcceptTcpClient();
                Console.WriteLine("Connected!");
            }
            catch
            {

            }
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Clear();
            Console.WriteLine("Output...");
            NetworkStream _stream = localClient.GetStream();
            while (true)
            {
                try
                {
                    byte[] _data = new byte[1025];
                    if (!localClient.GetStream().DataAvailable)
                    {
                        Thread.Sleep(1);
                    }
                    else
                    {
                        int readData = _stream.Read(_data, 0, _data.Length);
                        if (readData > 0)
                        {
                            switch (_data[0])
                            {
                                case 1:
                                    Console.WriteLine("Health: " + _data[1]);
                                    break;
                                case 2:
                                    Console.WriteLine("Secret");
                                    break;
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }
            }

        }
    }
}
