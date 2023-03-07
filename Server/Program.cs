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
            TcpClient client1 = null;
            TcpClient client2 = null;
                try
                {
                    Console.WriteLine("Waiting for connection...");

                    client1 = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");
                }
                catch
                {

                }
            while (!client1.Connected)
            {

            }
            if (!client1.Connected)
            {
                server.Stop();
            }
            byte[] client1Data = new byte[1024];
            byte[] client2Data = new byte[1024];

            //Initializing multithread stuff
            Thread checkThread = new Thread(checkConnection);
            Thread client1Read = new Thread(() => readServer(client1, client1Data));
            Thread client1Write = new Thread(() => writeServer(client1, client1Data));
            //Thread client2Read = new Thread(() => readServer(client1));
            //Thread client2Write = new Thread(() => writeServer(client2));

            client1Read.Start();
            client1Write.Start();
            //client2Read.Start();
            //client2Write.Start();
            checkThread.Start();

            void readServer(TcpClient client, byte[] _data)
            {
                NetworkStream _stream = client.GetStream();
                while (true)
                {
                    try
                    {
                        if (!client1.GetStream().DataAvailable)
                        {
                            Thread.Sleep(1);
                        }
                        else
                        {
                            int readData = _stream.Read(_data, 0, _data.Length);
                            StringBuilder myCompleteMessage = new StringBuilder();
                            if (readData > 0)
                            {
                                switch (_data[0])
                                {
                                    case 23:
                                        Console.WriteLine("test recieved!!");
                                        break;
                                }
                                Console.WriteLine(myCompleteMessage);
                            }
                        }
                    }
                    catch
                    {

                    }
                }
            }

            void writeServer(TcpClient client, byte[] _data)
            {
                client.GetStream().Write(new byte[] { 1, 2, 3 }, 0, 3);
            }
            void checkConnection()
            {
                while (true)
                {
                    if (!client1.Connected)
                    {
                        Console.WriteLine("wha?");
                        server.Stop();
                        Environment.Exit(0);
                    }
                }
            }
            async void test()
            {

            }
        }
    }
}
