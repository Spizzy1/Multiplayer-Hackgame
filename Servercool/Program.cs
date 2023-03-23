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
SafeList<Room> rooms = new SafeList<Room>();
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
            Client player = new Client(client);
            Thread readClient = new Thread(() => readServer(player));
            readClient.Start();
            Console.WriteLine("Client connected!");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
    server.Stop();
}


void readServer(Client client)
{
    NetworkStream _stream = client.connection.GetStream();
    while (true)
    {
        try
        {
            byte[] _data = new byte[255];
            if (!client.connection.GetStream().DataAvailable)
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
                                    Room newRoom = new Room(_data[2], server);
                                    rooms.Add(newRoom);
                                    newRoom.initiate(client);
                                    break;
                                }
                                else
                                {
                                    List<Room> saveList = rooms.getCopyOfInternalList();
                                    if(rooms.Count == 0)
                                    {
                                        client.writeMsg("No rooms available, currently... you can either restart and create a room or wait until a room shows up");
                                        while (client.connection.Connected && rooms.Count == 0)
                                        {
                                            Thread.Sleep(100);
                                        }
                                    }
                                }
                            }
                            break;
                        case 3: 
                            for(int i = 0; i < rooms.getCopyOfInternalList().Count)
                            {
                                client.writeMsg(i + ": " + rooms.GetAt(i).)
                            }
                            break;
                        case 69:
                            client.Name = DecodeRAW(_data);
                            client.writeMsg("Welcome! " + client.Name);
                            writeServer(client, (byte)1);
                            writeServer(client, (byte)69);
                            writeServer(client, new byte[] { 3, 1 });
                            break;
                        case 200:
                            Console.WriteLine("Client alive");
                            break;
                        default:
                            client.writeMsg("Invalid input");
                            writeServer(client, new byte[] { 3, 1 });
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
string DecodeRAW(byte[] input)
{
    byte[] processData = new byte[input[1]];
    for (int i = 0; i < input[1]; i++)
    {
        processData[i] = input[i + 2];
    }
    return Server.Decoder.Decode(processData);
}
void writeServer(Client client, object instruction)
{
    if (instruction.GetType() == typeof(byte))
    {
        switch ((byte)instruction)
        {
            case 1:
                byte[] data = client.formatMsg(new byte[] {1});
                client.connection.GetStream().Write(data, 0, data.Length);
                break;
            case 69:
                client.writeMsg("Type 'create' followed by the amount of max players to create a room and 'join' to join a room");
                break;
            case 68:
                
                break;
        }
    }
    else if (instruction.GetType() == typeof(byte[]))
    {
        byte[] input = client.formatMsg(((byte[])instruction).Where(x => x != 00).ToArray());
        Console.WriteLine(input[0]);
        client.connection.GetStream().Write(input, 0, input.Length);
    }

}

async void test()
{

}

class Client
{
    public Client(TcpClient client)
    {
        _client = client;
    }
    /// <summary>
    /// Formats the string to be sent and decoded by the client
    /// </summary>
    /// <param name="message"></param>
    public void writeMsg(string message)
    {
        byte[] msg = Server.Decoder.Encode(message);
        byte[] msgindex = new byte[1 + msg.Length];
        msgindex[0] = 69;
        msg.CopyTo(msgindex, 1);
        msgindex = formatMsg(msgindex);
        _client.GetStream().Write(msgindex, 0, msgindex.Length);
    }
    /// <summary>
    /// Formats your data to be read by the client correctly, data without correct formatting will return an error on the client
    /// </summary>
    /// <param name="instruction"></param>
    /// <returns></returns>
    public byte[] formatMsg(byte[] instruction)
    {
        byte[] msg = new byte[instruction.Length + 1 + Convert.ToInt16(instruction.Length >= 255)];
        msg[0] = (byte)instruction.Length;
        if (instruction.Length >= 255)
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
    /// <summary>
    /// Use this instead of the normal TcpClient.Write function as this formats your message for you
    /// </summary>
    /// <param name="msg"></param>
    public void Write(byte[] msg)
    {
        msg = formatMsg(msg);
        connection.GetStream().Write(msg, 0, msg.Length);
    }

    private TcpClient _client;
    public TcpClient connection { get { return _client; } }
    private string _name;
    public string Name { get { return _name; } set { _name = value; } }
}
class Room
{
    public Room(int maxPlayers, TcpListener server)
    {
        _maxPlayers = maxPlayers;
        _state = RoomState.instantiated;
        _clients = new SafeList<ConnectedClient>();
        _server = server;
        _shuttingDown = false;
    }
    public void initiate(Client host)
    {
        _state = RoomState.settup;
        addPlayer(host);
        _host = host;
        host.writeMsg("Room created with " + _maxPlayers + " maximum players...");
        host.Write(new byte[] { 3 });
        Task.Run(checkConnection);
        waitForPlayers();

    }
    void waitForPlayers()
    {
        _state = RoomState.waiting;
        int time = DateTime.Now.Millisecond;
        int saveCount = _clients.Count;

        Console.WriteLine("Waiting for player " + _clients.Count + 1);
        while (DateTime.Now.Millisecond - time < 20000 && _clients.Count != _maxPlayers && !_shuttingDown)
        {
            if (_clients.Count > saveCount)
            {
                time = DateTime.Now.Millisecond;
            }
        }
        return;
    }
    void addPlayer(Client client)
    {
        if (_state != RoomState.playing && _clients.Count > _maxPlayers)
        {
            ConnectedClient addClient = new ConnectedClient(client.connection);
            addClient.Name = client.Name;
            _clients.Add(addClient);
        }
    }
    void playerDisconnect()
    {

    }
    internal class ConnectedClient : Client
    {
        public ConnectedClient(TcpClient client) : base(client)
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
        List<ConnectedClient> saveList = _clients.getCopyOfInternalList();
        while (!_shuttingDown)
        {
            if(saveList.Count > _clients.getCopyOfInternalList().Count)
            {
                Console.WriteLine("client disconnected");
            }
            saveList = _clients.getCopyOfInternalList();
            Console.WriteLine("Checking connections...");
            if (_clients.Count == 0)
            {
                Console.WriteLine("Room shutting down");
                _shuttingDown = true;
            }
            foreach (var client in _clients.getCopyOfInternalList())
            {
                try
                {
                    client.Write(new byte[] { 200 });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("client disconnected");
                    if(_clients.Count == 0)
                    {
                        Console.WriteLine("Room shutting down");
                        _shuttingDown = true;
                    }
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
    bool _shuttingDown;
    int _maxPlayers;
    SafeList<ConnectedClient> _clients;
    Client _host;
    Client Host { get { return _host; } }
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