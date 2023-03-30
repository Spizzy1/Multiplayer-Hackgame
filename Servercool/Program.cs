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
                                    return;
                                }
                                else if(_data[1] == 2)
                                {
                                    try
                                    {
                                        if (client.SaveList != null && client.SaveList[_data[2]] != null && client.SaveList[_data[2]].addPlayer(client) && rooms.Count > _data[2])
                                        {
                                            client.writeMsg("You have joined " + client.SaveList[_data[2]].Host.Name + "'s room!");
                                            client.Write(new byte[] { 3 });
                                            return;
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
                            client.Name = "";
                            foreach (char character in client.DecodeRAW(_data))
                            {
                                if (character != ' ')
                                {
                                    client.Name += character;
                                }
                            }
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
    public string DecodeRAW(byte[] input)
    {
        byte[] processData = new byte[input[1]];
        for (int i = 0; i < input[1]; i++)
        {
            processData[i] = input[i + 2];
        }
        return Server.Decoder.Decode(processData);
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
        totalComputers = new SafeList<Computer>();
        _server = server;
        _shuttingDown = false;
        tasks = new SafeList<Process>();
        _handler = new ConnectionHandler();
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
        foreach(ConnectedClient client in _clients.getCopyOfInternalList())
        {
            if(client != null)
            {
                Task.Run(client.readRoom);
                Computer clientOwn = new Computer(client.Name);
                client.Computers.Add(clientOwn);
                totalComputers.Add(clientOwn);
                client.ConnectedComputer = clientOwn;
            }
        }
        foreach(Computer computer in totalComputers.getCopyOfInternalList())
        {
            foreach(Computer computer2 in totalComputers.getCopyOfInternalList())
            {
                _handler.Connect(computer, computer2);
                Console.WriteLine("Test: " + _handler.ConnectedComputers.Count);

            }
        }
        foreach(ConnectedClient client in _clients.getCopyOfInternalList())
        {
            client.Write(new byte[] { 3, 2 });
        }

    }
    void ManageGame()
    {
        while (!_shuttingDown)
        {
            Thread.Sleep(2000);
            foreach(Strengthen strength in tasks.getCopyOfInternalList())
            {
                strength.effect();
            }
            foreach(Attack attack in tasks.getCopyOfInternalList())
            {
                attack.effect();
            }
        }
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
                Thread.Sleep(1000);
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
            ConnectedClient addClient = new ConnectedClient(client.connection, _handler, ref tasks);
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
    internal class Computer
    {
        public Computer(string name = null)
        {
            _health = 100;
            _resources = 100;
            if(name != null)
            {
                _name = name;
            }
            else
            {
                Random random = new Random();
                _name = random.Next().ToString();
            }
        }
        public void ChangeHP(int amount)
        {
            _health = Math.Clamp(_health - amount, 0, 100);
            if(_health == 0)
            {
                //Add later
            }
        }
        private int _health;
        public int Health { get { return _health; } }
        private int _resources;
        public int Resources { get { return _resources; } set { Math.Clamp(value, 0, 100); } }

        string _name;
        public string Name { get { return _name; } }
    }
    internal class ConnectionHandler
    {
        public ConnectionHandler()
        {
            ConnectedComputers = new SafeList<Connection>();
        }
    public void Connect(Computer computer1, Computer computer2)
        {
            if(ConnectedComputers.getCopyOfInternalList().Where(x => x.Computers.Contains(computer1) && x.Computers.Contains(computer2)).ToList().Count == 0 && computer1 != computer2)
            {
                Console.WriteLine("Creating connection between: " + computer1.Name + " and " + computer2.Name);
                ConnectedComputers.Add(new Connection(computer1, computer2));
            }
        }
        public void Disconnect(Computer computer1, Computer computer2)
        {
            List<Connection>? computers = ConnectedComputers.getCopyOfInternalList().Where(x => x.Computers.Contains(computer1) && x.Computers.Contains(computer2)).ToList();
            if (computers != null && computer1 == computer2)
            {
                foreach(Connection computer in computers)
                {
                    ConnectedComputers.Remove(computer);
                }
            }
        }
        public List<Computer> GetComputers(Computer computer)
        {
            List<Computer> computers = new List<Computer>();
            foreach(Connection connection in ConnectedComputers.getCopyOfInternalList().Where(x => x.Computers.Contains(computer)).ToList())
            {
                Computer? addComputer = connection.Computers.Where(x => x != computer).Take(1).FirstOrDefault();
                if(addComputer != null)
                {
                    computers.Add(addComputer);
                }
            }
            return computers;
        }
        public List<Connection> GetConnections(Computer computer) {
            return ConnectedComputers.getCopyOfInternalList().Where(x => x.Computers.Contains(computer)).ToList();
        }
        public SafeList<Connection> ConnectedComputers;

    }
    internal class Connection
    {
        public Connection(Computer computer1, Computer computer2)
        {
            computers = new Computer[2];
            computers[0] = computer1;
            computers[1] = computer2;
            _strength = 1;
        }
        private Computer[] computers;
        public Computer[] Computers { get { return computers; } }

        private int _strength;
        public int strength { get { return _strength; } set { _strength = Math.Clamp(value, 0, 100); } }
    }
    internal class ConnectedClient : Client
    {
        public ConnectedClient(TcpClient client, ConnectionHandler handler, ref SafeList<Process> actions) : base(client)
        {
            _client = client;
            _ownedComputers = new List<Computer>();
            _handler = handler;
            _actions = actions;
        }
        public void writeClient(byte instruction)
        {
            Console.WriteLine("Input recieved: " + instruction);
            switch (instruction)
            {
                case 1:
                    break;
            }
        }
        public void readRoom()
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
                                case 5:
                                    foreach (Computer computer in _handler.GetComputers(ConnectedComputer)) {
                                        writeMsg(computer.Name);
                                    }
                                    Write(new byte[] { 3, 2 });
                                    break;
                                case 6:
                                    try
                                    {
                                        byte magnitude = _data[1];
                                        byte[] decode = new byte[_data.Length - 2];
                                        for(int i = 0; i < decode.Length; i++)
                                        {
                                            decode[i] = _data[i+2];
                                        }
                 
                                        Computer? target = _handler.GetComputers(ConnectedComputer).Where(x => x.Name.ToLower() == Server.Decoder.Decode(decode)).ToList().FirstOrDefault();
                                        Connection? targetConnection = _handler.GetConnections(ConnectedComputer).Where(x => x.Computers.Contains(target)).FirstOrDefault();
                                        Console.WriteLine(target != null);
                                        Console.WriteLine(targetConnection != null);
                                        if (target != null && targetConnection != null)
                                        {
                                            Attack attack = new Attack(targetConnection, this._ownedComputers[0], target, _data[1]);
                                            _actions.Add(attack);
                                            this.writeMsg(attack.Subject.Name);
                                            this.writeMsg(attack.Target.Name);
                                            Write(new byte[] { 3, 2 });
                                        }
                                        else
                                        {
                                            writeMsg("Target computer is unavailable");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.ToString());
                                    }
                                    break;
                                case 7:
                                    break;
                                case 8:
                                    break;
                                case 200:
                                    Console.WriteLine("Client alive");
                                    break;
                                default:
                                    writeMsg("Invalid input");
                                    Write(new byte[] { 3, 2 });
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
        List<Computer> _ownedComputers;
        private Computer _connectedComputer;
        SafeList<Process> _actions;
        public Computer ConnectedComputer { get { return _connectedComputer; } set { _connectedComputer = value; } }
        ConnectionHandler _handler;
        public List<Computer> Computers { get { return _ownedComputers; } }

    }
    internal class Process
    {
        public Process(Connection connection, Computer subject, Computer target, int cost)
        {
            _connection = connection;
            _subject = subject;
            _target = target;
            _cost = cost;
        }
        private Connection _connection;
        public Connection Connection { get { return _connection; } }
        private Computer _subject;
        public Computer Subject { get { return _subject; } }
        private Computer _target;
        public Computer Target { get { return _target; } }
        private int _cost;
        public int Cost { get { return _cost; } }
        public virtual void kill()
        {
            _subject.Resources -= Cost;
        }
        public virtual void effect()
        {

        }
    }
    internal class Attack : Process
    {
        public Attack(Connection connection, Computer subject, Computer target, int cost) : base(connection, subject, target, cost)
        { 

        }
        public override void effect()
        {
            base.effect();
            Target.ChangeHP(Cost * Connection.strength);
        }
    }
    internal class Strengthen : Process
    {
        public Strengthen (Connection connection, Computer subject, Computer target, int cost, bool positive) : base(connection, subject, target, cost)
        {
            _positive = positive;
        }

        public override void effect()
        {
            base.effect();
            Connection.strength += Cost * (Convert.ToByte(!_positive) + ((-1) * Convert.ToByte(_positive)));
        }
        bool _positive;
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
    SafeList<Computer> totalComputers;
    ConnectionHandler _handler;
    public int currentPlayers { get { return _clients.Count; } }
    Client _host;
    public Client Host { get { return _host; } }
    TcpListener _server;
    RoomState _state;
    SafeList<Process> tasks;

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