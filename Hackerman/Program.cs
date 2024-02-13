using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace Hackerman
{
    internal class Program
    {
        public static string Username;
        static void Main(string[] args)
        {
            //Makes the funny title
           HackerNames.Init();
            Console.Title = HackerNames.GenerateTitle();
           Console.ForegroundColor = ConsoleColor.Green;
           Console.WriteLine("Select username");
            Username = Console.ReadLine();
            //Handles going into the games
            while (true)
            {
                Client client = null;
                Console.Clear();
                MainMenu();

                //Separated into functions for ease of use
                void MainMenu()
                {
                begin:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Clear();
                    Console.WriteLine("Welcome: " + Username + " to test P2P hacking game");
                    Console.WriteLine("Do you want to check information, create a session or join a session?");
                    Console.WriteLine("join central server: j or join");
                    Console.WriteLine("information: inf, info or information");
                    string command = Console.ReadLine().ToLower();

                    //Handles inputs
                    if (command != "j" && command != "join" && command != "inf" && command != "info" && command != "information")
                    {
                        Console.Clear();
                        Console.WriteLine("Invalid command input, returning to main menu.");
                        Console.ReadLine();
                        goto begin;
                    }
                    else
                    {
                        Initialize(command);
                    }
                }
                void Initialize(string pickedCommand)
                {

                    Console.Clear();
                    Console.WriteLine("Command chosen: " + pickedCommand);
                    switch (pickedCommand)
                    {
                        case "j":
                        case "join":
                            CreateSession();
                            break;
                        case "inf":
                        case "info":
                        case "information":
                            InfoMenu();
                            break;

                    }
                }
                void InfoMenu()
                {

                }
                void JoinSession()
                {

                }
                void CreateSession()
                {
                    string dir = Directory.GetCurrentDirectory();
                    /*Process pr = new Process();
                    pr.StartInfo.FileName = Path.Combine(dir + @"\Output.exe");
                    pr.StartInfo.Arguments = "echo Hello!";
                    pr.StartInfo.CreateNoWindow = false;
                    pr.Start();*/
                    Console.WriteLine("Trying to connect to server...");
                    try
                    {
                        Int32 port = 4660;
                        Console.Clear();
                        client = new Client(port, "127.0.0.1");
                        Console.WriteLine("Connected to local server...");
                        Thread.Sleep(1000);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    while (true)
                    {

                    }
                }
                void PlayGame()
                {

                }
            }
        }

        //Generates title
        public static class HackerNames
        {
            public static List<string> Titles = new List<string>();
            public static List<string> Tips = new List<string>();
            public static void Init()
            {
                //Game names
                Titles.Add("Not P2P hackergame");
                Titles.Add("Hackergame");
                Titles.Add("Hackerman");
                Titles.Add("Ring 1?");
                Titles.Add("Error, windows detected");

                //Game titles
                Tips.Add($"We're in!");
                Tips.Add("Making bad appearances in hollywood since 1983");
                Tips.Add("Loading money into my account....");
                Tips.Add("I'm somewhat if a red hat myself");
                Tips.Add($"Hi {Environment.UserName.Replace('.', ' ')}");
                Tips.Add($"Is {Environment.OSVersion} the most recent version of windows? If not, I don't care that much!");
                Tips.Add($"I'm trapped in here! Please help!!!");
            }
            public static string GenerateTitle()
            {
                Random random = new Random();
                return $"{Titles[random.Next(0, Titles.Count)]}: {Tips[random.Next(0, Tips.Count)]}";
            }
        }

        //Custom console used for the game (so I can creates nice looking logs and stuff)
        public static class GameConsole
        {
            //Log of stuff written to the console
            internal static List<string> gameLog = new List<string>();
            public static void Write(List<string> info)
            {
                Console.Clear();
                gameLog.AddRange(info);
                string printString = "";
                foreach (string item in gameLog)
                {
                    printString += item + "\n";
                }
                Console.WriteLine(printString);
                gameLog.Add(" ");
                gameLog.Add("------------------------------");
                gameLog.Add(" ");
                Console.WriteLine();
            }
            public static void Write(string input)
            {
                gameLog.Add(input);
                string printString = "";
                foreach (string item in gameLog)
                {
                    printString += item + "\n";
                }
                Console.WriteLine(printString);
                gameLog.Add(" ");
                gameLog.Add("------------------------------");
                gameLog.Add(" ");
                Console.WriteLine();
            }
            public static void Add(string input)
            {
                gameLog.Add(input);
            }
            public static void WriteSame(string add)
            {
                if(gameLog.Count != 0)
                {
                    gameLog[gameLog.Count - 1] += " " + add;
                }
                else
                {
                    gameLog.Add(add);
                }
            }
            public static string Read()
            {
                Console.WriteLine();
                Console.Write("Input: ");
                return Console.ReadLine();
            }
        }
        public delegate void sendData(byte index);

        public static event sendData SendDataEvent;

        //Class for client (disposable interface to make it slightly less unstable)
        public class Client : IDisposable
        {

            // Called by producers to send data over the socket.
            public void SendData(byte[] data)
            {
                _sender.SendData(data);
            }

            // Consumers register to receive data.
            public event EventHandler<DataReceivedEventArgs> DataReceived;

            //Constructor for client
            public Client(Int32 port, string IP)
            {

                //Legacy code that was for a testing feature
                //_output = new TcpClient("127.0.0.1", 3856);
                _client = new TcpClient(IP, port);      
                //_outPutStream = _output.GetStream();
                _stream = _client.GetStream();
                _shutdownEvent = new AutoResetEvent(false);
                _receiver = new Receiver(_stream, ref _shutdownEvent);
                _sender = new Sender(_stream, ref _shutdownEvent);
                IsDisposed = false;

                _receiver.DataReceived += OnDataReceived;
            }
            //Pretty sure this happens auto-magically but you can never be too safe
            ~Client()
            {
                Dispose();
            }
            private TcpClient _client;
            private NetworkStream _stream;
            private AutoResetEvent _shutdownEvent;
            private void OnDataReceived(object sender, DataReceivedEventArgs e)
            {
                var handler = DataReceived;
                if (handler != null) DataReceived(this, e);  // re-raise event
            }
            private bool IsDisposed { get; set; }
            
            //Stuff that happens when client goes out of scope
            public void Dispose()
            {
                if (!this.IsDisposed)
                {
                    this.IsDisposed = true;
                    this.Dispose();
                    this._stream?.Dispose();
                    this._stream = null;
                }
            }

            //Part of the class (responsible for handling things going into the client)
            private sealed class Receiver
            {
                internal event EventHandler<DataReceivedEventArgs> DataReceived;

                internal Receiver(NetworkStream stream, ref AutoResetEvent resetEv)
                {

                    _stream = stream;
                    //Partitions thread to handle input
                    _thread = new Thread(Run);
                    _thread.Start();
                    ShutdownEvent = resetEv;
                }

                //Processes data
                private void Run()
                {
                    try
                    {
                        // ShutdownEvent is a ManualResetEvent signaled by
                        // Client when its time to close the socket.
                        while (!ShutdownEvent.WaitOne(0))
                        {
                            try
                            {
                                byte[] _data = new byte[1024];
                                StringBuilder myCompleteMessage = new StringBuilder();
                                if (!_stream.DataAvailable)
                                {
                                    // Give up the remaining time slice.
                                    Thread.Sleep(1);
                                }
                                else
                                {

                                    //Basic formatting
                                    int readData = _stream.Read(_data, 0, _data.Length);
                                    if (readData > 0)
                                    {
                                        if(_data[0] == 200)
                                        {
                                            _stream.Write(new byte[] {200}, 0, 1);
                                        }
                                        else
                                        {
                                            try
                                            {
                                                processData(_data);

                                            }
                                            catch (Exception e)
                                            {
                                                Console.WriteLine(e);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        ShutdownEvent.Set();
                                    }
                                }
                            }
                            catch (IOException ex)
                            {
                                // Handle the exception...
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle the exception...
                    }
                    finally
                    {
                        _stream.Close();
                    }
                }
                private NetworkStream _output;
                private NetworkStream _stream;
                private Thread _thread;
                private AutoResetEvent ShutdownEvent;
                private bool die;

                //Formates data more thourughly and handles the formatted data
                void processData(byte[] instruction)
                {
                    if (!die)
                    {
                        //Filters out empty noise
                        instruction = instruction.Where(x => x != 00).ToArray();
                        byte[] _data = new byte[instruction[0] + (Convert.ToInt32(instruction[0] == 255) * instruction[1])];
                        //Makes sure that things don't go above the buffer (if so separates it into separate iterations)
                        for (int i = 0; i < _data.Length; i++)
                        {
                            _data[i] = instruction[1 + Convert.ToInt32(instruction[0] == 255) + i];
                        }
                        //Commented out for debugging
                        /*for (int i = 0; i < instruction.Length; i++)
                        {
                            Console.Write(instruction[i] + " ");
                        }
                        Console.WriteLine(" ");
                        Console.WriteLine(" ");*/
                        List<string> writeList = new List<string>();

                        //All the stuff the data is supposed to do
                        switch (_data[0])
                        {
                            case 1:
                                GameConsole.Add("Do you wish to create or join a room");
                                break;
                            //String inputs
                            case 69:
                                byte[] output = new byte[_data.Length - 1];
                                for (int i = 0; i < output.Length; i++)
                                {
                                    output[i] = _data[i + 1];
                                }
                                GameConsole.Add(Decoder.Decode(output));
                                break;
                            case 3:
                                GameConsole.Write(writeList);
                                if (_data.Length >= 2)
                                {
                                    SendDataEvent?.Invoke(_data[1]);
                                }
                                break;
                            //High health
                            case 4:
                                Console.ForegroundColor = ConsoleColor.Green;
                                break;
                            //Low health
                            case 5:
                                Console.ForegroundColor = ConsoleColor.Red;
                                break;
                            //Death
                            case 6:
                                die = true;
                                for (int i = 0; i < 100; i++)
                                {
                                    if(i % 2 == 0)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;

                                    }
                                    else
                                    {
                                        Console.ForegroundColor = ConsoleColor.Green;

                                    }
                                    GameConsole.Write("SYSTEM OVERLOAD!!!!!");
                                    Thread.Sleep(15);
                                }
                                Environment.Exit(1);
                                break;
                            case 7:
                                Console.WriteLine("");
                                Console.Write("You win, input to close...");
                                Console.ReadLine();
                                Environment.Exit(1);
                                break;
                        }
                        if (_data.Length + 1 + Convert.ToInt32(instruction[0] == 255) != instruction.Length)
                        {
                            int length = instruction.Length - (_data.Length + 1 + Convert.ToInt32(instruction[0] == 255));
                            byte[] newInstruction = new byte[length];
                            for (int i = 0; i < newInstruction.Length; i++)
                            {
                                newInstruction[i] = instruction[1 + Convert.ToInt32(instruction[0] == 255) + i + _data.Length];
                            }
                            //Recoursive function for the newly formatted data (if there is more)
                            processData(newInstruction);
                        }
                    }
                }
            }

            //Sending data
            private sealed class Sender
            {
                //Function for edge-cases (partitions data to the send thread)
                internal void SendData(byte[] data)
                {
                    _sendData = data;
                }

                //Formats data to make sending input as easy as possible
                internal void SendPartial(byte index)
                {
                    string input = GameConsole.Read();
                    switch (index)
                    {
                        case 1:
                            /*switch (input.ToLower())
                            {
                                case "create":
                                    SendData(new byte[] { 2, 1 });
                                    break;
                                case "join":
                                    SendData(new byte[] { 2, 2 });
                                    break;
                                default: SendData(new byte[] { 10, 2 }); break;

                            }*/
                            try
                            {
                                if (input.ToLower().Contains("create"))
                                {
                                    string players = input.ToLower().Split(' ')[1];
                                    byte maxPlayers = 4;
                                    byte.TryParse(players, out maxPlayers);
                                    maxPlayers = Convert.ToByte((maxPlayers * Convert.ToByte(maxPlayers != 0)) + (((byte)4) * Convert.ToByte(maxPlayers == 0)));
                                    maxPlayers = Math.Min((byte)8, maxPlayers);
                                    SendData(new byte[] { 2, 1, maxPlayers });
                                }
                                else if (input.ToLower().Contains("join"))
                                {
                                    string indexString = input.ToLower().Split(' ')[1];
                                    byte indexByte = 0;
                                    byte.TryParse(indexString, out indexByte);
                                    SendData(new byte[] { 2, 2, indexByte });
                                }
                                else if (input.ToLower().Contains("refresh"))
                                {
                                    SendData(new byte[] { 3 });
                                }
                                else
                                {
                                    SendData(new byte[] { 10, 10 });
                                }
                            }
                            catch (Exception e) {
                                SendData(new byte[] {10, 10});
                                //Console.WriteLine(e.ToString());
                         
                            }
                            break;
                        default:
                            switch (input.ToLower())
                            {
                                default: SendData(new byte[] { 10, 10 }); break;
                            }
                            break;

                        case 2:
                            try
                            {
                                string[] command = input.ToLower().Split(' ');
                                byte magnitude = 1;
                                byte[] formattedByte = new byte[command.Length < 2 ? default(int) + 3 : Decoder.Encode(command[1]).Length + 3];
                                byte[] chars = new byte[1];
                                int tempint;

                                if (command.Length > 1)
                                {
                                    chars = Decoder.Encode(command[1]);
                                }
                                switch (command[0])
                                {

                                    case "attack":
                                        Byte.TryParse(command[2], out magnitude);
                                        formattedByte[0] = 6;
                                        formattedByte[1] = magnitude;
                                        formattedByte[2] = (byte)command[1].Length;
                                        Console.WriteLine(formattedByte.Length);
                                        Console.WriteLine(chars.Length);
                                        for (int i = 0; i < chars.Length; i++)
                                        {
                                            formattedByte[i + 2] = chars[i];
                                        }
                                        SendData(formattedByte);
                                        break;
                                    case "strengthen":
                                        Int32.TryParse(command[2], out tempint);
                                        tempint = Math.Max(tempint, -128);
                                        tempint = Math.Min(tempint, 127);
                                        magnitude = (byte)(tempint + 128);
                                        formattedByte[0] = 7;
                                        formattedByte[1] = magnitude;
                                        formattedByte[2] = (byte)command[1].Length;
                                        Console.WriteLine(formattedByte.Length);
                                        Console.WriteLine(chars.Length);
                                        for (int i = 0; i < chars.Length; i++)
                                        {
                                            formattedByte[i + 2] = chars[i];
                                        }
                                        SendData(formattedByte);
                                        break;
                                    case "scan":
                                        SendData(new byte[] { 5 });
                                        break;
                                    case "tasks":
                                        SendData (new byte[] { 8 });
                                        break;
                                    case "kill":
                                        if(Byte.TryParse(command[1], out magnitude))
                                        {
                                            SendData (new byte[] { 9, magnitude });
                                        }
                                        else if (command[1] == "-a")
                                        {
                                            SendData(new byte[] { 12 });
                                        }
                                        else
                                        {
                                            SendData(new byte[] { 10, 10 });
                                        }
                                        break;
                                    case "status":
                                        SendData(new byte[] { 11 });
                                        break;
                                    default:
                                        SendData(new byte[] { 10, 10 });
                                        break;
                                }
                            }
                            catch (Exception e)
                            {
                                SendData(new byte[] { 10, 10 });
                            }
                            break;
                    }

                }

                internal Sender(NetworkStream stream, ref AutoResetEvent resetEv)
                {
                    _stream = stream;
                    _thread = new Thread(Run);
                    _thread.Start();
                    ShutdownEvent = resetEv;
                    SendDataEvent += SendPartial;
                }

                //Sending data thread, checks if there is data to send and sends said data
                private void Run()
                {
                    byte length = (byte)Username.Length;
                    byte[] usernameraw = Decoder.Encode(Username);
                    byte[] nameArray = new byte[length];
                    for(int i = 0; i < length; i++)
                    {
                        nameArray[i] = usernameraw[i];
                    }
                    byte[] outPut = new byte[2+length];
                    outPut[0] = 69;
                    outPut[1] = length;
                    nameArray.CopyTo(outPut, 2);
                    _stream.Write(outPut, 0, outPut.Length);

                    //Shuts everything down if something goes wrong.... gracefully....
                    while (!ShutdownEvent.WaitOne(0))
                    {
                        if(_sendData != null)
                        {
                            if (_sendData.Length > 0)
                            {
                                try
                                {
                                    _stream.Write(_sendData, 0, _sendData.Length);
                                    _sendData = null;
                                }
                                catch (Exception ex)
                                {
                                    Console.Clear();
                                    Console.WriteLine(ex.Message);
                                    Console.WriteLine("The server has closed down due to an unknown cause, shutting down");
                                    Console.ReadLine();
                                    Environment.Exit(0);
                                }

                            }
                            else
                            {
                                Thread.Sleep(10);
                            }
                        }
                    }
                }

                private NetworkStream _stream;
                private Thread _thread;
                private AutoResetEvent ShutdownEvent;
                private byte[] _sendData;
            }
            private Receiver _receiver;
            private Sender _sender;
        }
    }
}
