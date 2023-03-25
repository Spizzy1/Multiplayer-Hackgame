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
    RoomHandler.OnRoom += RemoveRoom;
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
void RemoveRoom(object sender, EventArgs e)
{
    if(sender.GetType() == typeof(Room))
    {
        Room room = (Room)sender;
        try
        {
            Console.WriteLine("removing a room...");
            rooms.Remove(room);
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
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
                                else if(_data[1] == 2)
                                {
                                    try
                                    {
                                        if (client.SaveList != null && client.SaveList[_data[2]] != null && client.SaveList[_data[2]].addPlayer(client) && rooms.Count > _data[2])
                                        {
                                            client.writeMsg("You have joined " + client.SaveList[_data[2]].Host.Name + "'s room!");
                                            client.Write(new byte[] { 3 });
                                        }
                                        else
                                        {
                                            client.writeMsg("There was an issue trying to join the room, refreshing \n");
                                            writeServer(client, (byte)69);
                                            client.Write(new byte[] { 3, 1 });
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e.ToString());
                                    }
                                }
                            }
                            break;
                        case 69:
                            client.Name = DecodeRAW(_data);
                            client.writeMsg("Welcome! " + client.Name);
                            writeServer(client, (byte)1);
                            writeServer(client, (byte)69);
                            client.Write(new byte[] { 3, 1 });
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
                client.writeMsg("Type 'create' followed by the amount of max players to create a room and 'join' followed by the rook index to join a room");
                if(rooms.Count == 0)
                {
                    client.writeMsg("No rooms available, currently... you can either create a room or wait until a room shows up");
                }
                else
                {
                    client.writeMsg("Rooms:\n");
                    client.SaveList = rooms.getCopyOfInternalList();
                    int i = 0;
                    foreach(Room room in client.SaveList)
                    {
                        try
                        {
                            Console.WriteLine(room.Host.Name);
                            client.writeMsg(i + ": " + room.Host.Name + $"'s room \nJoinable: {room.Joinable} \nPlayers: {room.currentPlayers}/{room.MaxPlayers}\n");
                            i++;

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                    }
                }

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
public static class RoomHandler
{
    public static EventHandler OnRoom;
    public class RoomDestroyArgs : EventArgs
    {
        public RoomDestroyArgs(Room room)
        {
            this.room = room;
        }


        Room room { get; set; }
    }
}

public class Client
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
    public List<Room> SaveList { get; set; }
    public string Name { get { return _name; } set { _name = value; } }
}
public class Room
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
        host.writeMsg("Room created with " + _maxPlayers + " maximum players...");
        _host = _clients.GetAt(0);
        host.Write(new byte[] { 3 });
        Task.Run(checkConnection);
        waitForPlayers();
        foreach (ConnectedClient client in _clients.getCopyOfInternalList())
        {
            client?.writeMsg("starting game...");
            client?.Write(new byte[] { 3 });
        }
        _state = RoomState.starting;

    }
    void waitForPlayers()
    {
        _state = RoomState.waiting;
        int time = DateTime.Now.Millisecond;
        int saveCount = _clients.Count;

        Console.WriteLine("Waiting for player " + (_clients.Count + 1));
        while (DateTime.Now.Millisecond - time < 20000 && _clients.Count != _maxPlayers && !_shuttingDown)
        {
            if (_clients.Count > saveCount || _clients.Count == 1)
            {
                time = DateTime.Now.Millisecond;
            }
        }
        int Currentplayers = _clients.Count;
        if (!ShuttingDown)
        {
            for (int i = 0; i < 5; i++)
            {

                foreach (ConnectedClient client in _clients.getCopyOfInternalList())
                {
                    try
                    {
                        client?.writeMsg($"Starting in... {5 - i}");
                        client?.Write(new byte[] { 3 });
                        Thread.Sleep(1000);
                    }
                    catch (Exception ex)
                    {
                        playerDisconnect(client);
                    }

                }
                if (Currentplayers != _clients.Count)
                {
                    foreach (ConnectedClient client in _clients.getCopyOfInternalList())
                    {
                        client?.writeMsg("Start abrupted, redoing");
                        client?.Write(new byte[] { 3 });
                        waitForPlayers();
                        return;
                    }
                }
            }
        }
        return;
    }
    void removeRoom()
    {
        Console.WriteLine("Room shutting down");
        _shuttingDown = true;
        RoomHandler.OnRoom?.Invoke(this, new RoomHandler.RoomDestroyArgs(this));

    }
    public bool addPlayer(Client client)
    {
        if ((_state == RoomState.waiting || _state == RoomState.settup) && _clients.Count < _maxPlayers)
        {
            foreach (ConnectedClient c in _clients.getCopyOfInternalList())
            {
                c?.writeMsg($"{client.Name} has joined the room!");
            }
            ConnectedClient addClient = new ConnectedClient(client.connection);
            addClient.Name = client.Name;
            _clients.Add(addClient);
            return true;
        }
        else
        {
            client.writeMsg("The room is currently unavailable.");
            return false;
        }
    }
    void playerDisconnect(ConnectedClient client)
    {
        _clients.Remove(client);
        foreach (ConnectedClient c in _clients.getCopyOfInternalList())
        {
            try
            {
                c.writeMsg(client.Name + " has disconnected.");
                c.Write(new byte[] { 3 });
            }
            catch (Exception ex)
            {
                playerDisconnect(c);
            }
        }
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
            Console.WriteLine(_clients.Count);
            if (saveList.Count > _clients.getCopyOfInternalList().Count)
            {
                Console.WriteLine("client disconnected");
            }
            saveList = _clients.getCopyOfInternalList();
            Console.WriteLine("Checking connections...");
            if (_clients.Count == 0)
            {
                Console.WriteLine("Room shutting down");
                _shuttingDown = true;
                RoomHandler.OnRoom?.Invoke(this, new RoomHandler.RoomDestroyArgs(this));

            }
            foreach (var client in _clients.getCopyOfInternalList())
            {
                try
                {
                    client.Write(new byte[] { 200 });
                }
                catch (Exception ex)
                {
                    playerDisconnect(client);
                    Console.WriteLine("client disconnected");
                    if (_clients.Count == 0)
                    {
                        Console.WriteLine("Room shutting down");
                        _shuttingDown = true;
                        RoomHandler.OnRoom?.Invoke(this, new RoomHandler.RoomDestroyArgs(this));
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
    public bool ShuttingDown { get { return _shuttingDown; } }
    int _maxPlayers;
    public int MaxPlayers { get { return _maxPlayers; } }
    SafeList<ConnectedClient> _clients;
    public int currentPlayers { get { return _clients.Count; } }
    Client _host;
    public Client Host { get { return _host; } }
    TcpListener _server;
    RoomState _state;

    public bool Joinable
    {
        get { return (_state == RoomState.waiting || _state == RoomState.settup) && _clients.Count < _maxPlayers; }

    }
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