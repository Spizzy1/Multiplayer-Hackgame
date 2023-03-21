using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Server;

Console.WriteLine("Program started");
Int32 port = 1300;
IPAddress localAddr = IPAddress.Any;

TcpListener server = new TcpListener(localAddr, port);
Thread waitConnection = new Thread(() => awaitConnection());

waitConnection.Start();

void awaitConnection()
{
    server.Start();
    Console.WriteLine("waiting for connection...");
    while (true)
    {
        try
        {
            TcpClient client = server.AcceptTcpClient();
            Thread readClient = new Thread(() => readServer(client));
            readClient.Start();
            writeServer(client, (byte)1);
            writeServer(client, (byte)69);
            writeServer(client, new byte[] { 3, 1 });
            Console.WriteLine("Client connected!");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
    server.Stop();
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
                        case 2:
                            if (_data.Length >= 2)
                            {
                                if (_data[1] == 1)
                                {
                                    writeServer(client, 68);
                                }
                                else
                                {

                                }
                            }
                            break;
                        case 69:

                            break;
                        case 200:
                            Console.WriteLine("Client alive");
                            break;
                        default:
                            writeMsg(client, "Invalid input");
                            writeServer(client, new byte[] { 3, 2 });
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
void writeServer(TcpClient client, object instruction)
{
    if (instruction.GetType() == typeof(byte))
    {
        switch ((byte)instruction)
        {
            case 1:
                byte[] data = formatMsg(new byte[] {1});
                client.GetStream().Write(data, 0, data.Length);
                break;
            case 69:
                writeMsg(client, "amongus \n so\n sus");
                break;
            case 68:
                
                break;
        }
    }
    else if (instruction.GetType() == typeof(byte[]))
    {
        byte[] input = formatMsg(((byte[])instruction).Where(x => x != 00).ToArray());
        Console.WriteLine(input[0]);
        client.GetStream().Write(input, 0, input.Length);
    }

}
void writeMsg(TcpClient client, string message)
{
    byte[] msg = Server.Decoder.Encode(message);
    byte[] msgindex = new byte[1 + msg.Length];
    msgindex[0] = 69;
    msg.CopyTo(msgindex, 1);
    msgindex = formatMsg(msgindex);
    client.GetStream().Write(msgindex, 0, msgindex.Length);
}
byte[] formatMsg(byte[] instruction)
{
    byte[] msg = new byte[instruction.Length+1 + Convert.ToInt16(instruction.Length >= 255)];
    msg[0] = (byte)instruction.Length;
    if(instruction.Length >= 255)
    {
        msg[1] = (byte)(instruction.Length - 255);
        instruction.CopyTo(msg, 2);
    }
    else
    {
        instruction.CopyTo(msg, 1);
    }
    return msg;
}

async void test()
{

}

class Client
{
    Client(TcpClient client)
    {
        _client = client;
    }
    private TcpClient _client;
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
        _host = host;
        waitForPlayers();

    }
    void waitForPlayers()
    {
        _state = RoomState.waiting;
        int time = DateTime.Now.Millisecond;
        int saveCount = _clients.Count;

        Console.WriteLine("Waiting for player " + _clients.Count + 1);
        while (DateTime.Now.Millisecond - time < 20000 && _clients.Count != _maxPlayers)
        {
            if (_clients.Count > saveCount)
            {
                time = DateTime.Now.Millisecond;
            }
        }
        return;
    }
    void addPlayer(TcpClient client)
    {
        if (_state != RoomState.playing && _clients.Count > _maxPlayers)
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
    TcpClient _host;
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