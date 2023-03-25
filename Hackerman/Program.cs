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
           Console.ForegroundColor = ConsoleColor.Green;
           Console.WriteLine("Select username");
            Username = Console.ReadLine();
            while (true)
            {
                Client client = null;
                TcpClient outPutClient = null;
                Console.Clear();
                MainMenu();

                void MainMenu()
                {
                begin:
                    Console.Clear();
                    Console.WriteLine("Welcome: " + Username + " to test P2P hacking game");
                    Console.WriteLine("Do you want to check information, create a session or join a session?");
                    Console.WriteLine("create a session: C or Create");
                    Console.WriteLine("join a session: J or Join");
                    Console.WriteLine("information: inf, info or information");
                    string command = Console.ReadLine().ToLower();

                    if (command != "c" && command != "create" && command != "j" && command != "join" && command != "inf" && command != "info" && command != "information")
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
                        case "c":
                        case "create":
                            CreateSession();
                            break;
                        case "j":
                        case "join":
                            JoinSession();
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
                    Console.WriteLine("dhaouwdawouh");
                    Thread.Sleep(1000);
                    string dir = Directory.GetCurrentDirectory();
                    /*Process pr = new Process();
                    pr.StartInfo.FileName = Path.Combine(dir + @"\Output.exe");
                    pr.StartInfo.Arguments = "echo Hello!";
                    pr.StartInfo.CreateNoWindow = false;
                    pr.Start();*/
                    Console.WriteLine("Trying to connect to server...");
                    try
                    {
                        Int32 port = 1300;
                        Console.Clear();
                        client = new Client(port, "130.61.171.190");
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
        public static class GameConsole
        {
            internal static List<string> gameLog = new List<string>();
            public static void Write(List<string> info)
            {
                Console.Clear();
                gameLog.AddRange(info);
                foreach (string item in gameLog)
                {
                    Console.WriteLine(item);
                }
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
        public class Client : IDisposable
        {

            // Called by producers to send data over the socket.
            public void SendData(byte[] data)
            {
                _sender.SendData(data);
            }

            // Consumers register to receive data.
            public event EventHandler<DataReceivedEventArgs> DataReceived;

            public Client(Int32 port, string IP)
            {
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
            private sealed class Receiver
            {
                internal event EventHandler<DataReceivedEventArgs> DataReceived;

                internal Receiver(NetworkStream stream, ref AutoResetEvent resetEv)
                {

                    _stream = stream;
                    _thread = new Thread(Run);
                    _thread.Start();
                    ShutdownEvent = resetEv;
                }

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

                void processData(byte[] instruction)
                {
                    instruction = instruction.Where(x => x != 00).ToArray();
                    byte[] _data = new byte[instruction[0] + (Convert.ToInt32(instruction[0] == 255) * instruction[1])];
                    for(int i = 0; i < _data.Length; i++)
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
                    switch (_data[0])
                    {
                        case 1:
                            GameConsole.Add("Do you wish to create or join a room");
                            GameConsole.Add("LOOSER!!!!");
                            break;
                        case 69:
                            byte[] output = new byte[_data.Length - 1];
                            for (int i = 0; i < output.Length; i++)
                            {
                                output[i] = _data[i+1];
                            }
                            GameConsole.Add(Decoder.Decode(output));
                            break;
                        case 3:
                            GameConsole.Write(writeList);
                            if(_data.Length >= 2) {
                                SendDataEvent?.Invoke(_data[1]);
                            }
                            break;
                    }
                    if(_data.Length + 1 + Convert.ToInt32(instruction[0] == 255) != instruction.Length)
                    {
                        int length = instruction.Length - (_data.Length + 1 + Convert.ToInt32(instruction[0] == 255));
                        byte[] newInstruction = new byte[length];
                        for(int i = 0; i < newInstruction.Length; i++)
                        {
                            newInstruction[i] = instruction[1 + Convert.ToInt32(instruction[0] == 255) + i + _data.Length];
                        }
                        processData(newInstruction);
                    }
                }
            }

            private sealed class Sender
            {

                internal void SendData(byte[] data)
                {
                    _sendData = data;
                }
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
                            if (input.ToLower().Contains("create"))
                            {

                                string players = input.ToLower().Split(' ')[1];
                                byte maxPlayers = 4;
                                byte.TryParse(players, out maxPlayers);
                                maxPlayers = Convert.ToByte((maxPlayers * Convert.ToByte(maxPlayers != 0)) + (((byte)4) * Convert.ToByte(maxPlayers == 0)));
                                maxPlayers = Math.Min((byte)8, maxPlayers);
                                SendData(new byte[] { 2, 1, maxPlayers});
                            }
                            else if (input.ToLower().Contains("join"))
                            {
                                string indexString = input.ToLower().Split(' ')[1];
                                byte indexByte = 0;
                                byte.TryParse(indexString, out indexByte);
                                SendData(new byte[] {2, 2, indexByte});
                            }
                            else
                            {
                                SendData(new byte[] { 10, 10 });
                            }
                            break;
                        default:
                            switch (input.ToLower())
                            {
                                default: SendData(new byte[] { 10, 10 }); break;
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
