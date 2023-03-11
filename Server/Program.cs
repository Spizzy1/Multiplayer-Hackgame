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
            Int32 port = 1300;
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
            int health = 100;
            int health2 = 100;
            //Initializing multithread stuff
            Thread checkThread = new Thread(checkConnection);
            Thread client1Read = new Thread(() => readServer(client1));
            //Thread client1Write = new Thread(() => writeServer(client1));
            //Thread client2Read = new Thread(() => readServer(client1));
            //Thread client2Write = new Thread(() => writeServer(client2));

            client1Read.Start();
            //client1Write.Start();
            //client2Read.Start();
            //client2Write.Start();
            checkThread.Start();

            void readServer(TcpClient client)
            {
                NetworkStream _stream = client.GetStream();
                while (true)
                {
                    try
                    {
                        byte[] _data = new byte[255];
                        if (!client.GetStream().DataAvailable)
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
                                    case 23:
                                        Console.WriteLine("test recieved!!");
                                        break;
                                    case 1:
                                        writeServer(client, 1);
                                        break;
                                    case 200:
                                        Console.WriteLine("Client alive");
                                        break;
                                }
                            }
                        }
                    }
                    catch
                    {

                    }
                }
            }

            void writeServer(TcpClient client, byte instruction)
            {
                Console.WriteLine("Input recieved: " + instruction);
                switch (instruction)
                {
                    case 1:
                        client.GetStream().Write(new byte[] { 1, (byte)health }, 0, 2);
                        break;
                }
            }
            void checkConnection()
            {
                while (true)
                {
                    try
                    {
                        client1.GetStream().Write(new byte[]{200}, 0, 1);
                    }
                    catch (Exception ex)
                    {
                        server.Stop();
                        Environment.Exit(0);
                    }
                    Thread.Sleep(5000);
                }

            }
            async void test()
            {

            }
        }
    }
}
