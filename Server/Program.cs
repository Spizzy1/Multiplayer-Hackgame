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
            //Initializing multithread stuff
            Thread waitConnection = new Thread(() => awaitConnection());
            //Thread client1Write = new Thread(() => writeServer(client1));
            //Thread client2Read = new Thread(() => readServer(client1));
            //Thread client2Write = new Thread(() => writeServer(client2));

            waitConnection.Start();
            //client1Write.Start();
            //client2Read.Start();
            //client2Write.Start();
            void awaitConnection()
            {
                while (true)
                {
                    try
                    {
                        TcpClient client = server.AcceptTcpClient();
                        Thread readClient = new Thread(() => readServer(client));
                        readClient.Start();
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
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
                switch (instruction)
                {
                    case 1:
                        client.GetStream().Write(new byte[] { 1 }, 0, 2);
                        break;
                }
            }

            async void test()
            {

            }
        }
        class Room
        {
            public Room(int maxPlayers, TcpListener server)
            {
                _maxPlayers = maxPlayers;
                _state = RoomState.instantiated;
                _clients = new SafeList<ConnectedClient>();
                _server = server;
            }
            void initiate(TcpClient host)
            {
                _state = RoomState.settup;
                addPlayer(host);
            }
            void addPlayer(TcpClient client)
            {
                if(_state != RoomState.playing && _clients.Count > _maxPlayers)
                {
                    _clients.Add(new ConnectedClient(client));
                }
            }
            void playerDisconnect()
            {

            }
            internal class ConnectedClient
            {
                public ConnectedClient(TcpClient client)
                {
                    _client = client;
                    health = 100;
                }
                void writeClient(byte instruction)
                {
                    Console.WriteLine("Input recieved: " + instruction);
                    switch (instruction)
                    {
                        case 1:
                            Client.GetStream().Write(new byte[] { 1, (byte)Health }, 0, 2);
                            break;
                    }
                }
                void readRoom()
                {
                    NetworkStream _stream = Client.GetStream();
                    while (true)
                    {
                        try
                        {
                            byte[] _data = new byte[255];
                            if (!Client.GetStream().DataAvailable)
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
                                            writeClient(1);
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
                private TcpClient _client;
                public TcpClient Client { get { return _client; } }
                private int health;
                public int Health { get { return health; } set { health = value; } }

            }
            void checkConnection()
            {
                while (true)
                {
                    foreach (var client in _clients.getCopyOfInternalList())
                    {
                        try
                        {
                            client.Client.GetStream().Write(new byte[] { 200 }, 0, 1);
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    Thread.Sleep(5000);
                }

            }
            public enum RoomState
            {
                instantiated,
                settup,
                waiting,
                starting,
                playing
            }
            int _maxPlayers;
            SafeList<ConnectedClient> _clients;
            TcpListener _server;
            RoomState _state;
        }
        //Stole from pontus credit to him
        public class SafeList<T>
        {
            private List<T> _internalList;
            private bool isBusy = false;
            public int Count
            {
                get
                {
                    while (isBusy) ;
                    isBusy = true;
                    int value = _internalList.Count;
                    isBusy = false;
                    return value;
                }
            }

            public SafeList()
            {
                _internalList = new List<T>();
            }

            public void Add(T value)
            {
                while (isBusy) ;
                isBusy = true;
                _internalList.Add(value);
                isBusy = false;
            }
            public void Remove(T value)
            {
                while (isBusy) ;
                isBusy = true;
                _internalList.Remove(value);
                isBusy = false;
            }
            public void RemoveAt(int index)
            {
                while (isBusy) ;
                isBusy = true;
                _internalList.RemoveAt(index);
                isBusy = false;
            }

            public List<T> getCopyOfInternalList()
            {
                while (isBusy) ;
                isBusy = true;
                List<T> copyList = new List<T>(_internalList);
                isBusy = false;
                return copyList;
            }

            public T GetAt(int index)
            {
                while (isBusy) ;
                isBusy = true;
                T value = _internalList[index];
                isBusy = false;
                return value;
            }

            public int GetCount()
            {
                while (isBusy) ;
                isBusy = true;
                int value = _internalList.Count;
                isBusy = false;
                return value;
            }
        }
    }
}
