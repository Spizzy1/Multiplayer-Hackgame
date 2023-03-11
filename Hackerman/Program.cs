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
        static void Main(string[] args)
        {
           Console.ForegroundColor = ConsoleColor.Green;
           Console.WriteLine("Select username");
           string Username = Console.ReadLine();
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
                    string dir = Directory.GetCurrentDirectory();
                    Process pr = new Process();
                    pr.StartInfo.FileName = Path.Combine(dir + @"\Output.exe");
                    pr.StartInfo.Arguments = "echo Hello!";
                    pr.StartInfo.CreateNoWindow = false;
                    pr.Start();
                    Console.WriteLine("Trying to connect to server...");
                    try
                    {
                        Int32 port = 1300;
                        Console.Clear();
                        client = new Client(port, "130.61.171.190");
                        Console.WriteLine("Connected to local server...");
                    }
                    catch
                    {

                    }
                    while (true)
                    {
                        Console.Write("Input: ");
                        string command = Console.ReadLine().ToLower();
                        switch (command)
                        {
                            case "status":
                                client.SendData(new byte[] { 1 });
                                break;
                        }
                    }
                }
                void PlayGame()
                {

                }
            }
        }
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
                _output = new TcpClient("127.0.0.1", 3856);
                _client = new TcpClient(IP, port);
                _outPutStream = _output.GetStream();
                _stream = _client.GetStream();
                _shutdownEvent = new AutoResetEvent(false);
                _receiver = new Receiver(_stream, _outPutStream, ref _shutdownEvent);
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
            private TcpClient _output;
            private NetworkStream _outPutStream;
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
                    this._stream.Dispose();
                    this._stream = null;
                }
            }
            private sealed class Receiver
            {
                internal event EventHandler<DataReceivedEventArgs> DataReceived;

                internal Receiver(NetworkStream stream, NetworkStream outPut, ref AutoResetEvent resetEv)
                {

                    _output = outPut;
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
                                                _output.Write(_data, 0, _data.Length);
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
            }

            private sealed class Sender
            {
                internal void SendData(byte[] data)
                {
                    _sendData = data;
                }

                internal Sender(NetworkStream stream, ref AutoResetEvent resetEv)
                {
                    _stream = stream;
                    _thread = new Thread(Run);
                    _thread.Start();
                    ShutdownEvent = resetEv;
                }

                private void Run()
                {
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
